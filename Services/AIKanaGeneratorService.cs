using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Added for Secret Manager
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NipponQuest.Data;
using NipponQuest.Models;

namespace NipponQuest.Services
{
    public class AIKanaGeneratorService : BackgroundService
    {
        private readonly ILogger<AIKanaGeneratorService> _log;
        private readonly IServiceProvider _services;

        // Timeout increased to 10 minutes to prevent TaskCanceledExceptions on slow API calls
        private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        // Configuration variables loaded securely via IConfiguration
        private readonly string _endpoint;
        private readonly string _modelName;
        private readonly string _apiKey;

        // Set to 20 for production daily generation
        private const int BatchSize = 20;

        public AIKanaGeneratorService(ILogger<AIKanaGeneratorService> log,
                                      IServiceProvider services,
                                      IConfiguration config) // Inject IConfiguration
        {
            _log = log;
            _services = services;

            // Grabs from secrets.json locally, or Azure Environment Variables in production
            _endpoint = config["AI_ENDPOINT"] ?? throw new InvalidOperationException("AI_ENDPOINT missing from configuration/secrets.");
            _modelName = config["AI_MODEL"] ?? "gemini-2.5-flash";
            _apiKey = config["AI_API_KEY"] ?? string.Empty;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run once a day at 02:30 UTC (adjust schedule as you like)
            var nextRun = DateTime.UtcNow.Date.AddHours(2.5);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Timer is restored: The service will sleep here until the daily scheduled time
                var delay = nextRun - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                try
                {
                    await GenerateAndPersistAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "AI Kana generation failed");
                }

                nextRun = nextRun.AddDays(1); // schedule next day
            }
        }

        private async Task GenerateAndPersistAsync(CancellationToken token)
        {
            var alphabets = new[] { "hiragana", "katakana", "dakuten", "mixed" };
            var difficulties = new[] { "normal", "hard", "insanity" };

            using var scope = _services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var alpha in alphabets)
                foreach (var diff in difficulties)
                {
                    token.ThrowIfCancellationRequested();

                    var prompt = BuildPrompt(alpha, diff);
                    var requestPayload = BuildProviderPayload(prompt);

                    var httpReq = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(requestPayload),
                                                     Encoding.UTF8, "application/json")
                    };

                    if (!string.IsNullOrWhiteSpace(_apiKey))
                    {
                        if (_endpoint.Contains("generativelanguage.googleapis.com"))
                        {
                            // Native Gemini uses a specific header for API Keys
                            httpReq.Headers.Add("x-goog-api-key", _apiKey);
                        }
                        else
                        {
                            httpReq.Headers.Add("Authorization", $"Bearer {_apiKey}");
                        }
                    }

                    if (_endpoint.Contains("anthropic.com"))
                        httpReq.Headers.Add("anthropic-version", "2023-06-01");

                    var resp = await _http.SendAsync(httpReq, token);
                    resp.EnsureSuccessStatusCode();

                    var json = await resp.Content.ReadAsStringAsync(token);
                    var aiWords = ExtractAiWords(json);

                    // ---- VALIDATION & INSERT ----
                    var insertBatch = new List<KanaWord>();
                    foreach (var w in aiWords)
                    {
                        if (!ValidateAiWord(w, alpha, diff)) continue;

                        bool exists = await ctx.KanaWords.AnyAsync(x =>
                            x.WordKana == w.Kana &&
                            x.Alphabet == alpha &&
                            x.DifficultyLevel == diff, token);
                        if (exists) continue;

                        insertBatch.Add(new KanaWord
                        {
                            WordKana = w.Kana,
                            WordRomaji = w.Romaji,
                            MeaningEnglish = w.Meaning,
                            Alphabet = alpha,
                            DifficultyLevel = diff,
                            CategoryTag = "AI‑Generated",
                            MissingKana = w.Missing,
                            DisplayHtml = w.Display,
                            IsAIGenerated = true,
                            GeneratedAt = DateTime.UtcNow
                        });
                    }

                    if (insertBatch.Count > 0)
                    {
                        await ctx.KanaWords.AddRangeAsync(insertBatch, token);
                        await ctx.SaveChangesAsync(token);
                        _log.LogInformation("Inserted {Count} AI‑generated {Alpha}/{Diff} words.", insertBatch.Count, alpha, diff);
                    }
                }
        }

        // ----------------- Helper methods -----------------

        private string BuildPrompt(string alphabet, string difficulty)
        {
            return $@"
        You are a Japanese vocabulary generator. Generate {BatchSize} unique Japanese words using the {alphabet} script at {difficulty} difficulty.
        You MUST return ONLY a valid JSON array. Do not include any extra text, markdown, or greetings.

        EXAMPLE EXPECTED OUTPUT:
        [
          {{
            ""kana"": ""ねこ"",
            ""romaji"": ""neko"",
            ""meaning"": ""cat"",
            ""missing"": ""こ"",
            ""display"": ""ね<span class='missing-placeholder'>__</span>""
          }}
        ]";
        }

        private object BuildProviderPayload(string prompt)
        {
            if (_endpoint.Contains("generativelanguage.googleapis.com"))
            {
                // Native Google Gemini payload
                return new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        responseMimeType = "application/json" // Native JSON enforcing
                    }
                };
            }
            else if (_endpoint.Contains("vercel.com"))
            {
                return new { model = _modelName, prompt = prompt, temperature = 0.7, max_tokens = 2048 };
            }
            else if (_endpoint.Contains("anthropic.com"))
            {
                return new
                {
                    model = _modelName,
                    max_tokens = 2048,
                    temperature = 0.7,
                    messages = new[]
                    {
                          new { role = "system", content = "You are a helpful assistant." },
                          new { role = "user",   content = prompt }
                    }
                };
            }
            else if (_endpoint.Contains("huggingface.co"))
            {
                return new { inputs = prompt, parameters = new { temperature = 0.7, max_new_tokens = 1024 } };
            }
            else if (_endpoint.Contains("groq.com") || _endpoint.Contains("openai.com"))
            {
                return new
                {
                    model = _modelName,
                    temperature = 0.7,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant." },
                        new { role = "user", content = prompt }
                    }
                };
            }
            else if (_endpoint.Contains("11434") || _endpoint.Contains("ollama"))
            {
                return new
                {
                    model = _modelName,
                    prompt = prompt,
                    stream = false,
                    format = "json",
                    options = new { temperature = 0.7 }
                };
            }
            throw new NotSupportedException($"Endpoint '{_endpoint}' not recognised for payload building.");
        }

        private List<AiKanaDto> ExtractAiWords(string rawJson)
        {
            string textToParse = "";

            // 1. Extract the core text from the provider's unique response shell
            if (_endpoint.Contains("generativelanguage.googleapis.com"))
            {
                using var doc = JsonDocument.Parse(rawJson);
                textToParse = doc.RootElement
                                 .GetProperty("candidates")[0]
                                 .GetProperty("content")
                                 .GetProperty("parts")[0]
                                 .GetProperty("text")
                                 .GetString()!;
            }
            else if (_endpoint.Contains("vercel.com"))
            {
                textToParse = rawJson;
            }
            else if (_endpoint.Contains("anthropic.com"))
            {
                using var doc = JsonDocument.Parse(rawJson);
                textToParse = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString()!;
            }
            else if (_endpoint.Contains("huggingface.co"))
            {
                using var doc = JsonDocument.Parse(rawJson);
                textToParse = doc.RootElement[0].GetProperty("generated_text").GetString()!;
            }
            else if (_endpoint.Contains("groq.com") || _endpoint.Contains("openai.com"))
            {
                using var doc = JsonDocument.Parse(rawJson);
                textToParse = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
            }
            else if (_endpoint.Contains("11434") || _endpoint.Contains("ollama"))
            {
                using var doc = JsonDocument.Parse(rawJson);
                textToParse = doc.RootElement.GetProperty("response").GetString()!;
            }
            else
            {
                throw new NotSupportedException($"Cannot parse response from {_endpoint}");
            }

            // 2. Global Bulletproof JSON Extraction
            int arrayStart = textToParse.IndexOf('[');
            int arrayEnd = textToParse.LastIndexOf(']');
            int objStart = textToParse.IndexOf('{');
            int objEnd = textToParse.LastIndexOf('}');

            string cleanJson = "";

            if (arrayStart != -1 && arrayEnd != -1 && arrayEnd > arrayStart)
            {
                cleanJson = textToParse.Substring(arrayStart, arrayEnd - arrayStart + 1);
            }
            else if (objStart != -1 && objEnd != -1 && objEnd > objStart)
            {
                string singleObj = textToParse.Substring(objStart, objEnd - objStart + 1);
                cleanJson = $"[{singleObj}]";
            }
            else
            {
                throw new FormatException($"AI did not return valid JSON. Raw response: {textToParse}");
            }

            // 3. Deserialize with forgiving options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            return JsonSerializer.Deserialize<List<AiKanaDto>>(cleanJson, options)!;
        }

        private bool ValidateAiWord(AiKanaDto w, string alphabet, string diff)
        {
            if (string.IsNullOrWhiteSpace(w.Kana) || string.IsNullOrWhiteSpace(w.Romaji) ||
                string.IsNullOrWhiteSpace(w.Missing) || string.IsNullOrWhiteSpace(w.Display))
                return false;

            if (!w.Kana.Contains(w.Missing)) return false;

            return true;
        }

        private class AiKanaDto
        {
            public string Kana { get; set; } = "";
            public string Romaji { get; set; } = "";
            public string Meaning { get; set; } = "";
            public string Missing { get; set; } = "";
            public string Display { get; set; } = "";
        }
    }
}
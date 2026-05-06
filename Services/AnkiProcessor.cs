using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using NipponQuest.Models;
using System.Text.RegularExpressions;

namespace NipponQuest.Services
{
    public static class AnkiProcessor
    {
        public static List<Flashcard> GetCardsFromPackage(string packagePath, string workDir, int deckId)
        {
            SQLitePCL.Batteries_V2.Init();

            string dbFile = Path.Combine(workDir, "collection.anki2");
            string mediaMapFile = Path.Combine(workDir, "media");
            string mediaDir = Path.Combine(workDir, "media_files");

            Directory.CreateDirectory(mediaDir);
            var cards = new List<Flashcard>();

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(packagePath))
                {
                    var dbEntry = archive.GetEntry("collection.anki21") ?? archive.GetEntry("collection.anki2");
                    if (dbEntry == null) throw new Exception("No Anki database found.");
                    dbEntry.ExtractToFile(dbFile, true);

                    var mediaEntry = archive.GetEntry("media");
                    if (mediaEntry != null) mediaEntry.ExtractToFile(mediaMapFile, true);

                    foreach (var entry in archive.Entries)
                    {
                        if (int.TryParse(entry.Name, out _))
                        {
                            entry.ExtractToFile(Path.Combine(mediaDir, entry.Name), true);
                        }
                    }
                }

                if (File.Exists(mediaMapFile))
                {
                    var mediaJson = File.ReadAllText(mediaMapFile);
                    var mediaMap = JsonSerializer.Deserialize<Dictionary<string, string>>(mediaJson);
                    if (mediaMap != null)
                    {
                        foreach (var entry in mediaMap)
                        {
                            string oldPath = Path.Combine(mediaDir, entry.Key);
                            string newPath = Path.Combine(mediaDir, entry.Value);
                            if (File.Exists(oldPath)) File.Move(oldPath, newPath, true);
                        }
                    }
                }

                string connStr = $"Data Source={dbFile};Mode=ReadOnly;Pooling=False;";
                using (var sqlite = new SqliteConnection(connStr))
                {
                    sqlite.Open();
                    using var cmd = sqlite.CreateCommand();

                    // We grab 'flds' (the data) and 'sfld' (often the sort field)
                    cmd.CommandText = "SELECT flds FROM notes";

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var rawFields = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        var fields = rawFields.Split((char)0x1f);

                        if (fields.Length >= 2)
                        {
                            // CORE 2000 LOGIC: 
                            // Often Field 0 is a number. 
                            // We look for the first field that actually contains Japanese characters.
                            string front = fields[0];
                            string back = fields[1];

                            if (Regex.IsMatch(front, @"^\d+$") && fields.Length > 2)
                            {
                                // If front is just a number, shift to the next fields
                                front = fields[1];
                                back = fields[2];
                            }

                            string fullContent = string.Join(" ", fields);

                            cards.Add(new Flashcard
                            {
                                DeckId = deckId,
                                // Clean HTML but keep the text
                                FrontText = CleanHtml(front),
                                BackText = CleanHtml(back),
                                ImageFilePath = ExtractFilename(fullContent, "img"),
                                AudioFilePath = ExtractFilename(fullContent, "audio"),
                                Interval = 0,
                                EaseFactor = 2.5,
                                NextReview = DateTime.UtcNow,
                                SuccessCount = 0
                            });
                        }
                    }
                }
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(dbFile)) File.Delete(dbFile);
                if (File.Exists(mediaMapFile)) File.Delete(mediaMapFile);
            }
            return cards;
        }

        private static string CleanHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            // Remove HTML tags but keep content
            string step1 = Regex.Replace(input, "<.*?>", string.Empty);
            // Replace HTML entities like &nbsp;
            return System.Net.WebUtility.HtmlDecode(step1).Trim();
        }

        private static string ExtractFilename(string content, string type)
        {
            if (string.IsNullOrEmpty(content)) return "";

            if (type == "img")
            {
                var match = Regex.Match(content, @"src=[""']([^""']+\.(?:png|jpg|jpeg|gif|webp))[""']", RegexOptions.IgnoreCase);
                return match.Success ? match.Groups[1].Value : "";
            }
            else if (type == "audio")
            {
                var match = Regex.Match(content, @"\[sound:([^\]]+\.(?:mp3|wav|ogg|m4a))\]", RegexOptions.IgnoreCase);
                return match.Success ? match.Groups[1].Value : "";
            }
            return "";
        }
    }
}
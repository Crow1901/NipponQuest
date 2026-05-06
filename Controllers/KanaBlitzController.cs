using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NipponQuest.Data;
using NipponQuest.Models;

namespace NipponQuest.Controllers
{
    [Authorize]
    public class KanaBlitzController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        // ── Unlock gates ──
        // HARD     : 90% accuracy AND 30+ correct words on every Normal script
        // INSANITY : 95% accuracy AND 25+ correct words on every Hard script + Account Level 15
        private const double HardUnlockThreshold = 0.90;
        private const int HardCorrectFloor = 30;
        private const double InsanityUnlockThreshold = 0.95;
        private const int InsanityCorrectFloor = 25;
        private const int InsanityRequiredLevel = 15;
        private const int MinAttemptsForUnlock = 8;

        private static readonly string[] GatedAlphabets = new[] { "hiragana", "katakana", "dakuten", "mixed" };

        public KanaBlitzController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index() => View();

        // ─────────────────────────────────────────────────────────────
        //  UNLOCKS
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetUnlocks()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var stats = ParseAccuracy(user.BlitzAccuracyJson);
            int level = ResolveLevel(user);

            var hardAcc = GatedAlphabets.ToDictionary(a => a, a => stats.GetValueOrDefault($"normal:{a}", 0.0));
            var hardMax = GatedAlphabets.ToDictionary(a => a, a => (int)stats.GetValueOrDefault($"normal:{a}:max", 0.0));

            bool hardUnlocked = GatedAlphabets.All(a =>
                hardAcc[a] >= HardUnlockThreshold && hardMax[a] >= HardCorrectFloor);

            var insAcc = GatedAlphabets.ToDictionary(a => a, a => stats.GetValueOrDefault($"hard:{a}", 0.0));
            var insMax = GatedAlphabets.ToDictionary(a => a, a => (int)stats.GetValueOrDefault($"hard:{a}:max", 0.0));

            bool insanityHardCleared = GatedAlphabets.All(a =>
                insAcc[a] >= InsanityUnlockThreshold && insMax[a] >= InsanityCorrectFloor);
            bool insanityUnlocked = insanityHardCleared && level >= InsanityRequiredLevel;

            return Json(new
            {
                level,
                hard = new
                {
                    unlocked = hardUnlocked,
                    threshold = HardUnlockThreshold,
                    correctFloor = HardCorrectFloor,
                    progress = hardAcc,
                    progressMax = hardMax
                },
                insanity = new
                {
                    unlocked = insanityUnlocked,
                    threshold = InsanityUnlockThreshold,
                    correctFloor = InsanityCorrectFloor,
                    requiredLevel = InsanityRequiredLevel,
                    levelMet = level >= InsanityRequiredLevel,
                    hardCleared = insanityHardCleared,
                    progress = insAcc,
                    progressMax = insMax
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  ARENA PAYLOAD
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetArenaPayload(string alphabet, string difficulty)
        {
            try
            {
                if (string.IsNullOrEmpty(alphabet) || string.IsNullOrEmpty(difficulty))
                    return BadRequest(new { error = "Missing alphabet or difficulty parameter." });

                var diff = difficulty.ToLower();
                var alpha = alphabet.ToLower();

                var user = await _userManager.GetUserAsync(User);
                if (user != null && (diff == "hard" || diff == "insanity"))
                {
                    var stats = ParseAccuracy(user.BlitzAccuracyJson);
                    int level = ResolveLevel(user);

                    if (diff == "hard")
                    {
                        bool ok = GatedAlphabets.All(a =>
                            stats.GetValueOrDefault($"normal:{a}", 0.0) >= HardUnlockThreshold &&
                            stats.GetValueOrDefault($"normal:{a}:max", 0.0) >= HardCorrectFloor);
                        if (!ok)
                            return StatusCode(403, new { error = $"Hard mode is locked. Reach {(int)(HardUnlockThreshold * 100)}% accuracy AND {HardCorrectFloor}+ correct words in a single Normal run on every script." });
                    }
                    else
                    {
                        bool clearedHard = GatedAlphabets.All(a =>
                            stats.GetValueOrDefault($"hard:{a}", 0.0) >= InsanityUnlockThreshold &&
                            stats.GetValueOrDefault($"hard:{a}:max", 0.0) >= InsanityCorrectFloor);
                        if (!clearedHard || level < InsanityRequiredLevel)
                            return StatusCode(403, new { error = $"Insanity is locked. Reach {(int)(InsanityUnlockThreshold * 100)}% accuracy AND {InsanityCorrectFloor}+ correct words in a single Hard run on every script, plus Account Level {InsanityRequiredLevel}." });
                    }
                }

                var query = _context.KanaWords.AsNoTracking().Where(w => w.DifficultyLevel == diff);

                if (alpha == "mixed")
                    query = query.Where(w => w.Alphabet == "hiragana"
                                          || w.Alphabet == "katakana"
                                          || w.Alphabet == "dakuten"
                                          || w.Alphabet == "mixed");
                else
                    query = query.Where(w => w.Alphabet == alpha);

                var rawWords = await query.ToListAsync();
                if (rawWords.Count == 0)
                {
                    return Json(new
                    {
                        alphabet = alpha,
                        words = new object[0],
                        distractors = new string[0],
                        warning = $"No words seeded for alphabet='{alpha}' difficulty='{diff}'. Re-seed the database."
                    });
                }

                var rng = new Random();
                var payload = new List<object>();
                var distractorSet = new HashSet<string>();

                foreach (var w in rawWords)
                {
                    string missing;
                    string display;

                    if (diff == "easy")
                    {
                        display = (w.WordRomaji ?? "").ToUpper();
                        missing = w.WordKana ?? "";
                    }
                    else
                    {
                        // Always randomize at runtime so the missing kana
                        // varies between FIRST / MIDDLE / LAST positions.
                        (missing, display) = BuildRandomMissing(w.WordKana, diff, rng);
                    }

                    if (!string.IsNullOrEmpty(missing))
                        distractorSet.Add(missing);

                    payload.Add(new
                    {
                        id = w.Id,
                        kana = w.WordKana,
                        romaji = w.WordRomaji,
                        meaning = w.MeaningEnglish,
                        category = w.CategoryTag ?? "",
                        missing,
                        display
                    });
                }

                var shuffled = payload.OrderBy(_ => Guid.NewGuid()).ToList();

                return Json(new
                {
                    alphabet = alpha,
                    words = shuffled,
                    distractors = distractorSet.ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Arena payload failed.",
                    detail = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  RANDOM-MISSING BUILDER
        //  Splits a word into syllables (treating ゃゅょっッー as part of
        //  the previous kana) and blanks ONE random syllable. Works for
        //  both plain kana words and "Kanji (furigana)" insanity entries.
        // ─────────────────────────────────────────────────────────────
        private static (string missing, string displayHtml) BuildRandomMissing(string wordKana, string difficulty, Random rng)
        {
            const string Blank = "<span class='missing-placeholder'>__</span>";
            if (string.IsNullOrEmpty(wordKana)) return ("", Blank);

            // INSANITY format: "漢字 (ふりがな)" — blank inside the furigana
            if (wordKana.Contains('(') && wordKana.Contains(')'))
            {
                int open = wordKana.IndexOf('(');
                int close = wordKana.IndexOf(')');
                string kanji = wordKana.Substring(0, open).Trim();
                string furigana = wordKana.Substring(open + 1, close - open - 1).Trim();

                var sylls = SplitSyllables(furigana);
                if (sylls.Count == 0)
                {
                    return (furigana,
                        $"<div class=\"kanji-stack\"><div class=\"kanji-top\">{kanji}</div>" +
                        $"<div class=\"kana-bottom\">({Blank})</div></div>");
                }

                int idx = rng.Next(sylls.Count);
                string missingChar = sylls[idx];

                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < sylls.Count; i++)
                {
                    if (i == idx) sb.Append($"<span class='missing-placeholder'>{sylls[i]}</span>");
                    else sb.Append(sylls[i]);
                }

                string html = $"<div class=\"kanji-stack\">" +
                              $"<div class=\"kanji-top\">{kanji}</div>" +
                              $"<div class=\"kana-bottom\">{sb}</div>" +
                              $"</div>";
                return (missingChar, html);
            }

            // NORMAL / HARD plain kana
            var s = SplitSyllables(wordKana);
            if (s.Count == 0) return (wordKana, Blank);

            int p = rng.Next(s.Count);
            string miss = s[p];

            var outBuf = new System.Text.StringBuilder();
            for (int i = 0; i < s.Count; i++)
            {
                if (i == p) outBuf.Append($"<span class='missing-placeholder'>{s[i]}</span>");
                else outBuf.Append(s[i]);
            }
            return (miss, outBuf.ToString());
        }

        private static List<string> SplitSyllables(string s)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s)) return list;

            var smallMods = new HashSet<char> { 'ゃ', 'ゅ', 'ょ', 'ャ', 'ュ', 'ョ', 'っ', 'ッ', 'ー' };

            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '(' || c == ')') { i++; continue; }
                string syll = c.ToString();
                if (i + 1 < s.Length && smallMods.Contains(s[i + 1]))
                {
                    syll += s[i + 1];
                    i += 2;
                }
                else
                {
                    i += 1;
                }
                list.Add(syll);
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────────
        //  SUBMIT SCORE
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SubmitScore([FromBody] BlitzResultDto result)
        {
            try
            {
                if (result == null) return BadRequest(new { error = "Empty payload." });

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                string diffKey = (result.Difficulty ?? "").ToLower();
                string alphaKey = (result.Alphabet ?? "").ToLower();

                // ── XP / GOLD computation ──
                const double baseExp = 3.0;
                const double baseGold = 0.8;

                double diffMult = diffKey switch
                {
                    "easy" => 1.0,
                    "normal" => 2.0,
                    "hard" => 4.0,
                    "insanity" => 8.0,
                    _ => 1.0
                };

                double comboMult = 1.0 + Math.Min(result.MaxCombo, 30) * 0.04;

                int totalAttempts = result.CorrectAnswers + result.Mistakes;
                double accuracy = totalAttempts > 0 ? (double)result.CorrectAnswers / totalAttempts : 0;
                double accuracyMult = accuracy switch
                {
                    >= 0.95 => 1.30,
                    >= 0.85 => 1.10,
                    >= 0.70 => 1.00,
                    _ => 0.90
                };

                double rawExp = result.CorrectAnswers * baseExp * diffMult * comboMult * accuracyMult;
                double rawGold = result.CorrectAnswers * baseGold * diffMult * comboMult * accuracyMult;

                int perfectExpBonus = 0;
                int perfectGoldBonus = 0;
                if (result.Mistakes == 0 && result.CorrectAnswers >= 5)
                {
                    perfectExpBonus = (int)(20 * diffMult);
                    perfectGoldBonus = (int)(8 * diffMult);
                }

                int mistakePenalty = Math.Min(result.Mistakes * 2, (int)(rawExp * 0.3));

                int expEarned = Math.Max(0, (int)Math.Round(rawExp) - mistakePenalty + perfectExpBonus);
                int goldEarned = Math.Max(0, (int)Math.Round(rawGold) + perfectGoldBonus);

                user.TotalEXP += expEarned;
                user.CurrentXP += expEarned;
                user.WeeklyXP += expEarned;
                user.Gold += goldEarned;
                user.LessonsCompleted += 1;

                _context.RewardLedgers.Add(new RewardLedger
                {
                    ApplicationUserId = user.Id,
                    ExpDelta = expEarned,
                    GoldDelta = goldEarned,
                    Source = "kanablitz"
                });

                // ── BlitzAccuracyJson update ──
                if (totalAttempts >= 5)
                {
                    var stats = ParseAccuracy(user.BlitzAccuracyJson);
                    string accK = $"{diffKey}:{alphaKey}";
                    string maxK = $"{diffKey}:{alphaKey}:max";

                    if (totalAttempts >= MinAttemptsForUnlock && accuracy > stats.GetValueOrDefault(accK, 0.0))
                        stats[accK] = accuracy;

                    if (result.CorrectAnswers > stats.GetValueOrDefault(maxK, 0.0))
                        stats[maxK] = result.CorrectAnswers;

                    user.BlitzAccuracyJson = JsonSerializer.Serialize(stats);
                }

                // ── PERSONAL BEST + LEADERBOARD DELTA ──
                bool isNewPB = false;
                int rankBefore = 0;
                int rankAfter = 0;
                int pbCorrect = 0;
                int pbPoints = 0;

                if (!string.IsNullOrEmpty(diffKey) && !string.IsNullOrEmpty(alphaKey))
                {
                    var pb = await _context.BlitzPersonalBests
                        .FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id
                                              && p.Difficulty == diffKey
                                              && p.Alphabet == alphaKey);

                    int previousBestCorrect = pb?.BestCorrect ?? 0;

                    rankBefore = await _context.BlitzPersonalBests
                        .Where(p => p.Difficulty == diffKey && p.Alphabet == alphaKey)
                        .CountAsync(p => p.BestCorrect > previousBestCorrect) + 1;

                    if (pb == null)
                    {
                        pb = new BlitzPersonalBest
                        {
                            ApplicationUserId = user.Id,
                            Difficulty = diffKey,
                            Alphabet = alphaKey
                        };
                        _context.BlitzPersonalBests.Add(pb);
                    }

                    if (result.CorrectAnswers > pb.BestCorrect) { pb.BestCorrect = result.CorrectAnswers; isNewPB = true; }
                    if (result.Points > pb.BestPoints) { pb.BestPoints = result.Points; isNewPB = true; }
                    if (result.MaxCombo > pb.BestCombo) { pb.BestCombo = result.MaxCombo; }
                    if (totalAttempts >= MinAttemptsForUnlock && accuracy > pb.BestAccuracy)
                        pb.BestAccuracy = accuracy;
                    pb.UpdatedAt = DateTime.UtcNow;

                    pbCorrect = pb.BestCorrect;
                    pbPoints = pb.BestPoints;

                    await _userManager.UpdateAsync(user);
                    await _context.SaveChangesAsync();

                    rankAfter = await _context.BlitzPersonalBests
                        .Where(p => p.Difficulty == diffKey && p.Alphabet == alphaKey)
                        .CountAsync(p => p.BestCorrect > pb.BestCorrect) + 1;
                }
                else
                {
                    await _userManager.UpdateAsync(user);
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    exp = expEarned,
                    gold = goldEarned,
                    points = result.Points,
                    correct = result.CorrectAnswers,
                    accuracy = Math.Round(accuracy * 100),
                    maxCombo = result.MaxCombo,
                    perfectBonus = perfectExpBonus > 0,

                    isNewPB,
                    pbCorrect,
                    pbPoints,
                    rankBefore,
                    rankAfter,
                    rankDelta = rankBefore - rankAfter
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Score submission failed.",
                    detail = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────
        private static Dictionary<string, double> ParseAccuracy(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, double>();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, double>>(json)
                       ?? new Dictionary<string, double>();
            }
            catch
            {
                return new Dictionary<string, double>();
            }
        }

        private bool ValidateAiWord(KanaWord word)
        {
            // Basic sanity: romaji length matches kana length (rough check)
            if (string.IsNullOrWhiteSpace(word.WordKana) ||
                string.IsNullOrWhiteSpace(word.WordRomaji) ||
                string.IsNullOrWhiteSpace(word.MissingKana))
                return false;

            // Ensure the missing fragment actually exists in the full kana string
            if (!word.WordKana.Contains(word.MissingKana))
                return false;

            // You can plug in a full kana→romaji library here for stricter verification.
            return true;
        }

        private static int ResolveLevel(ApplicationUser user)
        {
            var t = user.GetType();
            foreach (var name in new[] { "Level", "CurrentLevel", "AccountLevel", "PlayerLevel" })
            {
                var p = t.GetProperty(name);
                if (p != null && p.PropertyType == typeof(int))
                    return (int)(p.GetValue(user) ?? 0);
            }
            var xpProp = t.GetProperty("TotalEXP");
            if (xpProp != null && xpProp.PropertyType == typeof(int))
                return Math.Max(1, ((int)(xpProp.GetValue(user) ?? 0)) / 1000 + 1);
            return 1;
        }
    }

    public class BlitzResultDto
    {
        public string Alphabet { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public int CorrectAnswers { get; set; }
        public int Mistakes { get; set; }
        public int MaxCombo { get; set; }
        public int Points { get; set; }
        public int DurationSeconds { get; set; }
    }
}
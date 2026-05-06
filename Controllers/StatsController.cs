using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
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
    public class StatsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StatsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET /Stats
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userId = user.Id;
            var nowUtc = DateTime.UtcNow;
            var sevenDaysAgo = nowUtc.AddDays(-6).Date;
            var thirtyDaysAgo = nowUtc.AddDays(-29).Date;

            // ── FLASHCARD AGGREGATES ─────────────────────────────
            var allCards = await _context.Flashcards
                .Where(f => f.Deck!.ApplicationUserId == userId)
                .Select(f => new { f.Id, f.SuccessCount, f.Interval, f.LastReviewed, f.NextReview, f.DeckId })
                .ToListAsync();

            int totalCards = allCards.Count;
            int cardsLearned = allCards.Count(c => c.SuccessCount > 0);
            int cardsMastered = allCards.Count(c => c.SuccessCount >= 5);
            int cardsLearning = allCards.Count(c => c.SuccessCount > 0 && c.SuccessCount < 5);
            int cardsNew = allCards.Count(c => c.SuccessCount == 0);
            int cardsDueToday = allCards.Count(c => c.NextReview <= nowUtc);
            int reviewedToday = allCards.Count(c => c.LastReviewed >= nowUtc.Date);

            int totalDecks = await _context.Decks.CountAsync(d => d.ApplicationUserId == userId);
            int communityClones = await _context.Decks.CountAsync(d => d.ApplicationUserId == userId && d.IsCommunityClone);
            int publishedDecks = await _context.Decks.CountAsync(d => d.ApplicationUserId == userId && d.IsPublic);

            // ── DAILY REVIEW HEATMAP / LINE (last 30 days, derived from LastReviewed) ──
            var dailyReviews = Enumerable.Range(0, 30)
                .Select(i => thirtyDaysAgo.AddDays(i))
                .ToDictionary(d => d, _ => 0);

            foreach (var c in allCards.Where(c => c.LastReviewed.HasValue && c.LastReviewed.Value >= thirtyDaysAgo))
            {
                var key = c.LastReviewed!.Value.Date;
                if (dailyReviews.ContainsKey(key)) dailyReviews[key]++;
            }

            // ── REWARD LEDGER (XP / Gold per day, last 30) ───────
            // Falls back to Flashcard activity if ledger empty.
            List<DailyReward> dailyRewards = await BuildDailyRewards(userId, thirtyDaysAgo, nowUtc, dailyReviews);

            // Weekly / Monthly totals
            int weeklyExp = dailyRewards.Where(r => r.Date >= sevenDaysAgo).Sum(r => r.Exp);
            int weeklyGold = dailyRewards.Where(r => r.Date >= sevenDaysAgo).Sum(r => r.Gold);
            int monthlyExp = dailyRewards.Sum(r => r.Exp);
            int monthlyGold = dailyRewards.Sum(r => r.Gold);

            // ── KANABLITZ ACCURACY MATRIX ────────────────────────
            var blitz = ParseBlitzAccuracy(user.BlitzAccuracyJson);
            string[] alphas = { "hiragana", "katakana", "dakuten", "mixed" };
            string[] diffs = { "easy", "normal", "hard", "insanity" };

            var blitzRows = diffs.Select(d => new BlitzMatrixRow
            {
                Difficulty = d,
                Cells = alphas.Select(a => new BlitzMatrixCell
                {
                    Alphabet = a,
                    Accuracy = blitz.GetValueOrDefault($"{d}:{a}", 0.0)
                }).ToList()
            }).ToList();

            double overallBlitzAccuracy = blitz.Count == 0 ? 0
                : blitz.Values.Average();

            // ── PRODUCTIVITY INSIGHTS (dynamic tips) ─────────────
            var insights = BuildInsights(blitz, dailyRewards, cardsDueToday, user.LoginStreak, allCards, nowUtc);

            // ── RECENT WINS (top decks by mastered count) ────────
            var topDecks = await _context.Decks
                .Where(d => d.ApplicationUserId == userId)
                .Select(d => new TopDeckRow
                {
                    Id = d.Id,
                    Title = d.Title ?? "Untitled",
                    ThemeColor = d.ThemeColor,
                    TotalCards = d.Flashcards.Count(),
                    Mastered = d.Flashcards.Count(f => f.SuccessCount >= 5)
                })
                .OrderByDescending(d => d.Mastered)
                .ThenByDescending(d => d.TotalCards)
                .Take(5)
                .ToListAsync();

            // ── ACTIVE STREAK CALENDAR (last 30 days) ─────────────
            var calendar = dailyReviews
                .OrderBy(kv => kv.Key)
                .Select(kv => new CalendarCell { Date = kv.Key, Count = kv.Value })
                .ToList();

            var vm = new StatsViewModel
            {
                User = user,
                TotalCards = totalCards,
                CardsLearned = cardsLearned,
                CardsMastered = cardsMastered,
                CardsLearning = cardsLearning,
                CardsNew = cardsNew,
                CardsDueToday = cardsDueToday,
                ReviewedToday = reviewedToday,
                TotalDecks = totalDecks,
                CommunityClones = communityClones,
                PublishedDecks = publishedDecks,
                WeeklyExp = weeklyExp,
                WeeklyGold = weeklyGold,
                MonthlyExp = monthlyExp,
                MonthlyGold = monthlyGold,
                DailyRewards = dailyRewards,
                BlitzRows = blitzRows,
                OverallBlitzAccuracy = overallBlitzAccuracy,
                Insights = insights,
                TopDecks = topDecks,
                Calendar = calendar
            };

            return View(vm);
        }

        // ── HELPERS ──────────────────────────────────────────────
        private async Task<List<DailyReward>> BuildDailyRewards(
            string userId, DateTime since, DateTime nowUtc, Dictionary<DateTime, int> dailyReviews)
        {
            var seed = Enumerable.Range(0, 30)
                .Select(i => since.AddDays(i))
                .ToDictionary(d => d, _ => new DailyReward { Date = _, Exp = 0, Gold = 0 });

            try
            {
                var ledger = await _context.Set<RewardLedger>()
                    .Where(r => r.ApplicationUserId == userId && r.EarnedAt >= since)
                    .GroupBy(r => r.EarnedAt.Date)
                    .Select(g => new { Date = g.Key, Exp = g.Sum(x => x.ExpDelta), Gold = g.Sum(x => x.GoldDelta) })
                    .ToListAsync();

                foreach (var row in ledger)
                    if (seed.ContainsKey(row.Date))
                    {
                        seed[row.Date].Exp = row.Exp;
                        seed[row.Date].Gold = row.Gold;
                    }
            }
            catch
            {
                // Ledger table not migrated yet — fall back to inferred activity from flashcard reviews
                foreach (var (day, count) in dailyReviews)
                    if (seed.ContainsKey(day))
                    {
                        seed[day].Exp = count * 1;     // est: 1 XP per card
                        seed[day].Gold = count > 0 ? 5 : 0; // est: 5 Gold per session-day
                    }
            }

            return seed.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
        }

        private static Dictionary<string, double> ParseBlitzAccuracy(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? new();
            }
            catch { return new(); }
        }

        private static List<string> BuildInsights(
            Dictionary<string, double> blitz,
            List<DailyReward> rewards,
            int dueToday,
            int loginStreak,
            IEnumerable<dynamic> allCards,
            DateTime nowUtc)
        {
            var tips = new List<string>();

            // Lowest blitz accuracy script
            if (blitz.Any())
            {
                var weakest = blitz.OrderBy(kv => kv.Value).First();
                if (weakest.Value < 0.85)
                {
                    var parts = weakest.Key.Split(':');
                    tips.Add($"Your weakest KanaBlitz lane is {parts[0].ToUpper()} on {parts[1].ToUpper()} at {Math.Round(weakest.Value * 100)}%. Run it twice this week to push past 90%.");
                }
            }
            else
            {
                tips.Add("Run a KanaBlitz session today — the arena is the fastest way to spot weak letters.");
            }

            if (dueToday > 0)
                tips.Add($"You have {dueToday} flashcard{(dueToday == 1 ? "" : "s")} due. Clear the queue to lock in retention before they decay.");

            // Streak protection
            var todayReviews = rewards.LastOrDefault();
            if (loginStreak >= 3 && (todayReviews?.Exp ?? 0) == 0)
                tips.Add($"Streak alert: {loginStreak} days strong. Review one card or run a 60s blitz to keep the chain alive.");

            // Consistency
            var activeDays = rewards.Count(r => r.Exp > 0);
            if (activeDays < 10)
                tips.Add($"You trained on {activeDays} of the last 30 days. Aim for 4 active days a week — short sessions beat long marathons.");

            if (tips.Count == 0)
                tips.Add("You are on pace. Keep cycling between flashcards and KanaBlitz to balance recall and recognition.");

            return tips;
        }

        // ── VIEW MODELS ──────────────────────────────────────────
        public class StatsViewModel
        {
            public ApplicationUser User { get; set; } = null!;
            public int TotalCards { get; set; }
            public int CardsLearned { get; set; }
            public int CardsMastered { get; set; }
            public int CardsLearning { get; set; }
            public int CardsNew { get; set; }
            public int CardsDueToday { get; set; }
            public int ReviewedToday { get; set; }
            public int TotalDecks { get; set; }
            public int CommunityClones { get; set; }
            public int PublishedDecks { get; set; }
            public int WeeklyExp { get; set; }
            public int WeeklyGold { get; set; }
            public int MonthlyExp { get; set; }
            public int MonthlyGold { get; set; }
            public List<DailyReward> DailyRewards { get; set; } = new();
            public List<BlitzMatrixRow> BlitzRows { get; set; } = new();
            public double OverallBlitzAccuracy { get; set; }
            public List<string> Insights { get; set; } = new();
            public List<TopDeckRow> TopDecks { get; set; } = new();
            public List<CalendarCell> Calendar { get; set; } = new();
        }

        public class DailyReward
        {
            public DateTime Date { get; set; }
            public int Exp { get; set; }
            public int Gold { get; set; }
        }
        public class BlitzMatrixRow
        {
            public string Difficulty { get; set; } = "";
            public List<BlitzMatrixCell> Cells { get; set; } = new();
        }
        public class BlitzMatrixCell
        {
            public string Alphabet { get; set; } = "";
            public double Accuracy { get; set; }
        }
        public class TopDeckRow
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? ThemeColor { get; set; }
            public int TotalCards { get; set; }
            public int Mastered { get; set; }
        }
        public class CalendarCell
        {
            public DateTime Date { get; set; }
            public int Count { get; set; }
        }
    }
}
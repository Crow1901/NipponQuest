using System.Collections.Generic;
using System.Linq;
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
    public class LeaguesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        private static readonly string[] AllowedAlphabets =
            new[] { "hiragana", "katakana", "dakuten", "mixed" };

        private static readonly string[] AllowedDifficulties =
            new[] { "easy", "normal", "hard", "insanity" };

        public LeaguesController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────
        //  LEAGUE DASHBOARD
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var allUsers = await _userManager.Users.ToListAsync();
            int totalUsersCount = allUsers.Count;

            var globalOrderedList = allUsers
                .OrderByDescending(u => u.CurrentLeague)
                .ThenByDescending(u => u.Level)
                .ThenByDescending(u => u.TotalEXP)
                .ToList();

            var goldList = allUsers.OrderByDescending(u => u.Gold).ToList();

            var arenaList = allUsers
                .Where(u => u.CurrentLeague == currentUser.CurrentLeague)
                .OrderByDescending(u => u.WeeklyXP)
                .ThenByDescending(u => u.Level)
                .ThenByDescending(u => u.TotalEXP)
                .ToList();

            int globalRank = globalOrderedList.FindIndex(u => u.Id == currentUser.Id) + 1;
            int arenaRank = arenaList.FindIndex(u => u.Id == currentUser.Id) + 1;

            var vm = new LeagueDashboardViewModel
            {
                CurrentUser = currentUser,
                TotalUserCount = totalUsersCount,
                LevelLeaderboard = globalOrderedList.Take(3)
                    .Select((u, i) => new RankedUserSB { Rank = i + 1, User = u, IsCurrentUser = u.Id == currentUser.Id })
                    .ToList(),
                GoldLeaderboard = goldList.Take(3)
                    .Select((u, i) => new RankedUserSB { Rank = i + 1, User = u, IsCurrentUser = u.Id == currentUser.Id })
                    .ToList(),
                ArenaRivals = GetRivals(arenaList, currentUser.Id),
                ArenaRank = arenaRank,
                GlobalLevelRank = globalRank,
                ArenaRankChange = currentUser.LastWeekArenaRank > 0 ? currentUser.LastWeekArenaRank - arenaRank : 0,
                GlobalLevelChange = currentUser.LastWeekGlobalRank > 0 ? currentUser.LastWeekGlobalRank - globalRank : 0
            };

            return View(vm);
        }

        private List<RankedUserSB> GetRivals(List<ApplicationUser> list, string myId)
        {
            return list.Select(u => new RankedUserSB
            {
                Rank = list.IndexOf(u) + 1,
                User = u,
                IsCurrentUser = u.Id == myId
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────
        //  KANABLITZ LEADERBOARD
        //  Ranks players by DIFFICULTY (Easy / Normal / Hard / Insanity)
        //  on a fixed Mixed-kana baseline so everyone competes on the
        //  same kana pool — only the difficulty pace differentiates.
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Blitz() => View();

        [HttpGet]
        public async Task<IActionResult> GetBlitzBoard(string difficulty = "normal", int top = 50)
        {
            difficulty = (difficulty ?? "").ToLower();
            if (!AllowedDifficulties.Contains(difficulty))
                return BadRequest(new { error = "Unknown difficulty." });

            // Leaderboard is always measured against the Mixed kana set so
            // results are directly comparable between players.
            const string alphabet = "mixed";
            top = System.Math.Clamp(top, 10, 200);

            var rows = await _context.BlitzPersonalBests
                .AsNoTracking()
                .Where(p => p.Difficulty == difficulty && p.Alphabet == alphabet)
                .OrderByDescending(p => p.BestCorrect)
                .ThenByDescending(p => p.BestPoints)
                .ThenBy(p => p.UpdatedAt)
                .Take(top)
                .Select(p => new
                {
                    userId = p.ApplicationUserId,
                    name = (p.User != null && !string.IsNullOrEmpty(p.User.GamerTag))
                                       ? p.User.GamerTag
                                       : (p.User != null ? p.User.UserName : "Anonymous"),
                    bestCorrect = p.BestCorrect,
                    bestPoints = p.BestPoints,
                    bestCombo = p.BestCombo,
                    bestAccuracy = p.BestAccuracy,
                    updatedAt = p.UpdatedAt
                })
                .ToListAsync();

            var me = await _userManager.GetUserAsync(User);
            object? selfRow = null;
            int selfRank = 0;

            if (me != null)
            {
                var myPb = await _context.BlitzPersonalBests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ApplicationUserId == me.Id
                                           && p.Difficulty == difficulty
                                           && p.Alphabet == alphabet);

                if (myPb != null)
                {
                    selfRank = await _context.BlitzPersonalBests
                        .Where(p => p.Difficulty == difficulty && p.Alphabet == alphabet)
                        .CountAsync(p => p.BestCorrect > myPb.BestCorrect) + 1;

                    selfRow = new
                    {
                        userId = me.Id,
                        name = string.IsNullOrEmpty(me.GamerTag) ? me.UserName : me.GamerTag,
                        bestCorrect = myPb.BestCorrect,
                        bestPoints = myPb.BestPoints,
                        bestCombo = myPb.BestCombo,
                        bestAccuracy = myPb.BestAccuracy,
                        updatedAt = myPb.UpdatedAt,
                        rank = selfRank
                    };
                }
            }

            return Json(new
            {
                alphabet,
                difficulty,
                rows,
                self = selfRow
            });
        }
    }
}
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace NipponQuest.Models
{
    // ENUM for various ranks in the league system. This will help us easily manage and display league information.
    public enum LeagueRank { Sprout, Wood, Iron, Gold, Diamond, Master, Challenger, Dragon, Legend }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        public required string GamerTag { get; set; }
        public int Level { get; set; } = 1;
        public int TotalEXP { get; set; } = 0;
        public int CurrentXP { get; set; } = 0;
        public int Gold { get; set; } = 0;
        public int WeeklyXP { get; set; } = 0;
        public int LessonsCompleted { get; set; } = 0;
        public LeagueRank CurrentLeague { get; set; } = LeagueRank.Sprout;

        // --- RANK TRACKING FIELDS ---
        public int LastWeekGlobalRank { get; set; } = 0;
        public int LastWeekArenaRank { get; set; } = 0;

        public int LoginStreak { get; set; } = 0;
        public DateTime? LastLoginDate { get; set; }

        public int DailyStreak { get; set; } = 0;
        public DateTime LastPlayedDate { get; set; } = DateTime.MinValue;
        public int HighestBlitzScore { get; set; } = 0;
        public string? BlitzAccuracyJson { get; set; }

        public string LeagueIconClass => CurrentLeague.ToString().ToLower();
        public int RequiredXP => Level <= 10 ? Level * 50 + 50 : Level <= 50 ? Level * 100 : Level * 250;
        public int XPPercentage => RequiredXP <= 0 ? 0 : (int)((double)CurrentXP / RequiredXP * 100);
    }
}
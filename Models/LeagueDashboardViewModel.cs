using System.Collections.Generic;

namespace NipponQuest.Models
{
    public class LeagueDashboardViewModel
    {
        public ApplicationUser CurrentUser { get; set; }
        public List<RankedUserSB> LevelLeaderboard { get; set; }
        public List<RankedUserSB> GoldLeaderboard { get; set; }
        public List<RankedUserSB> ArenaRivals { get; set; }
        public int ArenaRank { get; set; }
        public int ArenaRankChange { get; set; }
        public int GlobalLevelRank { get; set; }
        public int GlobalLevelChange { get; set; }

        // Total count of users in the system
        public int TotalUserCount { get; set; }

        public int GlobalRank => GlobalLevelRank;
        public int GlobalRankChange => GlobalLevelChange;
        public List<RankedUserSB> TopGlobalPlayers => LevelLeaderboard;
    }

    public class RankedUserSB
    {
        public int Rank { get; set; }
        public ApplicationUser User { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}
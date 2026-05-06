using System.ComponentModel.DataAnnotations;

namespace NipponQuest.Models
{
    public class LeaderboardEntry
    {
        [Key]
        public int Id { get; set; }

        public string Username { get; set; } = "";
        public int Score { get; set; }
        public string Difficulty { get; set; } = "";
        public DateTime DateAchieved { get; set; } = DateTime.UtcNow;
    }
}
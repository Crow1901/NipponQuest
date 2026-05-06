using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NipponQuest.Models
{
    public class RewardLedger
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = "";

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? User { get; set; }

        public int ExpDelta { get; set; }
        public int GoldDelta { get; set; }

        // "kanablitz", "flashcards", "lesson", "dev", etc.
        [Required, MaxLength(40)]
        public string Source { get; set; } = "lesson";

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }
}
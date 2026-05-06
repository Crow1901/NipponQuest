using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NipponQuest.Models
{
    /// <summary>
    /// Per-user, per-script, per-difficulty personal best for KanaBlitz.
    /// Powers the leaderboard page and the post-run "rank delta" tile.
    /// </summary>
    public class BlitzPersonalBest
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = "";

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? User { get; set; }

        // "easy" | "normal" | "hard" | "insanity"
        [Required, MaxLength(16)]
        public string Difficulty { get; set; } = "";

        // "hiragana" | "katakana" | "dakuten" | "mixed"
        [Required, MaxLength(16)]
        public string Alphabet { get; set; } = "";

        /// <summary>Most kana the user has answered correctly in a single run for this combo.</summary>
        public int BestCorrect { get; set; }

        /// <summary>Highest score (points) ever earned in this combo.</summary>
        public int BestPoints { get; set; }

        /// <summary>Longest combo (max streak) ever in this combo.</summary>
        public int BestCombo { get; set; }

        /// <summary>Best accuracy (0..1) ever achieved in this combo over a meaningful run.</summary>
        public double BestAccuracy { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace NipponQuest.Models
{
    public class Deck
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public List<Flashcard> Flashcards { get; set; } = new List<Flashcard>();

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = false;
        public int Downloads { get; set; } = 0;
        public int Likes { get; set; } = 0;
        public int Dislikes { get; set; } = 0;
        public string? AuthorName { get; set; }

        // --- NEW: MARKET ECONOMY PROPERTIES ---
        [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10,000 Gold.")]
        public int Price { get; set; } = 0;

        // Anti-Plagiarism locks
        public bool IsCommunityClone { get; set; } = false;
        public int? ParentDeckId { get; set; }
        public string ThemeColor { get; set; } = "#ffffff"; // White by default
    }

    public class Flashcard
    {
        public int Id { get; set; }

        [Required]
        public int DeckId { get; set; }

        [ForeignKey("DeckId")]
        public Deck? Deck { get; set; }

        [Required]
        public string FrontText { get; set; } = string.Empty;

        [Required]
        public string BackText { get; set; } = string.Empty;

        public string? ImageFilePath { get; set; }
        public string? AudioFilePath { get; set; }

        public int SuccessCount { get; set; } = 0;
        public int Interval { get; set; } = 0;
        public double EaseFactor { get; set; } = 2.5;

        public DateTime NextReview { get; set; } = DateTime.UtcNow;
        public DateTime? LastReviewed { get; set; }
    }

    public class DeckVote
    {
        public int Id { get; set; }
        public int DeckId { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public bool IsUpvote { get; set; }
    }

    public class DeckViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CardCount { get; set; }
        public int DueCount { get; set; }
        public bool IsPublic { get; set; }
        public string? AuthorName { get; set; }
        public int? NewCount { get; set; }
        public int? LearningCount { get; set; }

        // --- NEW: Exposing Economy variables to views ---
        public int Price { get; set; }
        public bool IsCommunityClone { get; set; }

        // --- NEW: Theme Tracking ---
        public string ThemeColor { get; set; } = "#ffffff";

        // Updated Helper: Now relies on the strict structural database flag
        public bool IsCommunityDeck => IsCommunityClone || Description.StartsWith("(Community Deck)");
    }

    public class DeckPurchase
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public int DeckId { get; set; } // The ID of the original Market Board deck
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    }

    // Notice how the extra namespace wrapper is gone here!
    public class UserColorPurchase
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    }
}
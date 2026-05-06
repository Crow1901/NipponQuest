namespace NipponQuest.Models
{
    public class Hiragana
    {
        // Primary key for the Hiragana table. This will be auto-incremented by the database.
        public int Id { get; set; }
        public string Symbol { get; set; }     // e.g. "あ"
        public string Romaji { get; set; }     // e.g. "a"
        public string? StrokeOrderUrl { get; set; } // Link to an image/gif
        public int UnlockLevel { get; set; } = 1;  // Which level unlocks this row
    }
}

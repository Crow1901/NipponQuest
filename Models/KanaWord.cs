using System.ComponentModel.DataAnnotations;

namespace NipponQuest.Models
{
    public partial class KanaWord
    {
        [Key]
        public int Id { get; set; }

        public required string WordKana { get; set; }
        public required string WordRomaji { get; set; }
        public required string MeaningEnglish { get; set; }
        public required string Alphabet { get; set; }
        public required string DifficultyLevel { get; set; }
        public string? DisplayHtml { get; set; }
        public string? MissingKana { get; set; }

        // Optional contextual hint shown in the arena (e.g. category or usage note)
        public string? CategoryTag { get; set; }
    }
}
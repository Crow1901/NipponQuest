// Auto‑generated extension for KanaWord to include AI generation flags
using System;

namespace NipponQuest.Models
{
    // The original KanaWord entity is defined elsewhere – we extend it with a partial class.
    public partial class KanaWord
    {
        /// <summary>
        /// True when this entry was created by the AI generation pipeline.
        /// </summary>
        public bool IsAIGenerated { get; set; } = false;

        /// <summary>
        /// When the AI‑generated entry was inserted.
        /// </summary>
        public DateTime? GeneratedAt { get; set; }
    }
}
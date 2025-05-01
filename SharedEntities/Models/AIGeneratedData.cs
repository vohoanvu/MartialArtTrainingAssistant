using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
{
    public class AiTechniqueIdentification
    {
        [Key]
        public int Id { get; set; }
        public int AiAnalysisResultId { get; set; }
        [ForeignKey("AiAnalysisResultId")]
        public virtual AiAnalysisResult AiAnalysisResult { get; set; }
        public int TechniqueId { get; set; }
        [ForeignKey("TechniqueId")]
        public virtual Techniques Technique { get; set; }
        public TimeSpan? Timespan { get; set; }
        public string? FighterIdentifier { get; set; }
    }

    public class AiStrength
    {
        [Key]
        public int Id { get; set; }
        public int AiAnalysisResultId { get; set; }
        [ForeignKey("AiAnalysisResultId")]
        public virtual AiAnalysisResult AiAnalysisResult { get; set; }
        public string? FighterIdentifier { get; set; }
        public string? StrengthDescription { get; set; } // e.g., "Good hand positioning"
    }

    public class AiImprovementArea
    {
        [Key]
        public int Id { get; set; }
        public int AiAnalysisResultId { get; set; }
        [ForeignKey("AiAnalysisResultId")]
        public virtual AiAnalysisResult AiAnalysisResult { get; set; }
        public string? FighterIdentifier { get; set; }
        public string? ImprovementDescription { get; set; } // e.g., "Secure legs to prevent escape"
    }

    public class AiSuggestedDrill
    {
        [Key]
        public int Id { get; set; }
        public int AiAnalysisResultId { get; set; }
        [ForeignKey("AiAnalysisResultId")]
        public virtual AiAnalysisResult AiAnalysisResult { get; set; }
        public string? Name { get; set; } // e.g., "Leg Hook Drill"
        public string? Description { get; set; }
        public string? Focus { get; set; } // e.g., "Position Stabilization"
        public string? Duration { get; set; } // e.g., "2 minutes"
    }
}
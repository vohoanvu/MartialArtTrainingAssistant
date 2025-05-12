using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
{
    public enum TargetLevel
    {
        Kids,
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }

    public enum FocusModule
    {
        General,
        SelfDefense,
        Competition,
    }

    public class PositionalScenario
    {
        [Key]
        public int Id { get; set; }
    
        public required string Name { get; set; } //Example: "Standing Techniques", "Guard", "Mount", "Back", "Side Control"

        public FocusModule FocusModule { get; set; } = FocusModule.General;

        public TargetLevel TargetLevel { get; set; } = TargetLevel.Beginner;
    }

    public class TechniqueType 
    {
        [Key]
        public int Id { get; set; }

        public required string Name { get; set; } //Example: "Offensive", "Submission", "Defensive"

        public int PositionalScenarioId { get; set; }
        [ForeignKey("PositionalScenarioId")]
        public virtual PositionalScenario PositionalScenario { get; set; }
    }

    public class Techniques
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; } //Example: "Armbar", "Hip Escape"
        public string? Description { get; set; }

        
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual PointScoringTechnique? Category { get; set; }

        public int TechniqueTypeId { get; set; }
        [ForeignKey("TechniqueTypeId")]
        public TechniqueType TechniqueType { get; set; }


        public int? AiAnalysisResultId { get; set; }
        [ForeignKey("AiAnalysisResultId")]
        public virtual AiAnalysisResult? AiAnalysisResult { get; set; } 

        public int? VideoId { get; set; }
        [ForeignKey("VideoId")]
        public virtual VideoMetadata? Video { get; set; }
    }

    public class PointScoringTechnique
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; } //Example: "Knee on Belly"
        public string? Description { get; set; } //Example: "Hold the position using knee-on-belly for 3 seconds"
        public int Points { get; set; }
        public MartialArt MartialArt { get; set; } = MartialArt.BrazilianJiuJitsu_GI;
    }

    public enum MartialArt
    {
        None,
        BrazilianJiuJitsu_GI,
        BrazilianJiuJitsu_NO_GI,
        Wrestling,
        Boxing,
        MuayThai,
        Judo,
        Karate,
        Taekwondo,
        Kickboxing,
        Sumo,
    }

    public class Drills
    {
        [Key]
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }

        public string? Focus { get; set; }

        public string Duration { get; set; }

        public int TechniqueId { get; set; }
        [ForeignKey("TechniqueId")]
        public virtual Techniques Technique { get; set; }

        public int? AiAnalysisResultId { get; set; }
        [ForeignKey("AiAnalysisResultId")]
        public virtual AiAnalysisResult? AiAnalysisResult { get; set; } 

        public int? VideoId { get; set; }
        [ForeignKey("VideoId")]
        public virtual VideoMetadata? Video { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
{
    //Currently unused
    //Captures instructor feedback on specific video timestamps, including AI analysis for later fine-tuning.
    // public class InstructorFeedback
    // {
    //     public int Id { get; set; }

    //     public int AiAnalysisResultId { get; set; }
    //     [ForeignKey("AiAnalysisResultId")]
    //     public virtual AiAnalysisResult AiAnalysisResult { get; set; }

    //     public string InstructorId { get; set; }
    //     [ForeignKey("InstructorId")]
    //     public virtual AppUserEntity Instructor { get; set; }

    //     public TimeSpan? StartTimestamp { get; set; }
    //     public TimeSpan? EndTimestamp { get; set; }
    //     public string? FeedbackDescription { get; set; }
    //     public string? EditedTechniquesJson { get; set; }
    //     public string? EditedStrengths { get; set; }
    //     public string? EditedAreasForImprovement { get; set; }

    //     public string ChangedFieldsJson { get; set; } // e.g., {"Strengths": "Old value -> New value"}
    //     public DateTime ChangedAt { get; set; }
    // }

    // Linking a technique to a specific video, allowing for multiple techniques to be associated with a single video.
    public class VideoSegmentFeedback
    {
        [Key]
        public int Id { get; set; }

        public int VideoId { get; set; }
        public string? AnalysisJson { get; set; }
        public TimeSpan? StartTimestamp { get; set; }
        public TimeSpan? EndTimestamp { get; set; }

        public int TechniqueId { get; set; }
        [ForeignKey("TechniqueId")]
        public Techniques Technique { get; set; } //Assuming this is a reference to the technique being analyzed

        [ForeignKey("VideoId")]
        public virtual VideoMetadata Video { get; set; }
    }

    //Provides a comprehensive AI analysis of an entire video.
    //While similar to AiFeedback, this table is kept separate assuming it serves a distinct purpose (overall analysis vs. specific feedback)
    public class AiAnalysisResult
    {
        public int Id { get; set; }

        public int VideoId { get; set; }
        [ForeignKey("VideoId")]
        public virtual VideoMetadata Video { get; set; }

        public required string AnalysisJson { get; set; } //need to keep this JSON Blob for storing raw AI-generated data

        public virtual ICollection<Drills> Drills { get; set; } = [];
        public virtual ICollection<Techniques> Techniques { get; set; } = [];
        public string? OverallDescription { get; set; }
        public string Strengths { get; set; } //JSON blob
        public string AreasForImprovement { get; set; } //JSON blob

        public DateTime? GeneratedAt { get; set; }

        public DateTime? LastUpdatedAt { get; set; }

        public string? UpdatedBy { get; set; } //AppUser.Id
    }

    public class WeaknessCategory
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } // e.g., "Takedown Defense", "Guard Retention"
        public string? Description { get; set; }
    }

    public class AnalysisWeakness
    {
        [Key]
        public int Id { get; set; }

        public int AiAnalysisResultId { get; set; }
        [ForeignKey("AiAnalysisResultId")]
        public virtual AiAnalysisResult AiAnalysisResult { get; set; }

        public int WeaknessCategoryId { get; set; }
        [ForeignKey("WeaknessCategoryId")]
        public virtual WeaknessCategory WeaknessCategory { get; set; }
    }
}

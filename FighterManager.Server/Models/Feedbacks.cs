using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FighterManager.Server.Models
{
    // public class Feedback
    // {
    //     [Key]
    //     public int FeedbackId { get; set; }

    //     public int InstructorId { get; set; }
    //     [ForeignKey("InstructorId")]
    //     public virtual Instructor? Instructor { get; set; }

    //     public int ProgramId { get; set; }
    //     [ForeignKey("ProgramId")]
    //     public virtual TrainingPrograms? Program { get; set; }

    //     public int? TechniqueId { get; set; }
    //     [ForeignKey("TechniqueId")]
    //     public virtual Techniques? Technique { get; set; }

    //     public int? DrillId { get; set; }
    //     [ForeignKey("DrillId")]
    //     public virtual Drills? Drill { get; set; }

    //     public int? SessionId { get; set; }
    //     [ForeignKey("SessionId")]
    //     public virtual TrainingSession? Session { get; set; }

    //     public required FeedbackType FeedbackType { get; set; }

    //     [Range(1, 5)]
    //     public required int Rating { get; set; }

    //     public required string Comments { get; set; }

    //     public required DateTime FeedbackDate { get; set; }
    // }

    // public enum FeedbackType
    // {
    //     Technique,
    //     Drill
    // }

    //Captures instructor feedback on specific video timestamps, including AI analysis for later fine-tuning.
    public class HumanFeedback
    {
        [Key]
        public int Id { get; set; }

        public int VideoId { get; set; }
        public string InstructorId { get; set; }
        public double? Timestamp { get; set; }
        public string FeedbackText { get; set; }

        //JSON storing AI analysis at that timestamp (use JSONB in PostgreSQL).
        public string? AIAnalysisJson { get; set; }


        [ForeignKey("VideoId")]
        public virtual UploadedVideo Video { get; set; }

        [ForeignKey("InstructorId")]
        public virtual AppUserEntity Instructor { get; set; }
    }

    //Stores AI-generated feedback for specific aspects of a video.
    public class AiFeedback
    {
        [Key]
        public int Id { get; set; }

        public int VideoId { get; set; }
        public string AnalysisJson { get; set; }


        [ForeignKey("VideoId")]
        public virtual UploadedVideo Video { get; set; }
    }

    //Provides a comprehensive AI analysis of an entire video.
    //While similar to AiFeedback, this table is kept separate assuming it serves a distinct purpose (overall analysis vs. specific feedback)
    public class AiAnalysisResult
    {
        public int Id { get; set; }
        public int VideoId { get; set; }
        public string AnalysisJson { get; set; }

        [ForeignKey("VideoId")]
        public virtual UploadedVideo Video { get; set; }
    }
}

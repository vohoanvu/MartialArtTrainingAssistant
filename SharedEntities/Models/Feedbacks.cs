using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SharedEntities.Models
{
    //Captures instructor feedback on specific video timestamps, including AI analysis for later fine-tuning.
    public class HumanFeedback
    {
        [Key]
        public int Id { get; set; }

        public TimeSpan? StartTimestamp { get; set; }
        public TimeSpan? EndTimestamp { get; set; }

        public string? FeedbackText { get; set; }
        
        public int TechniqueId { get; set; }
        [ForeignKey("TechniqueId")]
        [JsonIgnore]
        public virtual Techniques Technique { get; set; }

        public int VideoId { get; set; }
        [ForeignKey("VideoId")]
        [JsonIgnore]
        public virtual UploadedVideo Video { get; set; }

        public string InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        [JsonIgnore]
        public virtual AppUserEntity Instructor { get; set; }

        public int? AiFeedbackId { get; set; }
        [ForeignKey("AiFeedbackId")]
        [JsonIgnore]
        public virtual AiFeedback? AiFeedback { get; set; }
    }

    // AI-generated insights on video content, including timestamps and types of feedback.
    public class AiFeedback
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
        public virtual UploadedVideo Video { get; set; }
    }

    //Provides a comprehensive AI analysis of an entire video.
    //While similar to AiFeedback, this table is kept separate assuming it serves a distinct purpose (overall analysis vs. specific feedback)
    public class AiAnalysisResult
    {
        public int Id { get; set; }

        public int VideoId { get; set; }
        [ForeignKey("VideoId")]
        public virtual UploadedVideo Video { get; set; }

        public required string AnalysisJson { get; set; } //need to keep this JSON Blob for storing AI-generated data

        public virtual List<Drills>? Drills { get; set; }
    }
}

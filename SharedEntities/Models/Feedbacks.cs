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

        public string? Description { get; set; }
        
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

        [JsonIgnore]
        public virtual AiFeedback? AiFeedback { get; set; }
    }

    // Linking a technique to a specific video, allowing for multiple techniques to be associated with a single video.
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

        public virtual ICollection<Drills> Drills { get; set; } = [];
        public virtual ICollection<Techniques> Techniques { get; set; } = [];
        public string? OverallDescription { get; set; }
        public string Strengths { get; set; } //JSON blob
        public string AreasForImprovement { get; set; } //JSON blob
    }
}

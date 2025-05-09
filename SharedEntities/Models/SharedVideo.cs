using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
{
    //For storing metadata about YOUTUBE videos shared by users.
    public class SharedVideo
    {
        [Key]
        public int Id { get; set; }

        public required string Title { get; set; }

        public required string Description { get; set; }

        public required string Url { get; set; }

        public required string VideoId { get; set; }

        public required DateTime DateShared { get; set; }


        public required string UserId { get; set; }
        // Navigation property for the user who shared the video
        [ForeignKey(nameof(UserId))]
        public virtual AppUserEntity SharedBy { get; set; }
    }

    public class UploadedVideo
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
        public int Duration { get; set; }
        public string? Description { get; set; }
        public string? AISummary { get; set; }
        public string StudentIdentifier { get; set; } // Used for LLM Prompt parsing, e.g., "Fighter in blue gi"
        public MartialArt MartialArt { get; set; } = MartialArt.BrazilianJiuJitsu_GI;
        [ForeignKey("UserId")]
        public virtual AppUserEntity AppUser { get; set; }
        public string FileHash { get; set; }

        public virtual ICollection<Techniques> Techniques { get; set; } = [];
        public virtual ICollection<Drills> Drills { get; set; } = [];
    }

    //Stores instructor-uploaded videos demonstrating techniques.
    //This table is distinct from UploadedVideo as it’s for instructor reference content. 
    //TechniqueTag remains a string but could reference a future Techniques table if predefined tags are needed.
    public class Demonstration
    {
        public int Id { get; set; }
        public string InstructorId { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadTimestamp { get; set; }
        public string? Description { get; set; }
        public string? TechniqueTag { get; set; }

        [ForeignKey("InstructorId")]
        public virtual AppUserEntity Instructor { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
{
    public enum VideoType
    {
        Shared,        // YouTube videos shared by users
        StudentUpload, // Student sparring videos for AI analysis
        Demonstration  // Instructor reference videos
    }

    public class VideoMetadata
    {
        [Key]
        public int Id { get; set; }

        public required string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUserEntity AppUser { get; set; }

        public required VideoType Type { get; set; } // Differentiates Shared, StudentUpload, Demonstration

        public string? Title { get; set; } // For Shared videos
        public string? Description { get; set; } // Shared, StudentUpload, Demonstration
        public string? Url { get; set; } // YouTube URL for Shared
        public string? YoutubeVideoId { get; set; } // YouTube ID for Shared
        public string? FilePath { get; set; } // GCS path for StudentUpload, Demonstration
        public string? FileHash { get; set; } // For StudentUpload, Demonstration
        public DateTime UploadedAt { get; set; } // Shared (DateShared), StudentUpload, Demonstration
        public int? Duration { get; set; } // StudentUpload
        public string? StudentIdentifier { get; set; } // StudentUpload (e.g., "Fighter in blue gi")
        public MartialArt MartialArt { get; set; } = MartialArt.BrazilianJiuJitsu_GI; // StudentUpload
        public string? AISummary { get; set; } // StudentUpload
        public string? TechniqueTag { get; set; } // Demonstration
    }
}

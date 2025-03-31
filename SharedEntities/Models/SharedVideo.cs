using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
{
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

        //Path to the video in Google Cloud Storage (e.g., gs://bucket-name/video-id.mp4).
        public string FilePath { get; set; }
        public DateTime UploadTimestamp { get; set; }
        public string? Description { get; set; }
        public string? AISummary { get; set; }


        [ForeignKey("UserId")]
        public virtual AppUserEntity AppUser { get; set; }
    }

    //Stores instructor-uploaded videos demonstrating techniques.
    //This table is distinct from UploadedVideo as it’s for instructor reference content. 
    //TechniqueTag remains a string but could reference a future Techniques table if predefined tags are needed.
    public class Demonstration
    {
        public int Id { get; set; }
        public string InstructorId { get; set; }
        public string FilePath { get; set; }
        public string? Description { get; set; }
        public string TechniqueTag { get; set; }

        [ForeignKey("InstructorId")]
        public virtual AppUserEntity Instructor { get; set; }
    }
}

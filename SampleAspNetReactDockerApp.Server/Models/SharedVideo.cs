using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SampleAspNetReactDockerApp.Server.Models
{
    public class SharedVideo
    {
        [Key]
        public required int Id { get; set; }

        public required string Title { get; set; }

        public required string Description { get; set; }

        public required string Url { get; set; }

        public required DateTime DateShared { get; set; }


        public required int UserId { get; set; }
        // Navigation property for the user who shared the video
        [ForeignKey(nameof(UserId))]
        public virtual AppUserEntity SharedBy { get; set; }
    }
}

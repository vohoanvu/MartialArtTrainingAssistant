using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FighterManager.Server.Models
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
}

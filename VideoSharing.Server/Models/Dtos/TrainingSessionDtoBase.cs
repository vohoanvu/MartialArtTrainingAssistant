using System.ComponentModel.DataAnnotations;

namespace VideoSharing.Server.Models.Dtos
{
     public class TrainingSessionDtoBase
    {
        [Required]
        public DateTime TrainingDate { get; set; }

        public string? Description { get; set; }

        [Required]
        public int Capacity { get; set; }

        [Required]
        public double Duration { get; set; } // in hours

        [Required]
        public string Status { get; set; } // Assuming SessionStatus is an enum, use string for flexibility

        public int? InstructorId { get; set; }

        public List<int>? StudentIds { get; set; } = new List<int>(); // List of Fighter IDs
    }
}

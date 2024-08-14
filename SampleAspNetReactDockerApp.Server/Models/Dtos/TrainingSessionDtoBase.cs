using System.ComponentModel.DataAnnotations;

namespace SampleAspNetReactDockerApp.Server.Models.Dtos
{
     public class TrainingSessionDtoBase
    {
        public int? Id { get; set; }

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

        public List<int>? StudentIds { get; set; } = []; // List of Fighter IDs
    }

    public class GetSessionDetailResponse : TrainingSessionDtoBase
    {
        public int Id { get; set; }

        public ViewFighterDto Instructor { get; set; }

        public List<ViewFighterDto> Students { get; set; }
    }
}

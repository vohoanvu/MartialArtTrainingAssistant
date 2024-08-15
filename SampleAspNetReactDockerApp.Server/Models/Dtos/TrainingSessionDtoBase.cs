using System.ComponentModel.DataAnnotations;

namespace SampleAspNetReactDockerApp.Server.Models.Dtos
{
     public class TrainingSessionDtoBase
    {
        public int? Id { get; set; }

        public DateTime? TrainingDate { get; set; }

        public string? Description { get; set; }

        public int? Capacity { get; set; }

        public double? Duration { get; set; } // in hours

        public string? Status { get; set; } // Assuming SessionStatus is an enum, use string for flexibility

        public int? InstructorId { get; set; }

        public List<int>? StudentIds { get; set; } = []; // List of Fighter IDs
    }

    public class GetSessionDetailResponse : TrainingSessionDtoBase
    {
        public int Id { get; set; }

        public ViewFighterDto Instructor { get; set; }

        public List<ViewFighterDto> Students { get; set; }
    }

    public class UpdateSessionDetailsRequest : TrainingSessionDtoBase 
    {

    }
}

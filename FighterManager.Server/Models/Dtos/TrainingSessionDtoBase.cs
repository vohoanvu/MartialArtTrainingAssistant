using SharedEntities.Models;

namespace FighterManager.Server.Models.Dtos
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
        public required ViewFighterDto Instructor { get; set; }

        public List<ViewFighterDto> Students { get; set; } = [];
    }

    public class UpdateSessionDetailsRequest : TrainingSessionDtoBase 
    {

    }

    public class AttendanceRecordDto
    {
        [Required]
        [StringLength(100)]
        public required string FighterName { get; set; }

        [Required]
        public DateTime Birthdate { get; set; }

        [Range(0, 500)] // Add reasonable limits
        public double Weight { get; set; }

        [Range(0, 300)] // Add reasonable limits
        public double Height { get; set; }

        [Required]
        public BeltColor BeltColor { get; set; }

        [Required]
        public Gender Gender { get; set; }
    }

    public class TakeAttendanceRequest
    {
        public List<AttendanceRecordDto> Records { get; set; } = [];
    }

    public class TakeAttendanceResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public GetSessionDetailResponse? UpdatedSession { get; set; }
    }
}

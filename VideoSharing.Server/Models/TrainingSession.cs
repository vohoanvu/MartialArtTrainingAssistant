using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VideoSharing.Server.Models
{
    public class TrainingSession
    {
        [Key]
        public int Id { get; set; }

        public string? Description { get; set; }

        public required DateTime TrainingDate { get; set; }

        public required int Capacity { get; set; }

        public required double Duration { get; set; } // in hours

        public required SessionStatus Status { get; set; }

        public int InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public virtual Fighter? Instructor { get; set; }

        public virtual List<TrainingSessionFighterJoint>? Students { get; set; }
    }

    public class TrainingSessionFighterJoint
    {
        [Key]
        public int Id { get; set; }

        public int TrainingSessionId { get; set; }
        [ForeignKey("TrainingSessionId")]
        public TrainingSession? TrainingSession { get; set; }

        public int FighterId { get; set; }
        [ForeignKey("FighterId")]
        public Fighter? Fighter { get; set; }
    }
}

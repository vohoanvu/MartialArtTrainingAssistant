using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FighterManager.Server.Models
{
    public class TrainingSession
    {
        [Key]
        public int Id { get; set; }

        public string? SessionNotes { get; set; }

        public required DateTime TrainingDate { get; set; }

        public required int Capacity { get; set; }

        public required double Duration { get; set; } // in hours

        public required SessionStatus Status { get; set; }

        public required SessionType SessionType { get; set; }

        public required TargetLevel TargetLevel { get; set; }

        public string? Warmup { get; set; } // JSONB in PostgreSQL

        public string? Cooldown { get; set; } // JSONB in PostgreSQL

        public int InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public virtual Fighter? Instructor { get; set; }

        public virtual List<TrainingSessionFighterJoint>? Students { get; set; }
    }

    public enum SessionType
    {
        Fundamentals,
        Submissions,
        SelfDefense,
        Sparring,
        OpenMat
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

    public class TrainingPrograms
    {
        [Key]
        public int Id { get; set; }

        public required string Name { get; set; }
        
        public TargetLevel TargetLevel { get; set; }
    }

    public enum TargetLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }
}

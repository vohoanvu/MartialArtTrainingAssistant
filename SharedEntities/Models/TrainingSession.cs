using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
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
        public required TargetLevel TargetLevel { get; set; }

        public int InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public virtual Fighter? Instructor { get; set; }

        //Tracking Student attendance
        public virtual List<TrainingSessionFighterJoint>? Students { get; set; }
        //Activities for the session
        public virtual List<TrainingSessionTechniqueJoint>? SessionContents { get; set; }

        public MartialArt MartialArt { get; set; } = MartialArt.BrazilianJiuJitsu_GI;

        public virtual ICollection<Drills>? RecommendedDrills { get; set; }

        public string? RawCurriculumJson { get; set; }

        public string? EditedCurriculumJson { get; set; }

        public string? RawFighterPairsJson { get; set; }

        public string? EditedFighterPairsJson { get; set; }

        // public bool IsHelpful { get; set; }
        // public string? HelpfulComment { get; set; }
    }

    public class TrainingSessionTechniqueJoint
    {
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }
        [ForeignKey("SessionId")]
        public virtual TrainingSession Session { get; set; }

        public int TechniqueId { get; set; }
        [ForeignKey("TechniqueId")]
        public virtual Techniques Technique { get; set; }

        public int? DrillId { get; set; }
        [ForeignKey("DrillId")]
        public virtual Drills? Drill { get; set; }

        public int? DurationMinutes { get; set; } // e.g., 20 minutes for a technique
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FighterManager.Server.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }

        public int InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public virtual Instructor? Instructor { get; set; }

        public int ProgramId { get; set; }
        [ForeignKey("ProgramId")]
        public virtual TrainingPrograms? Program { get; set; }

        public int? TechniqueId { get; set; }
        [ForeignKey("TechniqueId")]
        public virtual Techniques? Technique { get; set; }

        public int? DrillId { get; set; }
        [ForeignKey("DrillId")]
        public virtual Drills? Drill { get; set; }

        public int? SessionId { get; set; }
        [ForeignKey("SessionId")]
        public virtual TrainingSession? Session { get; set; }

        public required FeedbackType FeedbackType { get; set; }

        [Range(1, 5)]
        public required int Rating { get; set; }

        public required string Comments { get; set; }

        public required DateTime FeedbackDate { get; set; }
    }

    public enum FeedbackType
    {
        Technique,
        Drill
    }
}

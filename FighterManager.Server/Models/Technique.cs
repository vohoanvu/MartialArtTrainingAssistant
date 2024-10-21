using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FighterManager.Server.Models
{
    public class Techniques
    {
        [Key]
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }
    }

    public class Drills
    {
        [Key]
        public int Id { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }
    }

    public class SessionContent
    {
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }
        [ForeignKey("SessionId")]
        public virtual TrainingSession? Session { get; set; }

        public int? TechniqueId { get; set; }
        [ForeignKey("TechniqueId")]
        public virtual Techniques? Technique { get; set; }

        public int? DrillId { get; set; }
        [ForeignKey("DrillId")]
        public virtual Drills? Drill { get; set; }

        public int? DurationMinutes { get; set; }
    }
}

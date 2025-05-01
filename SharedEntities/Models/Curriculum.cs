using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
{
    //Define a Curriculum cycle for a specific target level and focus module
    public class Curriculum
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; } // e.g., "Beginner Competition Cycle 2024"
        public TargetLevel Level { get; set; }    // e.g., Beginner
        public FocusModule Module { get; set; }   // e.g., Competition
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual List<CurriculumScenario> Scenarios { get; set; } //Linking weekly themes to the curriculum
    }

    //This table assigns weekly themes by linking positional scenarios (e.g., Guard, Mount) to specific weeks within the curriculum
    public class CurriculumScenario
    {
        [Key]
        public int Id { get; set; }

        public int CurriculumId { get; set; }
        [ForeignKey("CurriculumId")]
        public virtual Curriculum Curriculum { get; set; }

        public int PositionalScenarioId { get; set; }
        [ForeignKey("PositionalScenarioId")]
        public virtual PositionalScenario PositionalScenario { get; set; }

        public int WeekNumber { get; set; } // e.g., Week 1 for Guard
    }
}
using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Models
{
    public class Fighter
    {
        [Key]
        public int Id { get; set; }


        public required string FighterName { get; set; }

        public required double Height { get; set; }      //in centimeters

        public required double Weight { get; set; }      //in kilograms

        public required double BMI { get; set; }

        public required Gender Gender { get; set; }

        public required FighterRole Role { get; set; }

        public DateTime Birthdate { get; set; }

        public required int MaxWorkoutDuration { get; set; } //how long can you spar without taking a break (in minutes)?

        public TrainingExperience Experience { get; set; }

        public BeltColor BelkRank { get; set; }
        public bool IsWalkIn { get; set; } = false;


        public virtual List<TrainingSessionFighterJoint>? TrainingSessions { get; set; }
    }
}

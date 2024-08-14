using System.ComponentModel.DataAnnotations;

namespace MatchMaker.Server.Models
{
    public class Fighter
    {
        [Key]
        public int Id { get; set; }


        public required string FighterName { get; set; }

        public required double Height { get; set; }      //in ft

        public required double Weight { get; set; }      //in lbs

        public required double BMI { get; set; }

        public required Gender Gender { get; set; }

        public required FighterRole Role { get; set; }

        public DateTime Birthdate { get; set; }

        public required int MaxWorkoutDuration { get; set; } //how long can you spar without taking a break (in minutes)?

        public TrainingExperience Experience { get; set; }

        public BeltColor BelkRank { get; set; }


        public virtual List<TrainingSessionFighterJoint>? TrainingSessions { get; set; } 
    }
}
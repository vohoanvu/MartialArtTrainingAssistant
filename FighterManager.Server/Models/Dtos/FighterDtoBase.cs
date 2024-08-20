using System.ComponentModel.DataAnnotations;

namespace FighterManager.Server.Models.Dtos
{
    public class FighterDtoBase
    {
        [Display(Name = "Fighter nickname")]
        public string FighterName { get; set; }

        public required double Height { get; set; }

        public required double Weight { get; set; }

        public double? BMI { get; set; }

        public required string Gender { get; set; }

        public DateTime Birthdate { get; set; }

        public required string FighterRole { get; set; }

        public required int MaxWorkoutDuration { get; set; }

        public required string BeltColor { get; set; }

        public required TrainingExperience Experience { get; set; }
    }
}

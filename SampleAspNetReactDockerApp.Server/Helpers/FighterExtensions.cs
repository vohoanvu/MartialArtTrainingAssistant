using SampleAspNetReactDockerApp.Server.Models;
using SampleAspNetReactDockerApp.Server.Models.Dtos;

namespace SampleAspNetReactDockerApp.Server.Helpers
{
    public static class FighterExtensions
    {
        public static void Update(this Fighter destination, UpdateFighterDto source)
        {
            destination.FighterName = source.FighterName;
            destination.Gender = Enum.Parse<Gender>(source.Gender);
            destination.Role = Enum.Parse<FighterRole>(source.FighterRole);
            destination.BMI = source.Weight / (source.Height * source.Weight);
            destination.Height = source.Height;
            destination.Weight = source.Weight;
            destination.MaxWorkoutDuration = source.MaxWorkoutDuration;
            destination.Birthdate = source.Birthdate;
        }
    }
}

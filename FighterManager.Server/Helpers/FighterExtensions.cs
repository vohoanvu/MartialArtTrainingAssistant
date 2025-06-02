using SharedEntities.Data;
using FighterManager.Server.Models.Dtos;
using SharedEntities.Models;

namespace FighterManager.Server.Helpers
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

    public static class TrainingSessionExtensions
    {
        public static void Update(this TrainingSession destination, UpdateSessionDetailsRequest source, MyDatabaseContext context)
        {
            if (source.Description != null)
            {
                destination.SessionNotes = source.Description;
            }

            if (source.TrainingDate.HasValue)
            {
                destination.TrainingDate = DateTime.SpecifyKind(source.TrainingDate.Value, DateTimeKind.Utc);
            }

            if (source.Capacity.HasValue)
            {
                destination.Capacity = source.Capacity.Value;
            }

            if (source.Duration.HasValue)
            {
                destination.Duration = source.Duration.Value;
            }

            if (!string.IsNullOrEmpty(source.TargetLevel))
            {
                destination.TargetLevel = Enum.Parse<TargetLevel>(source.TargetLevel);
            }

            if (!string.IsNullOrEmpty(source.Status))
            {
                destination.Status = Enum.Parse<SessionStatus>(source.Status);
            }

            if (source.InstructorId.HasValue)
            {
                destination.InstructorId = source.InstructorId.Value;
            }

            if (source.StudentIds != null && source.StudentIds.Count > 0)
            {
                // Ensure no duplicates by filtering out existing student IDs
                var existingStudentIds = destination.Students?.Select(s => s.FighterId).ToList() ?? new List<int>();

                var newStudentIds = source.StudentIds.Where(id => !existingStudentIds.Contains(id)).ToList();

                foreach (var studentId in newStudentIds)
                {
                    var studentFighter = context.Fighters.Find(studentId);
                    if (studentFighter != null)
                    {
                        destination.Students ??= new List<TrainingSessionFighterJoint>();
                        destination.Students.Add(new TrainingSessionFighterJoint
                        {
                            FighterId = studentId,
                            TrainingSessionId = destination.Id
                        });
                    }
                }
            }
        }
    }
}

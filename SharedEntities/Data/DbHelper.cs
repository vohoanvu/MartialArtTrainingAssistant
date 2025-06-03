using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedEntities.Models;

namespace SharedEntities.Data
{
    public static class DbHelper
    {
        public static async Task EnsureDbIsCreatedAndSeededAsync(AsyncServiceScope scope, bool deleteIfExists = false)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUserEntity>>();

            if (deleteIfExists) await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();

            await SeedDatabaseAsync(dbContext, userManager);
        }
        private static async Task SeedDatabaseAsync(MyDatabaseContext dbContext, UserManager<AppUserEntity> userManager)
        {
            // Seed Users and Fighters
            if (!await dbContext.Users.AnyAsync())
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Seed Student (existing)
                    var studentFighter = new Fighter
                    {
                        FighterName = "Test Student 0",
                        Height = 165,
                        Weight = 60,
                        BMI = 60 / (165/100 * (165/100)),
                        Gender = Gender.Male,
                        Birthdate = new DateTime(1996, 3, 19, 0, 0, 0, DateTimeKind.Utc),
                        MaxWorkoutDuration = 5,
                        Experience = TrainingExperience.LessThanTwoYears,
                        BelkRank = BeltColor.White,
                        Role = FighterRole.Student
                    };
                    dbContext.Fighters.Add(studentFighter);
                    await dbContext.SaveChangesAsync();

                    var studentUser = new AppUserEntity
                    {
                        UserName = "test-student-0@gmail.com",
                        Email = "test-student-0@gmail.com",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        FighterId = studentFighter.Id
                    };
                    var studentResult = await userManager.CreateAsync(studentUser, "qW123456*");
                    if (!studentResult.Succeeded) throw new Exception("Failed to seed student user: " + string.Join(", ", studentResult.Errors.Select(e => e.Description)));

                    // Seed Instructor (existing)
                    var instructorFighter = new Fighter
                    {
                        FighterName = "Test Instructor 0",
                        Height = 177,
                        Weight = 71,
                        BMI = 71 / (177/100 * (177/100)),
                        Gender = Gender.Male,
                        Birthdate = new DateTime(1988, 3, 19, 0, 0, 0, DateTimeKind.Utc),
                        MaxWorkoutDuration = 30,
                        Experience = TrainingExperience.MoreThanFiveYears,
                        BelkRank = BeltColor.Black,
                        Role = FighterRole.Instructor
                    };
                    dbContext.Fighters.Add(instructorFighter);
                    await dbContext.SaveChangesAsync();

                    var instructorUser = new AppUserEntity
                    {
                        UserName = "test-instructor-0@gmail.com",
                        Email = "test-instructor-0@gmail.com",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        FighterId = instructorFighter.Id
                    };
                    var instructorResult = await userManager.CreateAsync(instructorUser, "qW123456*");
                    if (!instructorResult.Succeeded) throw new Exception("Failed to seed instructor user: " + string.Join(", ", instructorResult.Errors.Select(e => e.Description)));

                    // Seed Additional Students
                    var additionalStudents = new List<(Fighter Fighter, AppUserEntity User)>
                    {
                        // Student 1: Small, Beginner Female
                        (
                            new Fighter
                            {
                                FighterName = "Test Student 1",
                                Height = 147,
                                Weight = 50,
                                BMI = 50 / (147/100 * (147/100)),
                                Gender = Gender.Female,
                                Birthdate = new DateTime(2000, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                                MaxWorkoutDuration = 4,
                                Experience = TrainingExperience.LessThanTwoYears,
                                BelkRank = BeltColor.White,
                                Role = FighterRole.Student
                            },
                            new AppUserEntity
                            {
                                UserName = "test-student-1@gmail.com",
                                Email = "test-student-1@gmail.com",
                                EmailConfirmed = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            }
                        ),
                        // Student 2: Medium, Intermediate Male
                        (
                            new Fighter
                            {
                                FighterName = "Test Student 2",
                                Height = 171,
                                Weight = 75,
                                BMI = 75 / (171/100 * (171/100)),
                                Gender = Gender.Male,
                                Birthdate = new DateTime(1995, 11, 22, 0, 0, 0, DateTimeKind.Utc),
                                MaxWorkoutDuration = 10,
                                Experience = TrainingExperience.FromTwoToFiveYears,
                                BelkRank = BeltColor.Blue,
                                Role = FighterRole.Student
                            },
                            new AppUserEntity
                            {
                                UserName = "test-student-2@gmail.com",
                                Email = "test-student-2@gmail.com",
                                EmailConfirmed = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            }
                        ),
                        // Student 3: Large, Advanced Female
                        (
                            new Fighter
                            {
                                FighterName = "Test Student 3",
                                Height = 183,
                                Weight = 91,
                                BMI = 91 / (183/100 * (183/100)),
                                Gender = Gender.Female,
                                Birthdate = new DateTime(1990, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                                MaxWorkoutDuration = 20,
                                Experience = TrainingExperience.MoreThanFiveYears,
                                BelkRank = BeltColor.Brown,
                                Role = FighterRole.Student
                            },
                            new AppUserEntity
                            {
                                UserName = "test-student-3@gmail.com",
                                Email = "test-student-3@gmail.com",
                                EmailConfirmed = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            }
                        ),
                        // Student 4: Medium, Beginner Male
                        (
                            new Fighter
                            {
                                FighterName = "Test Student 4",
                                Height = 180,
                                Weight = 79,
                                BMI = 79 / (180/100 * (180/100)),
                                Gender = Gender.Male,
                                Birthdate = new DateTime(1998, 9, 5, 0, 0, 0, DateTimeKind.Utc),
                                MaxWorkoutDuration = 6,
                                Experience = TrainingExperience.LessThanTwoYears,
                                BelkRank = BeltColor.White,
                                Role = FighterRole.Student
                            },
                            new AppUserEntity
                            {
                                UserName = "test-student-4@gmail.com",
                                Email = "test-student-4@gmail.com",
                                EmailConfirmed = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            }
                        ),
                        // Student 5: Small, Intermediate Female
                        (
                            new Fighter
                            {
                                FighterName = "Test Student 5",
                                Height = 158,
                                Weight = 57,
                                BMI = 57 / (158/100 * (158/100)),
                                Gender = Gender.Female,
                                Birthdate = new DateTime(1997, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                                MaxWorkoutDuration = 8,
                                Experience = TrainingExperience.FromTwoToFiveYears,
                                BelkRank = BeltColor.Purple,
                                Role = FighterRole.Student
                            },
                            new AppUserEntity
                            {
                                UserName = "test-student-5@gmail.com",
                                Email = "test-student-5@gmail.com",
                                EmailConfirmed = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            }
                        )
                    };

                    // Add additional students
                    foreach (var (fighter, user) in additionalStudents)
                    {
                        dbContext.Fighters.Add(fighter);
                        await dbContext.SaveChangesAsync(); // Save Fighter to get Id

                        user.FighterId = fighter.Id;
                        var result = await userManager.CreateAsync(user, "qW123456*");
                        if (!result.Succeeded) throw new Exception($"Failed to seed student user {user.Email}: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine("Seeded initial Student and Instructor records with associated Fighters.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Seeding failed: {ex.Message}");
                    throw;
                }
            }

            if (!dbContext.PointScoringTechniques.Any())
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Define technique categories based on IBJJF rules
                    var techniques = new List<PointScoringTechnique>
                    {
                        // GI Competition Categories
                        new() { Name = "Takedown", Points = 2, MartialArt = MartialArt.BrazilianJiuJitsu_GI },
                        new() { Name = "Sweep", Points = 2, MartialArt = MartialArt.BrazilianJiuJitsu_GI },
                        new() { Name = "Guard Pass", Points = 3, MartialArt = MartialArt.BrazilianJiuJitsu_GI },
                        new() { Name = "Knee on Belly", Points = 2, MartialArt = MartialArt.BrazilianJiuJitsu_GI },
                        new() { Name = "Mount", Points = 4, MartialArt = MartialArt.BrazilianJiuJitsu_GI },
                        new() { Name = "Back Control", Points = 4, MartialArt = MartialArt.BrazilianJiuJitsu_GI },
                        new() { Name = "Back Mount", Points = 4, MartialArt = MartialArt.BrazilianJiuJitsu_GI },
                        new() { Name = "Submission", Points = 0, MartialArt = MartialArt.BrazilianJiuJitsu_GI },

                        // NO GI Competition Categories
                        new() { Name = "Takedown", Points = 2, MartialArt = MartialArt.BrazilianJiuJitsu_NO_GI },
                        new() { Name = "Sweep", Points = 2, MartialArt = MartialArt.BrazilianJiuJitsu_NO_GI },
                        new() { Name = "Guard Pass", Points = 3, MartialArt = MartialArt.BrazilianJiuJitsu_NO_GI },
                        new() { Name = "Knee on Belly", Points = 2, MartialArt = MartialArt.BrazilianJiuJitsu_NO_GI },
                        new() { Name = "Mount", Points = 4, MartialArt = MartialArt.BrazilianJiuJitsu_NO_GI },
                        new() { Name = "Back Control", Points = 4, MartialArt = MartialArt.BrazilianJiuJitsu_NO_GI },
                        new() { Name = "Submission", Points = 0, MartialArt = MartialArt.BrazilianJiuJitsu_NO_GI },
                    };

                    // Add categories to the database and save changes
                    dbContext.PointScoringTechniques.AddRange(techniques);
                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    Console.WriteLine("Seeded Point-Scoring rule.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Seeding Point-Scoring rule failed: {ex.Message}");
                    throw;
                }
            }

            // Seed PositionalScenario
            if (!dbContext.PositionalScenarios.Any())
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    var positionalScenarios = new List<PositionalScenario>
                    {
                        new() { Name = "Standing", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Beginner },
                        new() { Name = "Guard", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Beginner },
                        new() { Name = "Half Guard", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Intermediate },
                        new() { Name = "Side Control", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Beginner },
                        new() { Name = "Mount", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Beginner },
                        new() { Name = "Back Control", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Intermediate },
                        new() { Name = "Knee on Belly", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Intermediate },
                        new() { Name = "Turtle", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Intermediate }
                    };

                    dbContext.PositionalScenarios.AddRange(positionalScenarios);
                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    Console.WriteLine("Seeded PositionalScenario data.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Seeding PositionalScenario data failed: {ex.Message}");
                    throw;
                }
            }

            // Seed TechniqueType
            if (!dbContext.TechniqueTypes.Any())
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Fetch PositionalScenarios to link with TechniqueTypes
                    var positionalScenarios = await dbContext.PositionalScenarios.ToListAsync();
                    var standing = positionalScenarios.FirstOrDefault(ps => ps.Name == "Standing");
                    var guard = positionalScenarios.FirstOrDefault(ps => ps.Name == "Guard");
                    var generic = positionalScenarios.FirstOrDefault(ps => ps.Name == "Generic") ?? new PositionalScenario { Name = "Generic", FocusModule = FocusModule.General, TargetLevel = TargetLevel.Beginner };

                    var techniqueTypes = new List<TechniqueType>
                    {
                        new() { Name = "Takedown", PositionalScenario = standing ?? generic },
                        new() { Name = "Submission", PositionalScenario = guard ?? generic },
                        new() { Name = "Sweep", PositionalScenario = guard ?? generic },
                        new() { Name = "Pass", PositionalScenario = guard ?? generic },
                        new() { Name = "Escape", PositionalScenario = generic },
                        new() { Name = "Transition", PositionalScenario = generic },
                        new() { Name = "Control", PositionalScenario = generic },
                        new() { Name = "Defense", PositionalScenario = generic }
                    };

                    dbContext.TechniqueTypes.AddRange(techniqueTypes);
                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    Console.WriteLine("Seeded TechniqueType data.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Seeding TechniqueType data failed: {ex.Message}");
                    throw;
                }
            }

            if (!dbContext.Techniques.Any())
            {
                var defaultTechniques = new Techniques()
                {
                    Name = "Generic",
                    TechniqueType = new()
                    {
                        Name = "Conditioning",
                        PositionalScenarioId = dbContext.PositionalScenarios.First(ps => ps.Name == "Standing").Id
                    },
                };
                dbContext.Techniques.Add(defaultTechniques);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
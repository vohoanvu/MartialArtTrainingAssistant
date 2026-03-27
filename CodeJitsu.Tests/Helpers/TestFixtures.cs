using SharedEntities.Models;

namespace CodeJitsu.Tests.Helpers;

/// <summary>
/// Static factory methods for constructing domain model instances with sensible defaults.
/// Use these in test arrangements to avoid repeating boilerplate.
/// </summary>
public static class TestFixtures
{
    // ---------------------------------------------------------------------------
    // Fighter
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="Fighter"/> with sensible defaults.
    /// </summary>
    public static Fighter CreateFighter(
        int id = 1,
        string fighterName = "Test Fighter",
        double height = 175.0,
        double weight = 75.0,
        double bmi = 24.5,
        Gender gender = Gender.Male,
        FighterRole role = FighterRole.Student,
        int maxWorkoutDuration = 60,
        TrainingExperience experience = TrainingExperience.LessThanTwoYears,
        BeltColor beltColor = BeltColor.White,
        bool isWalkIn = false)
    {
        return new Fighter
        {
            Id = id,
            FighterName = fighterName,
            Height = height,
            Weight = weight,
            BMI = bmi,
            Gender = gender,
            Role = role,
            Birthdate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            MaxWorkoutDuration = maxWorkoutDuration,
            Experience = experience,
            BelkRank = beltColor,
            IsWalkIn = isWalkIn,
        };
    }

    // ---------------------------------------------------------------------------
    // TrainingSession
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="TrainingSession"/> with sensible defaults.
    /// </summary>
    public static TrainingSession CreateTrainingSession(
        int id = 1,
        int instructorId = 1,
        SessionStatus status = SessionStatus.Active,
        TargetLevel targetLevel = TargetLevel.Beginner,
        MartialArt martialArt = MartialArt.BrazilianJiuJitsu_GI,
        int capacity = 20,
        double duration = 1.5,
        string? sessionNotes = null,
        DateTime? trainingDate = null)
    {
        return new TrainingSession
        {
            Id = id,
            InstructorId = instructorId,
            TrainingDate = trainingDate ?? DateTime.UtcNow.AddDays(1),
            Capacity = capacity,
            Duration = duration,
            Status = status,
            TargetLevel = targetLevel,
            MartialArt = martialArt,
            SessionNotes = sessionNotes,
        };
    }

    // ---------------------------------------------------------------------------
    // VideoMetadata
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="VideoMetadata"/> for a shared YouTube video.
    /// </summary>
    public static VideoMetadata CreateVideoMetadata(
        int id = 1,
        string userId = "test-user-id",
        VideoType type = VideoType.Shared,
        string title = "Test Video",
        string description = "A test video description",
        string url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        string youtubeVideoId = "dQw4w9WgXcQ",
        MartialArt martialArt = MartialArt.BrazilianJiuJitsu_GI,
        DateTime? uploadedAt = null)
    {
        return new VideoMetadata
        {
            Id = id,
            UserId = userId,
            Type = type,
            Title = title,
            Description = description,
            Url = url,
            YoutubeVideoId = youtubeVideoId,
            MartialArt = martialArt,
            UploadedAt = uploadedAt ?? DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a <see cref="VideoMetadata"/> for a student upload (GCS-backed).
    /// </summary>
    public static VideoMetadata CreateStudentUploadMetadata(
        int id = 1,
        string userId = "test-user-id",
        string filePath = "gs://bucket/videos/sparring.mp4",
        string fileHash = "abc123hash",
        string studentIdentifier = "Fighter in blue gi",
        MartialArt martialArt = MartialArt.BrazilianJiuJitsu_GI,
        int duration = 300,
        DateTime? uploadedAt = null)
    {
        return new VideoMetadata
        {
            Id = id,
            UserId = userId,
            Type = VideoType.StudentUpload,
            FilePath = filePath,
            FileHash = fileHash,
            StudentIdentifier = studentIdentifier,
            MartialArt = martialArt,
            Duration = duration,
            UploadedAt = uploadedAt ?? DateTime.UtcNow,
        };
    }

    // ---------------------------------------------------------------------------
    // AppUserEntity
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates an <see cref="AppUserEntity"/> with sensible defaults.
    /// </summary>
    public static AppUserEntity CreateAppUser(
        string id = "test-user-id",
        string userName = "testuser",
        string email = "testuser@codejitsu.com",
        int fighterId = 1)
    {
        return new AppUserEntity
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FighterId = fighterId,
        };
    }
}


using Moq;
using SharedEntities.Models;
using VideoAnalysis.Server.Domain.GeminiService;
using VideoAnalysis.Server.Models.Dtos;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

/// <summary>
/// Tests for the IGeminiVisionService contract using a mock implementation.
/// The concrete GeminiVisionService requires live GCP credentials and environment
/// variables; these tests verify correct behavior via the interface to allow
/// downstream consumers to be tested in isolation.
/// </summary>
public class GeminiVisionServiceTests
{
    private readonly Mock<IGeminiVisionService> _serviceMock;

    public GeminiVisionServiceTests()
    {
        _serviceMock = new Mock<IGeminiVisionService>();
    }

    [Fact]
    public async Task Should_ReturnAnalysisJson_When_AnalyzeVideoAsyncCalledWithValidInputs()
    {
        // Arrange
        const string expectedJson = """{"overall_description":"Good technique."}""";
        _serviceMock
            .Setup(s => s.AnalyzeVideoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GeminiVisionResponse { AnalysisJson = expectedJson });

        // Act
        var result = await _serviceMock.Object.AnalyzeVideoAsync(
            videoInput: "gs://bucket/video.mp4",
            martialArt: "BJJ",
            studentIdentifier: "Fighter in blue gi",
            videoDescription: "Sparring session",
            skillLevel: "Beginner");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedJson, result.AnalysisJson);
    }

    [Fact]
    public async Task Should_ReturnCurriculumJson_When_SuggestClassCurriculumCalled()
    {
        // Arrange
        const string expectedJson = """{"session_title":"Guard Retention Focus"}""";
        _serviceMock
            .Setup(s => s.SuggestClassCurriculum(
                It.IsAny<List<string>?>(),
                It.IsAny<List<Fighter>?>(),
                It.IsAny<TrainingSession>()))
            .ReturnsAsync(new GeminiChatResponse { CurriculumJson = expectedJson });

        var session = new TrainingSession
        {
            Id = 1,
            InstructorId = 1,
            TrainingDate = DateTime.UtcNow,
            Capacity = 10,
            Duration = 1.5,
            Status = SessionStatus.Active,
            TargetLevel = TargetLevel.Beginner,
            MartialArt = MartialArt.BrazilianJiuJitsu_GI
        };

        // Act
        var result = await _serviceMock.Object.SuggestClassCurriculum(
            weaknesses: new List<string> { "Guard Retention" },
            students: null,
            classSession: session);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedJson, result.CurriculumJson);
    }

    [Fact]
    public async Task Should_ReturnEmptyPairs_When_SuggestFighterPairsCalledWithNoFighters()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.SuggestFighterPairs(
                It.Is<List<Fighter>>(f => f == null || !f.Any()),
                It.IsAny<TrainingSession>()))
            .ReturnsAsync(new MatchMakerResponse
            {
                SuggestedPairings = new MatchMakerResponseContent
                {
                    Pairs = new List<FighterPair>(),
                    PairingRationale = "No students provided to pair."
                },
                IsSuccessfullyParsed = true
            });

        var session = new TrainingSession
        {
            Id = 1,
            InstructorId = 1,
            TrainingDate = DateTime.UtcNow,
            Capacity = 10,
            Duration = 1.5,
            Status = SessionStatus.Active,
            TargetLevel = TargetLevel.Beginner,
            MartialArt = MartialArt.BrazilianJiuJitsu_GI
        };

        // Act
        var result = await _serviceMock.Object.SuggestFighterPairs(
            fighters: new List<Fighter>(),
            classSession: session);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessfullyParsed);
        Assert.NotNull(result.SuggestedPairings);
        Assert.Empty(result.SuggestedPairings!.Pairs);
    }

    [Fact]
    public async Task Should_ReturnUnpairedStudent_When_SuggestFighterPairsCalledWithSingleFighter()
    {
        // Arrange
        var singleFighter = new Fighter
        {
            Id = 1,
            FighterName = "Solo Fighter",
            Height = 175,
            Weight = 75,
            BMI = 24,
            Gender = Gender.Male,
            Role = FighterRole.Student,
            Birthdate = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            MaxWorkoutDuration = 60,
            Experience = TrainingExperience.LessThanTwoYears,
            BelkRank = BeltColor.White
        };

        _serviceMock
            .Setup(s => s.SuggestFighterPairs(
                It.Is<List<Fighter>>(f => f.Count == 1),
                It.IsAny<TrainingSession>()))
            .ReturnsAsync(new MatchMakerResponse
            {
                SuggestedPairings = new MatchMakerResponseContent
                {
                    Pairs = new List<FighterPair>(),
                    UnpairedStudent = new UnpairedFighterInfo
                    {
                        StudentId = 1,
                        StudentName = "Solo Fighter",
                        Reason = "Only one student in the class."
                    },
                    PairingRationale = "Only one student available."
                },
                IsSuccessfullyParsed = true
            });

        var session = new TrainingSession
        {
            Id = 1,
            InstructorId = 1,
            TrainingDate = DateTime.UtcNow,
            Capacity = 10,
            Duration = 1.5,
            Status = SessionStatus.Active,
            TargetLevel = TargetLevel.Beginner,
            MartialArt = MartialArt.BrazilianJiuJitsu_GI
        };

        // Act
        var result = await _serviceMock.Object.SuggestFighterPairs(
            fighters: new List<Fighter> { singleFighter },
            classSession: session);

        // Assert
        Assert.NotNull(result.SuggestedPairings);
        Assert.Empty(result.SuggestedPairings!.Pairs);
        Assert.NotNull(result.SuggestedPairings.UnpairedStudent);
        Assert.Equal(1, result.SuggestedPairings.UnpairedStudent!.StudentId);
    }

    [Fact]
    public async Task Should_ReturnFailedResponse_When_ApiCallThrowsException()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.SuggestFighterPairs(It.IsAny<List<Fighter>>(), It.IsAny<TrainingSession>()))
            .ReturnsAsync(new MatchMakerResponse
            {
                IsSuccessfullyParsed = false,
                ErrorMessage = "Error: Network timeout",
                SuggestedPairings = new MatchMakerResponseContent
                {
                    PairingRationale = "Error: Network timeout"
                }
            });

        var session = new TrainingSession
        {
            Id = 1,
            InstructorId = 1,
            TrainingDate = DateTime.UtcNow,
            Capacity = 10,
            Duration = 1.5,
            Status = SessionStatus.Active,
            TargetLevel = TargetLevel.Beginner,
            MartialArt = MartialArt.BrazilianJiuJitsu_GI
        };

        // Act
        var result = await _serviceMock.Object.SuggestFighterPairs(
            fighters: new List<Fighter> { new() { Id = 1, FighterName = "Fighter A", Height = 170, Weight = 70, BMI = 24, Gender = Gender.Male, Role = FighterRole.Student, MaxWorkoutDuration = 60, Birthdate = DateTime.UtcNow, Experience = TrainingExperience.LessThanTwoYears, BelkRank = BeltColor.White } },
            classSession: session);

        // Assert
        Assert.False(result.IsSuccessfullyParsed);
        Assert.Contains("Network timeout", result.ErrorMessage);
    }

    [Fact]
    public async Task Should_ReturnParsedPairs_When_ValidResponseReceived()
    {
        // Arrange
        var fighter1 = new Fighter { Id = 1, FighterName = "Alice", Height = 165, Weight = 60, BMI = 22, Gender = Gender.Female, Role = FighterRole.Student, MaxWorkoutDuration = 60, Birthdate = DateTime.UtcNow, Experience = TrainingExperience.LessThanTwoYears, BelkRank = BeltColor.White };
        var fighter2 = new Fighter { Id = 2, FighterName = "Bob", Height = 170, Weight = 75, BMI = 26, Gender = Gender.Male, Role = FighterRole.Student, MaxWorkoutDuration = 60, Birthdate = DateTime.UtcNow, Experience = TrainingExperience.LessThanTwoYears, BelkRank = BeltColor.White };

        _serviceMock
            .Setup(s => s.SuggestFighterPairs(It.IsAny<List<Fighter>>(), It.IsAny<TrainingSession>()))
            .ReturnsAsync(new MatchMakerResponse
            {
                SuggestedPairings = new MatchMakerResponseContent
                {
                    Pairs = new List<FighterPair>
                    {
                        new() { Fighter1Id = 1, Fighter1Name = "Alice", Fighter2Id = 2, Fighter2Name = "Bob" }
                    },
                    PairingRationale = "Similar experience levels."
                },
                IsSuccessfullyParsed = true
            });

        var session = new TrainingSession
        {
            Id = 1, InstructorId = 1, TrainingDate = DateTime.UtcNow,
            Capacity = 10, Duration = 1.5, Status = SessionStatus.Active,
            TargetLevel = TargetLevel.Beginner, MartialArt = MartialArt.BrazilianJiuJitsu_GI
        };

        // Act
        var result = await _serviceMock.Object.SuggestFighterPairs(
            fighters: new List<Fighter> { fighter1, fighter2 },
            classSession: session);

        // Assert
        Assert.True(result.IsSuccessfullyParsed);
        Assert.Single(result.SuggestedPairings!.Pairs);
        Assert.Equal(1, result.SuggestedPairings.Pairs[0].Fighter1Id);
        Assert.Equal(2, result.SuggestedPairings.Pairs[0].Fighter2Id);
    }
}


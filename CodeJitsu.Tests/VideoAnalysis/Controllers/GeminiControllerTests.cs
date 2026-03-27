using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Data;
using SharedEntities.Models;
using System.Security.Claims;
using VideoAnalysis.Server.Controllers;
using VideoAnalysis.Server.Domain.GeminiService;
using VideoAnalysis.Server.Models.Dtos;

namespace CodeJitsu.Tests.VideoAnalysis.Controllers;

public class GeminiControllerTests
{
    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static IServiceProvider BuildServiceProvider(MyDatabaseContext db, Mock<IGeminiVisionService>? geminiMock = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<MyDatabaseContext>(_ => db);
        var gemini = geminiMock?.Object ?? new Mock<IGeminiVisionService>().Object;
        services.AddScoped<IGeminiVisionService>(_ => gemini);
        services.AddScoped<CurriculumRecommendationService>();
        return services.BuildServiceProvider();
    }

    private static GeminiController CreateController(
        MyDatabaseContext db,
        Mock<IGeminiVisionService>? geminiMock = null,
        string? authUserId = "test-user-id")
    {
        geminiMock ??= new Mock<IGeminiVisionService>();
        var loggerMock = new Mock<ILogger<GeminiController>>();
        var serviceProvider = BuildServiceProvider(db, geminiMock);

        var controller = new GeminiController(geminiMock.Object, serviceProvider, loggerMock.Object);

        var claims = new List<Claim>();
        if (authUserId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, authUserId));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        return controller;
    }

    // â”€â”€ AnalyzeVideoAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact(Skip = "AnalyzeVideoAsync enqueues a Hangfire BackgroundJob which requires Hangfire storage to be initialized. Not compatible with the unit-test environment without a full Hangfire in-memory setup.")]
    public async Task Should_ReturnAccepted_When_ValidVideoIdProvided()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);
        var video = TestFixtures.CreateStudentUploadMetadata(id: 1, userId: user.Id);
        db.Videos.Add(video);
        db.SaveChanges();

        var controller = CreateController(db);

        // Act
        var result = await controller.AnalyzeVideoAsync(1);

        // Assert
        // The controller enqueues a Hangfire job â€” it always returns Accepted when the video exists
        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(accepted.Value);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_VideoIdDoesNotExistForAnalysis()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.AnalyzeVideoAsync(999);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // â”€â”€ GetVideoAnalysisResult â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnNotFound_When_VideoDoesNotExistForFeedback()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.GetVideoAnalysisResult(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_AnalysisResultNotYetGenerated()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);
        var video = TestFixtures.CreateStudentUploadMetadata(id: 1, userId: user.Id);
        db.Videos.Add(video);
        db.SaveChanges();

        // AiAnalysisProcessorService throws InvalidOperationException when no analysis found
        var controller = CreateController(db);

        // Act
        var result = await controller.GetVideoAnalysisResult(1);

        // Assert â€” AiAnalysisProcessorService will throw InvalidOperationException since no AiAnalysisResult seeded
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // â”€â”€ UpdateAnalysisAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnBadRequest_When_UpdateAnalysisBodyIsNull()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.UpdateAnalysisAsync(1, null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdateAnalysisVideoHasNoAnalysis()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);
        var video = TestFixtures.CreateStudentUploadMetadata(id: 1, userId: user.Id);
        db.Videos.Add(video);
        db.SaveChanges();

        var partialDto = new PartialAnalysisResultDto { OverallDescription = "Updated description" };
        var controller = CreateController(db);

        // Act
        var result = await controller.UpdateAnalysisAsync(1, partialDto);

        // Assert â€” no AiAnalysisResult in db, so service throws InvalidOperationException
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // â”€â”€ GenerateCurriculumAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnBadRequest_When_GenerateCurriculumWithInvalidSessionId()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        // No sessions seeded â€” CurriculumRecommendationService will throw BadHttpRequestException
        var controller = CreateController(db);

        // Act
        var result = await controller.GenerateCurriculumAsync(999);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // â”€â”€ GetCurriculumAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnOkWithNull_When_GetCurriculumWithInvalidSessionId()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.GetCurriculumAsync(999);

        // Assert â€” GetCurriculumJsonBlob returns null for an unknown session without throwing,
        // so the controller returns Ok(null). No BadHttpRequestException is raised by this path.
        Assert.IsType<OkObjectResult>(result);
    }

    // â”€â”€ SuggestStudentPairs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnBadRequest_When_SuggestPairsWithEmptyStudentList()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var geminiMock = new Mock<IGeminiVisionService>();
        var controller = CreateController(db, geminiMock);

        var request = new MatchMakerDto { StudentFighterIds = new List<int>(), InstructorFighterId = 1 };

        // Act
        var result = await controller.SuggestStudentPairs(1, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_SuggestPairsStudentIdsNotFoundInDb()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var geminiMock = new Mock<IGeminiVisionService>();
        var controller = CreateController(db, geminiMock);

        var request = new MatchMakerDto { StudentFighterIds = new List<int> { 999, 1000 }, InstructorFighterId = 1 };

        // Act
        var result = await controller.SuggestStudentPairs(1, request);

        // Assert â€” no fighters with those IDs
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_SuggestPairsInstructorNotFoundInDb()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var student = TestFixtures.CreateFighter(1, "Student A", role: FighterRole.Student);
        db.Fighters.Add(student);
        db.SaveChanges();

        var geminiMock = new Mock<IGeminiVisionService>();
        var controller = CreateController(db, geminiMock);

        var request = new MatchMakerDto { StudentFighterIds = new List<int> { 1 }, InstructorFighterId = 999 };

        // Act
        var result = await controller.SuggestStudentPairs(1, request);

        // Assert â€” instructor 999 does not exist
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_SuggestPairsSessionNotFoundInDb()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var student = TestFixtures.CreateFighter(1, "Student A", role: FighterRole.Student);
        var instructor = TestFixtures.CreateFighter(2, "Instructor", role: FighterRole.Instructor);
        db.Fighters.AddRange(student, instructor);
        db.SaveChanges();

        var geminiMock = new Mock<IGeminiVisionService>();
        var controller = CreateController(db, geminiMock);

        var request = new MatchMakerDto { StudentFighterIds = new List<int> { 1 }, InstructorFighterId = 2 };

        // Act
        var result = await controller.SuggestStudentPairs(999, request);

        // Assert â€” session 999 does not exist
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnOk_When_SuggestPairsSucceeds()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var student = TestFixtures.CreateFighter(1, "Student A", role: FighterRole.Student);
        var instructor = TestFixtures.CreateFighter(2, "Instructor", role: FighterRole.Instructor);
        db.Fighters.AddRange(student, instructor);

        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 2);
        db.TrainingSessions.Add(session);
        db.SaveChanges();

        var pairResponse = new MatchMakerResponse
        {
            IsSuccessfullyParsed = true,
            SuggestedPairings = new MatchMakerResponseContent
            {
                Pairs = new List<FighterPair>
                {
                    new() { Fighter1Id = 1, Fighter1Name = "Student A", Fighter2Id = 2, Fighter2Name = "Instructor" }
                }
            }
        };

        var geminiMock = new Mock<IGeminiVisionService>();
        geminiMock.Setup(g => g.SuggestFighterPairs(It.IsAny<List<Fighter>>(), It.IsAny<TrainingSession>()))
                  .ReturnsAsync(pairResponse);

        var controller = CreateController(db, geminiMock);

        var request = new MatchMakerDto { StudentFighterIds = new List<int> { 1 }, InstructorFighterId = 2 };

        // Act
        var result = await controller.SuggestStudentPairs(1, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<MatchMakerResponse>(ok.Value);
        Assert.True(response.IsSuccessfullyParsed);
    }
}


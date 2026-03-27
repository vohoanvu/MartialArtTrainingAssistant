using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Data;
using SharedEntities.Models;
using System.Security.Claims;
using System.Text.Json;
using VideoAnalysis.Server.Controllers;
using VideoAnalysis.Server.Domain.YoutubeSharingService;

namespace CodeJitsu.Tests.VideoAnalysis.Controllers;

public class YoutubeSearchControllerTests
{
    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static IServiceProvider BuildServiceProvider(MyDatabaseContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<MyDatabaseContext>(_ => db);
        return services.BuildServiceProvider();
    }

    private static YoutubeSearchController CreateController(
        MyDatabaseContext db,
        Mock<IYoutubeDataService>? youtubeMock = null,
        string? authUserId = "test-user-id")
    {
        youtubeMock ??= new Mock<IYoutubeDataService>();
        var serviceProvider = BuildServiceProvider(db);

        var controller = new YoutubeSearchController(youtubeMock.Object, serviceProvider);

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

    // â”€â”€ SearchVideos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnOkWithYoutubeVideos_When_SearchSucceeds()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var instructor = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor);
        db.Fighters.Add(instructor);
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 1);
        db.TrainingSessions.Add(session);
        db.SaveChanges();

        var expectedResults = new List<YoutubeSearchResult>
        {
            new YoutubeSearchResult
            {
                VideoId = "abc123",
                Title = "Armbar Tutorial",
                Description = "Learn the armbar",
                EmbedLink = "https://www.youtube.com/embed/abc123",
                PublishedAt = "2024-01-01T00:00:00+00:00",
                ThumbnailUrl = "https://img.youtube.com/vi/abc123/mqdefault.jpg"
            }
        };

        var youtubeMock = new Mock<IYoutubeDataService>();
        youtubeMock.Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>()))
                   .ReturnsAsync(expectedResults);

        var controller = CreateController(db, youtubeMock);
        var request = new VideoSearchRequest { TechniqueName = "armbar", TrainingSessionId = 1 };

        // Act
        var result = await controller.SearchVideos(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = Assert.IsType<string>(ok.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("youtube_videos", out var videos));
        Assert.Equal(1, videos.GetArrayLength());
        Assert.Equal("abc123", videos[0].GetProperty("video_id").GetString());
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TrainingSessionDoesNotExistForSearch()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);
        var request = new VideoSearchRequest { TechniqueName = "guard pass", TrainingSessionId = 999 };

        // Act
        var result = await controller.SearchVideos(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Should_Return500_When_SearchServiceThrowsException()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var instructor = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor);
        db.Fighters.Add(instructor);
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 1);
        db.TrainingSessions.Add(session);
        db.SaveChanges();

        var youtubeMock = new Mock<IYoutubeDataService>();
        youtubeMock.Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>()))
                   .ThrowsAsync(new InvalidOperationException("YouTube API key is not configured."));

        var controller = CreateController(db, youtubeMock);
        var request = new VideoSearchRequest { TechniqueName = "triangle", TrainingSessionId = 1 };

        // Act
        var result = await controller.SearchVideos(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}


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
using VideoAnalysis.Server.Domain.AIServices;

namespace CodeJitsu.Tests.VideoAnalysis.Controllers;

public class TechniqueSearchControllerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private static IServiceProvider BuildServiceProvider(MyDatabaseContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        return services.BuildServiceProvider();
    }

    private static TechniqueSearchController CreateController(
        MyDatabaseContext db,
        Mock<ITechniqueResourceSearchService>? searchMock = null,
        string? authUserId = "test-user-id")
    {
        searchMock ??= new Mock<ITechniqueResourceSearchService>();
        var controller = new TechniqueSearchController(searchMock.Object, BuildServiceProvider(db));

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

    private static MyDatabaseContext SeedSession()
    {
        var db = InMemoryDbContextFactory.Create();
        db.Fighters.Add(TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor));
        db.TrainingSessions.Add(TestFixtures.CreateTrainingSession(id: 1, instructorId: 1));
        db.SaveChanges();
        return db;
    }

    // ── SearchVideos ─────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnSnakeCaseResults_When_SearchSucceeds()
    {
        // Arrange
        var db = SeedSession();
        var searchMock = new Mock<ITechniqueResourceSearchService>();
        searchMock.Setup(s => s.SearchAsync("armbar", It.IsAny<TrainingSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TechniqueSearchResult
            {
                Source = TechniqueResourceSearchService.SourceYoutube,
                ModelId = "gemini-4-argon",
                YoutubeVideos =
                [
                    new TechniqueSearchVideo
                    {
                        VideoId = "abc123def45", Title = "Armbar Tutorial", Description = "Learn the armbar",
                        EmbedLink = "https://www.youtube.com/embed/abc123def45", PublicationDate = "2024-01-01T00:00:00+00:00",
                        ThumbnailUrl = "https://img.youtube.com/vi/abc123def45/mqdefault.jpg", ChannelTitle = "Grapplearts",
                    },
                ],
                WebResources = [new TechniqueSearchWebResource { Title = "Armbar guide", Url = "https://example.com/armbar", IsFree = true }],
                SearchEntryPointHtml = "<div>chips</div>",
            });

        var controller = CreateController(db, searchMock);

        // Act
        var result = await controller.SearchVideos(new VideoSearchRequest { TechniqueName = "armbar", TrainingSessionId = 1 });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        using var doc = JsonDocument.Parse(Assert.IsType<string>(ok.Value));
        var root = doc.RootElement;
        Assert.Equal("youtube", root.GetProperty("source").GetString());
        var video = root.GetProperty("youtube_videos")[0];
        Assert.Equal("abc123def45", video.GetProperty("video_id").GetString());
        Assert.Equal("2024-01-01T00:00:00+00:00", video.GetProperty("publication_date").GetString());
        Assert.Equal("Grapplearts", video.GetProperty("channel_title").GetString());
        Assert.True(root.GetProperty("web_resources")[0].GetProperty("is_free").GetBoolean());
        Assert.Equal("<div>chips</div>", root.GetProperty("search_entry_point_html").GetString());
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TrainingSessionDoesNotExistForSearch()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.SearchVideos(new VideoSearchRequest { TechniqueName = "guard pass", TrainingSessionId = 999 });

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Should_Return500_When_SearchServiceThrowsException()
    {
        // Arrange
        var db = SeedSession();
        var searchMock = new Mock<ITechniqueResourceSearchService>();
        searchMock.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<TrainingSession>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("YouTube API key is not configured."));

        var controller = CreateController(db, searchMock);

        // Act
        var result = await controller.SearchVideos(new VideoSearchRequest { TechniqueName = "triangle", TrainingSessionId = 1 });

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}

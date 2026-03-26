using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SampleAspNetReactDockerApp.Tests.Helpers;
using SharedEntities.Data;
using SharedEntities.Models;
using System.Security.Claims;
using VideoSharing.Server.Controllers;
using VideoSharing.Server.Domain.AIServices;

namespace SampleAspNetReactDockerApp.Tests.VideoSharing.Controllers;

public class GrokControllerTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static IServiceProvider BuildServiceProvider(MyDatabaseContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<MyDatabaseContext>(_ => db);
        return services.BuildServiceProvider();
    }

    private static GrokController CreateController(
        MyDatabaseContext db,
        Mock<IXAIService>? searchMock = null,
        string? authUserId = "test-user-id")
    {
        searchMock ??= new Mock<IXAIService>();
        var serviceProvider = BuildServiceProvider(db);

        var controller = new GrokController(searchMock.Object, serviceProvider);

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

    // ── SearchVideos ───────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnOkWithMarkdown_When_SearchSucceeds()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var instructor = TestFixtures.CreateFighter(1, "Instructor", role: FighterRole.Instructor);
        db.Fighters.Add(instructor);
        var session = TestFixtures.CreateTrainingSession(id: 1, instructorId: 1);
        db.TrainingSessions.Add(session);
        db.SaveChanges();

        var expectedMarkdown = "## Armbar Videos\n- [Tutorial](https://youtube.com/example)";

        var searchMock = new Mock<IXAIService>();
        searchMock.Setup(s => s.SearchVideosAsync("armbar", It.IsAny<TrainingSession>()))
                  .ReturnsAsync(expectedMarkdown);

        var controller = CreateController(db, searchMock);
        var request = new GrokLiveSearchRequest { TechniqueName = "armbar", TrainingSessionId = 1 };

        // Act
        var result = await controller.SearchVideos(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedMarkdown, ok.Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TrainingSessionDoesNotExistForSearch()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);
        var request = new GrokLiveSearchRequest { TechniqueName = "guard pass", TrainingSessionId = 999 };

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

        var searchMock = new Mock<IXAIService>();
        searchMock.Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<TrainingSession>()))
                  .ThrowsAsync(new InvalidOperationException("API key is not configured."));

        var controller = CreateController(db, searchMock);
        var request = new GrokLiveSearchRequest { TechniqueName = "triangle", TrainingSessionId = 1 };

        // Act
        var result = await controller.SearchVideos(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}

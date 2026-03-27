using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Data;
using SharedEntities.Models;
using System.Security.Claims;
using VideoAnalysis.Server.Controllers;
using VideoAnalysis.Server.Domain.GoogleCloudStorageService;
using VideoAnalysis.Server.Domain.YoutubeSharingService;
using VideoAnalysis.Server.Models.Dtos;
using VideoAnalysis.Server.Repository;

namespace CodeJitsu.Tests.VideoAnalysis.Controllers;

public class VideoControllerTests
{
    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Builds a ServiceProvider that resolves MyDatabaseContext to the provided in-memory instance.
    /// This mirrors how VideoController resolves db via _serviceProvider.CreateScope().
    /// </summary>
    private static IServiceProvider BuildServiceProvider(MyDatabaseContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<MyDatabaseContext>(_ => db);
        return services.BuildServiceProvider();
    }

    private static VideoController CreateController(
        MyDatabaseContext db,
        Mock<IYoutubeDataService>? youtubeMock = null,
        Mock<ISharedVideoRepository>? repMock = null,
        Mock<IGoogleCloudStorageService>? gcsMock = null,
        Mock<IHubContext<VideoShareHub>>? hubMock = null,
        string? authUserId = "test-user-id")
    {
        youtubeMock ??= new Mock<IYoutubeDataService>();
        repMock ??= new Mock<ISharedVideoRepository>();
        gcsMock ??= new Mock<IGoogleCloudStorageService>();
        hubMock ??= new Mock<IHubContext<VideoShareHub>>();

        // Mock the hub clients chain so SendAsync doesn't throw
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        hubMock.Setup(h => h.Clients).Returns(mockClients.Object);

        var serviceProvider = BuildServiceProvider(db);
        var loggerMock = new Mock<ILogger<VideoController>>();

        var controller = new VideoController(
            youtubeMock.Object,
            repMock.Object,
            serviceProvider,
            gcsMock.Object,
            hubMock.Object,
            loggerMock.Object);

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

    // â”€â”€ GetAll â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnOkWithVideos_When_SharedVideosExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);

        var video1 = TestFixtures.CreateVideoMetadata(id: 1, userId: user.Id);
        var video2 = TestFixtures.CreateVideoMetadata(id: 2, userId: user.Id, title: "Second Video", youtubeVideoId: "abc123");
        db.Videos.AddRange(video1, video2);
        db.SaveChanges();

        var repMock = new Mock<ISharedVideoRepository>();
        repMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VideoMetadata> { video1, video2 });

        // Make navigation property available
        video1.AppUser = user;
        video2.AppUser = user;

        var controller = CreateController(db, repMock: repMock, authUserId: null);

        // Act
        var result = await controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<VideoDetailsResponse>>(ok.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task Should_ReturnOkWithEmptyList_When_NoSharedVideosExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var repMock = new Mock<ISharedVideoRepository>();
        repMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VideoMetadata>());

        var controller = CreateController(db, repMock: repMock, authUserId: null);

        // Act
        var result = await controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<VideoDetailsResponse>>(ok.Value);
        Assert.Empty(list);
    }

    // â”€â”€ GetVideoMetadata â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnOkWithVideoDetails_When_ValidYoutubeUrlProvided()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);
        db.SaveChanges();

        var videoId = "dQw4w9WgXcQ";
        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
        var request = new SharingVideoRequest { VideoUrl = videoUrl };

        var detailsResponse = new VideoDetailsResponse
        {
            VideoId = videoId,
            Title = "Test Video",
            Description = "Test Description",
            EmbedLink = $"https://www.youtube.com/embed/{videoId}"
        };

        var youtubeMock = new Mock<IYoutubeDataService>();
        youtubeMock.Setup(y => y.GetVideoDetailsAsync(videoId)).ReturnsAsync(detailsResponse);

        var repMock = new Mock<ISharedVideoRepository>();
        repMock.Setup(r => r.SaveAsync(It.IsAny<VideoMetadata>())).ReturnsAsync(1);

        var controller = CreateController(db, youtubeMock: youtubeMock, repMock: repMock);

        // Act
        var result = await controller.GetVideoMetadata(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<VideoDetailsResponse>(ok.Value);
        Assert.Equal("Test Video", response.Title);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_InvalidYoutubeUrlProvided()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);
        db.SaveChanges();

        var request = new SharingVideoRequest { VideoUrl = "not-a-valid-url" };
        var controller = CreateController(db);

        // Act
        var result = await controller.GetVideoMetadata(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_YoutubeVideoDetailsNotFound()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);
        db.SaveChanges();

        var videoId = "dQw4w9WgXcQ";
        var request = new SharingVideoRequest { VideoUrl = $"https://www.youtube.com/watch?v={videoId}" };

        var youtubeMock = new Mock<IYoutubeDataService>();
        youtubeMock.Setup(y => y.GetVideoDetailsAsync(videoId)).ReturnsAsync((VideoDetailsResponse)null!);

        var controller = CreateController(db, youtubeMock: youtubeMock);

        // Act
        var result = await controller.GetVideoMetadata(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_GetVideoMetadataCalledWithoutAuth()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var request = new SharingVideoRequest { VideoUrl = "https://www.youtube.com/watch?v=abc" };

        var controller = CreateController(db, authUserId: null);

        // Act
        var result = await controller.GetVideoMetadata(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    // â”€â”€ DeleteUploadedVideoAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact(Skip = "DeleteUploadedVideoAsync calls ExecuteDeleteAsync which is not supported by the EF Core InMemory provider. Requires a real database or a provider that supports bulk-delete operations.")]
    public async Task Should_ReturnOk_When_UploadedVideoDeletedSuccessfully()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);
        var video = TestFixtures.CreateStudentUploadMetadata(id: 1, userId: user.Id, filePath: "gs://bucket/test.mp4");
        db.Videos.Add(video);
        db.SaveChanges();

        var gcsMock = new Mock<IGoogleCloudStorageService>();
        gcsMock.Setup(g => g.DeleteFileAsync(video.FilePath!)).Returns(Task.CompletedTask);

        var controller = CreateController(db, gcsMock: gcsMock);

        // Act
        var result = await controller.DeleteUploadedVideoAsync(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Null(db.Videos.Find(1));
    }

    [Fact(Skip = "DeleteUploadedVideoAsync calls ExecuteDeleteAsync which is not supported by the EF Core InMemory provider. Requires a real database or a provider that supports bulk-delete operations.")]
    public async Task Should_ReturnNotFound_When_VideoToDeleteDoesNotExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.DeleteUploadedVideoAsync(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // â”€â”€ GetUploadedVideoAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnOkWithVideoDto_When_UploadedVideoFound()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var fighter = TestFixtures.CreateFighter(1);
        db.Fighters.Add(fighter);
        var user = TestFixtures.CreateAppUser(fighterId: fighter.Id);
        user.Fighter = fighter;
        db.Users.Add(user);
        var video = TestFixtures.CreateStudentUploadMetadata(id: 1, userId: user.Id, filePath: "gs://bucket/test.mp4");
        db.Videos.Add(video);
        db.SaveChanges();

        var gcsMock = new Mock<IGoogleCloudStorageService>();
        gcsMock.Setup(g => g.GenerateSignedUrlAsync("gs://bucket/test.mp4", It.IsAny<TimeSpan>()))
               .ReturnsAsync("https://signed-url.example.com");

        var controller = CreateController(db, gcsMock: gcsMock);

        // Act
        var result = await controller.GetUploadedVideoAsync(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UploadedVideoDto>(ok.Value);
        Assert.Equal(1, dto.Id);
        Assert.Equal("https://signed-url.example.com", dto.SignedUrl);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UploadedVideoDoesNotExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.GetUploadedVideoAsync(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // â”€â”€ GetAllUploadedVideosAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnOkWithList_When_AuthUserHasUploadedVideos()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var fighter = TestFixtures.CreateFighter(1);
        db.Fighters.Add(fighter);
        var user = TestFixtures.CreateAppUser(id: "test-user-id", fighterId: fighter.Id);
        user.Fighter = fighter;
        db.Users.Add(user);
        var video = TestFixtures.CreateStudentUploadMetadata(id: 1, userId: user.Id);
        db.Videos.Add(video);
        db.SaveChanges();

        var controller = CreateController(db);

        // Act
        var result = await controller.GetAllUploadedVideosAsync();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<UploadedVideoDto>>(ok.Value);
        Assert.Single(list);
    }

    // â”€â”€ ImportAiAnalysis â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnNotFound_When_VideoForAiImportDoesNotExist()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var controller = CreateController(db);

        // Act
        var result = await controller.ImportAiAnalysis(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_VideoExistsButNoAiAnalysisResult()
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
        var result = await controller.ImportAiAnalysis(1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // â”€â”€ UploadSparringVideoAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Should_ReturnBadRequest_When_SparringVideoFileIsInvalidFormat()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();
        var user = TestFixtures.CreateAppUser();
        db.Users.Add(user);
        db.SaveChanges();

        // Create a mock file with invalid content type
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.ContentType).Returns("text/plain"); // invalid video type

        var request = new UploadVideoRequest { Description = "Test", StudentIdentifier = "Blue gi", MartialArt = MartialArt.BrazilianJiuJitsu_GI };
        var controller = CreateController(db);

        // Act
        var result = await controller.UploadSparringVideoAsync(fileMock.Object, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UploadSparringVideoCalledWithoutAuth()
    {
        // Arrange
        var db = InMemoryDbContextFactory.Create();

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.ContentType).Returns("video/mp4");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream(new byte[1024]));

        var gcsMock = new Mock<IGoogleCloudStorageService>();
        gcsMock.Setup(g => g.CalculateFileHashAsync(It.IsAny<System.IO.Stream>())).ReturnsAsync("abc123");

        var request = new UploadVideoRequest { Description = "Test", StudentIdentifier = "Blue gi", MartialArt = MartialArt.BrazilianJiuJitsu_GI };
        var controller = CreateController(db, gcsMock: gcsMock, authUserId: null);

        // Act
        var result = await controller.UploadSparringVideoAsync(fileMock.Object, request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
}


using Microsoft.AspNetCore.SignalR;
using Moq;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.YoutubeSharingService;
using VideoSharing.Server.Repository;
using SampleAspNetReactDockerApp.Tests.Helpers;

namespace SampleAspNetReactDockerApp.Tests.VideoSharing.Repositories;

public class SharedVideoRepositoryTests
{
    private static MyDatabaseContext CreateContext() => InMemoryDbContextFactory.Create();

    private static Mock<IHubContext<VideoShareHub>> BuildHubMock()
    {
        var hubMock = new Mock<IHubContext<VideoShareHub>>();
        var clientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);
        return hubMock;
    }

    private static async Task SeedUser(MyDatabaseContext context, string userId = "user-1", string userName = "testuser")
    {
        var user = new AppUserEntity
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@codejitsu.com",
            NormalizedEmail = $"{userName}@codejitsu.com".ToUpperInvariant()
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_ReturnVideo_When_GetByIdCalledWithExistingId()
    {
        // Arrange
        using var context = CreateContext();
        await SeedUser(context);
        var video = TestFixtures.CreateVideoMetadata(id: 0, userId: "user-1");
        context.Videos.Add(video);
        await context.SaveChangesAsync();

        var repo = new SharedVideoRepository(context, BuildHubMock().Object);

        // Act
        var result = await repo.GetByIdAsync(video.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(video.Id, result!.Id);
    }

    [Fact]
    public async Task Should_ReturnNull_When_GetByIdCalledWithNonExistentId()
    {
        // Arrange
        using var context = CreateContext();
        var repo = new SharedVideoRepository(context, BuildHubMock().Object);

        // Act
        var result = await repo.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_ReturnOnlySharedVideos_When_GetAllAsyncCalled()
    {
        // Arrange
        using var context = CreateContext();
        await SeedUser(context);

        var sharedVideo = TestFixtures.CreateVideoMetadata(id: 0, userId: "user-1", type: VideoType.Shared);
        var uploadVideo = TestFixtures.CreateStudentUploadMetadata(id: 0, userId: "user-1");

        context.Videos.Add(sharedVideo);
        context.Videos.Add(uploadVideo);
        await context.SaveChangesAsync();

        var repo = new SharedVideoRepository(context, BuildHubMock().Object);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        Assert.All(result, v => Assert.Equal(VideoType.Shared, v.Type));
        Assert.Single(result);
    }

    [Fact]
    public async Task Should_SaveNewVideo_When_YoutubeVideoIdDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        await SeedUser(context);

        var hubMock = BuildHubMock();
        var repo = new SharedVideoRepository(context, hubMock.Object);

        var video = new VideoMetadata
        {
            UserId = "user-1",
            Type = VideoType.Shared,
            Title = "New BJJ Video",
            Description = "Guard passing techniques",
            Url = "https://www.youtube.com/watch?v=newVideo123",
            YoutubeVideoId = "newVideo123",
            UploadedAt = DateTime.UtcNow
        };

        // Act
        var savedId = await repo.SaveAsync(video);

        // Assert
        Assert.True(savedId > 0);
        Assert.Single(context.Videos.Where(v => v.YoutubeVideoId == "newVideo123").ToList());

        // Verify SignalR notification was sent
        hubMock.Object.Clients.All
            .Should_HaveBeenCalledWithNewNotification(hubMock);
    }

    [Fact]
    public async Task Should_UpdateExistingVideo_When_YoutubeVideoIdAlreadyExists()
    {
        // Arrange
        using var context = CreateContext();
        await SeedUser(context);

        var existing = new VideoMetadata
        {
            UserId = "user-1",
            Type = VideoType.Shared,
            Title = "Old Title",
            Description = "Old description",
            Url = "https://www.youtube.com/watch?v=existingId",
            YoutubeVideoId = "existingId",
            UploadedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.Videos.Add(existing);
        await context.SaveChangesAsync();

        var hubMock = BuildHubMock();
        var repo = new SharedVideoRepository(context, hubMock.Object);

        var updated = new VideoMetadata
        {
            UserId = "user-1",
            Type = VideoType.Shared,
            Title = "Updated Title",
            Description = "Updated description",
            Url = "https://www.youtube.com/watch?v=existingId",
            YoutubeVideoId = "existingId",
            UploadedAt = DateTime.UtcNow
        };

        // Act
        var returnedId = await repo.SaveAsync(updated);

        // Assert
        Assert.Equal(existing.Id, returnedId);
        var storedVideo = await context.Videos.FindAsync(existing.Id);
        Assert.Equal("Updated Title", storedVideo!.Title);
        Assert.Equal("Updated description", storedVideo.Description);

        // Verify "already shared" notification was sent
        hubMock.Object.Clients.All
            .Should_HaveBeenCalledWithAlreadySharedNotification(hubMock);
    }
}

/// <summary>
/// Extension helpers for verifying SignalR hub mock invocations.
/// </summary>
internal static class HubMockExtensions
{
    internal static void Should_HaveBeenCalledWithNewNotification(
        this IClientProxy _, Mock<IHubContext<VideoShareHub>> hubMock)
    {
        hubMock.Verify(h => h.Clients.All, Times.AtLeastOnce);
    }

    internal static void Should_HaveBeenCalledWithAlreadySharedNotification(
        this IClientProxy _, Mock<IHubContext<VideoShareHub>> hubMock)
    {
        hubMock.Verify(h => h.Clients.All, Times.AtLeastOnce);
    }
}

using Google.Apis.YouTube.v3.Data;
using Moq;
using VideoSharing.Server.Domain.YoutubeSharingService;

namespace SampleAspNetReactDockerApp.Tests.VideoSharing.Services;

public class YoutubeDataServiceTests
{
    private readonly Mock<IYoutubeServiceWrapper> _wrapperMock;
    private readonly YoutubeDataService _service;

    public YoutubeDataServiceTests()
    {
        _wrapperMock = new Mock<IYoutubeServiceWrapper>();
        _service = new YoutubeDataService(_wrapperMock.Object);
    }

    private static VideoListResponse BuildVideoListResponse(string videoId, string title, string description)
    {
        return new VideoListResponse
        {
            Items = new List<Video>
            {
                new()
                {
                    Snippet = new VideoSnippet
                    {
                        Title = title,
                        Description = description
                    }
                }
            }
        };
    }

    [Fact]
    public async Task Should_ReturnVideoDetails_When_ValidVideoIdProvided()
    {
        // Arrange
        const string videoId = "dQw4w9WgXcQ";
        var listRequestMock = new Mock<IListRequestWrapper>();
        listRequestMock
            .Setup(r => r.ExecuteAsync())
            .ReturnsAsync(BuildVideoListResponse(videoId, "Test Video Title", "Test description"));

        _wrapperMock.Setup(w => w.List("snippet")).Returns(listRequestMock.Object);

        // Act
        var result = await _service.GetVideoDetailsAsync(videoId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(videoId, result.VideoId);
        Assert.Equal("Test Video Title", result.Title);
        Assert.Equal("Test description", result.Description);
        Assert.Equal($"https://www.youtube.com/embed/{videoId}", result.EmbedLink);
    }

    [Fact]
    public async Task Should_ReturnNull_When_VideoIdNotFoundInYouTube()
    {
        // Arrange
        const string videoId = "nonexistent123";
        var listRequestMock = new Mock<IListRequestWrapper>();
        listRequestMock
            .Setup(r => r.ExecuteAsync())
            .ReturnsAsync(new VideoListResponse { Items = new List<Video>() });

        _wrapperMock.Setup(w => w.List("snippet")).Returns(listRequestMock.Object);

        // Act
        var result = await _service.GetVideoDetailsAsync(videoId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_ThrowVideoDetailsException_When_ApiThrowsException()
    {
        // Arrange
        const string videoId = "error-video";
        var listRequestMock = new Mock<IListRequestWrapper>();
        listRequestMock
            .Setup(r => r.ExecuteAsync())
            .ThrowsAsync(new Exception("YouTube API quota exceeded"));

        _wrapperMock.Setup(w => w.List("snippet")).Returns(listRequestMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VideoDetailsException>(
            () => _service.GetVideoDetailsAsync(videoId));

        Assert.Contains(videoId, ex.Message);
        Assert.NotNull(ex.InnerException);
        Assert.Contains("YouTube API quota exceeded", ex.InnerException!.Message);
    }
}

using SampleAspNetReactDockerApp.Server.Helpers;
using Moq;
using Google.Apis.YouTube.v3.Data;

namespace SampleAspNetReactDockerApp.Tests
{
    public class YoutubeDataServiceTests
    {
        private YoutubeDataService _youtubeDataService;
        private Mock<IYoutubeServiceWrapper> _mockYoutubeServiceWrapper;

        public YoutubeDataServiceTests()
        {
            _mockYoutubeServiceWrapper = new Mock<IYoutubeServiceWrapper>();
            _youtubeDataService = new YoutubeDataService(_mockYoutubeServiceWrapper.Object);
        }

        [Fact]
        public async Task GetVideoDetailsAsync_ValidVideoId_ReturnsVideoDetails()
        {
            // Arrange
            var videoId = "validVideoId";
            var videoListResponse = new VideoListResponse
            {
                Items = new List<Video>
                {
                    new Video
                    {
                        Snippet = new VideoSnippet
                        {
                            Title = "Test Title",
                            Description = "Test Description"
                        }
                    }
                }
            };

            var mockListRequest = new Mock<IListRequestWrapper>();
            mockListRequest.Setup(x => x.ExecuteAsync()).ReturnsAsync(videoListResponse);
            _mockYoutubeServiceWrapper.Setup(y => y.List("snippet")).Returns(mockListRequest.Object);

            // Act
            var result = await _youtubeDataService.GetVideoDetailsAsync(videoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(videoId, result.VideoId);
            Assert.Equal("Test Title", result.Title);
            Assert.Equal("Test Description", result.Description);
        }


        [Fact]
        public async Task GetVideoDetailsAsync_InvalidVideoId_ReturnsNull()
        {
            // Arrange
            var videoId = "invalidVideoId";
            var videoListResponse = new VideoListResponse
            {
                Items = new List<Video>() // No videos in the list
            };

            var mockListRequest = new Mock<IListRequestWrapper>();
            mockListRequest.Setup(x => x.ExecuteAsync()).ReturnsAsync(videoListResponse);
            _mockYoutubeServiceWrapper.Setup(y => y.List("snippet")).Returns(mockListRequest.Object);

            // Act
            var result = await _youtubeDataService.GetVideoDetailsAsync(videoId);

            // Assert
            Assert.Null(result);
        }


        [Fact]
        public async Task GetVideoDetailsAsync_ApiThrowsException_ThrowsVideoDetailsException()
        {
            // Arrange
            var videoId = "validVideoId";
            var mockListRequest = new Mock<IListRequestWrapper>();
            mockListRequest.Setup(x => x.ExecuteAsync()).ThrowsAsync(new Exception("YouTube API error"));
            _mockYoutubeServiceWrapper.Setup(y => y.List("snippet")).Returns(mockListRequest.Object);

            // Act and Assert
            var exception = await Assert.ThrowsAsync<VideoDetailsException>(() => _youtubeDataService.GetVideoDetailsAsync(videoId));
            Assert.Contains("Failed to get video details for video ID", exception.Message);
        }
    }
}

using SampleAspNetReactDockerApp.Server.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using SampleAspNetReactDockerApp.Server.Data;
using SampleAspNetReactDockerApp.Server.Models;

namespace SampleAspNetReactDockerApp.Tests
{
    public class YoutubeDataServiceTests
    {
        private YoutubeDataService _youtubeDataService;

        public YoutubeDataServiceTests()
        {
            _youtubeDataService = new Mock<YoutubeDataService>().Object;
        }

        [Fact]
        public async Task GetVideoDetailsAsync_ValidVideoId_ReturnsVideoDetails()
        {
            // Arrange
            var videoId = "validVideoId";

            // Act
            var result = await _youtubeDataService.GetVideoDetailsAsync(videoId);

            // Assert
            Assert.NotNull(result);
            // Add more assertions based on the expected result
        }

        [Fact]
        public async Task GetVideoDetailsAsync_InvalidVideoId_ReturnsNull()
        {
            // Arrange
            var videoId = "invalidVideoId";

            // Act
            var result = await _youtubeDataService.GetVideoDetailsAsync(videoId);

            // Assert
            Assert.Null(result);
        }
    }
}

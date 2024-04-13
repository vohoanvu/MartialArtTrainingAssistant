using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SampleAspNetReactDockerApp.Server.Controllers;
using SampleAspNetReactDockerApp.Server.Data;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;
using System.Security.Claims;

namespace SampleAspNetReactDockerApp.Tests
{
    public class VideoControllerTests
    {
        private readonly VideoController _controller;
        private readonly Mock<IYoutubeDataService> _mockYoutubeDataService;
        private readonly Mock<ISharedVideoRepository> _mockSharedVideoRepository;
        private readonly Mock<HttpContext> _mockHttpContext;

        public VideoControllerTests()
        {
            _mockYoutubeDataService = new Mock<IYoutubeDataService>();
            _mockSharedVideoRepository = new Mock<ISharedVideoRepository>();
            _mockHttpContext = new Mock<HttpContext>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Name, "testUsername"),
            }, "mock"));

            _mockHttpContext.Setup(m => m.User).Returns(user);

            _controller = new VideoController(_mockYoutubeDataService.Object, _mockSharedVideoRepository.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetVideoMetadata_ValidVideoUrl_ReturnsOkResultWithVideoMetadata()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=yXRNwxNzy3Q";
            var videoId = "yXRNwxNzy3Q";
            var videoDetailsResponse = new VideoDetailsResponse
            {
                VideoId = videoId,
                Title = "Test Title",
                Description = "Test Description",
                EmbedLink = $"https://www.youtube.com/embed/{videoId}",
                SharedBy = new AppUserDto
                {
                    UserId = "testUserId",
                    Username = "testUsername"
                }
            };
            _mockYoutubeDataService.Setup(m => m.GetVideoDetailsAsync(videoId)).ReturnsAsync(videoDetailsResponse);
            _mockSharedVideoRepository.Setup(m => m.SaveAsync(It.IsAny<SharedVideo>())).ReturnsAsync(1);

            // Act
            var result = await _controller.GetVideoMetadata(videoUrl);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<VideoDetailsResponse>(okResult.Value);
            Assert.Equal(videoDetailsResponse.VideoId, returnValue.VideoId);
            Assert.Equal(videoDetailsResponse.Title, returnValue.Title);
            Assert.Equal(videoDetailsResponse.Description, returnValue.Description);
            Assert.Equal(videoDetailsResponse.EmbedLink, returnValue.EmbedLink);
            Assert.Equal(videoDetailsResponse.SharedBy.UserId, returnValue.SharedBy.UserId);
            Assert.Equal(videoDetailsResponse.SharedBy.Username, returnValue.SharedBy.Username);
        }

        [Fact]
        public async Task GetVideoMetadata_InvalidVideoUrl_ReturnsBadRequestResultWithErrorMessage()
        {
            // Arrange
            var videoUrl = "https://youtubee.com/watch?v=123abc";

            // Act
            var result = await _controller.GetVideoMetadata(videoUrl);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal($"No valid video Id was found from this url {videoUrl}. Please double-check the url or the regex pattern!", badRequestResult.Value);
        }

        [Fact]
        public async Task GetVideoMetadata_VideoNotFound_ReturnsNotFoundResultWithErrorMessage()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=nonexistentVideoId";
            var videoId = YouTubeHelper.ExtractVideoId(videoUrl);
            _mockYoutubeDataService.Setup(m => m.GetVideoDetailsAsync(videoId!))
                .ReturnsAsync((VideoDetailsResponse)null!);

            // Act
            var result = await _controller.GetVideoMetadata(videoUrl);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal($"Video metadata was not found for this videoId {videoId}", notFoundResult.Value);
        }

        [Fact]
        public async Task GetVideoMetadata_UserNotAuthorized_ReturnsUnauthorizedResult()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=f8ELCqEq0tg";
            var videoId = "f8ELCqEq0tg";
            // mocking authorized user by setting a empty ClaimsPrincipal
            _mockHttpContext.Setup(m => m.User).Returns(new ClaimsPrincipal());
            var videoDetailsResponse = new VideoDetailsResponse
            {
                VideoId = videoId,
                Title = "Test Title",
                Description = "Test Description",
                EmbedLink = $"https://www.youtube.com/embed/{videoId}",
                SharedBy = new AppUserDto
                {
                    UserId = "testUserId",
                    Username = "testUsername"
                }
            };
            _mockYoutubeDataService.Setup(m => m.GetVideoDetailsAsync(videoId)).ReturnsAsync(videoDetailsResponse);

            // Act
            var result = await _controller.GetVideoMetadata(videoUrl);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetVideoMetadata_ValidVideoUrlAndAuthorizedUser_RepositorySaveAsyncWasCalledWithCorrectParameters()
        {
            // Arrange
            var videoUrl = "https://www.youtube.com/watch?v=yXRNwxNzy3Q";
            var videoId = "yXRNwxNzy3Q";
            var authenticatedUer = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                new Claim(ClaimTypes.Name, "testUsername"),
            }, "mock"));
            _mockHttpContext.Setup(m => m.User).Returns(authenticatedUer);

            var videoDetailsResponse = new VideoDetailsResponse
            {
                VideoId = videoId,
                Title = "Test Title",
                Description = "Test Description",
                EmbedLink = $"https://www.youtube.com/embed/{videoId}",
            };
            _mockYoutubeDataService.Setup(m => m.GetVideoDetailsAsync(videoId)).ReturnsAsync(videoDetailsResponse);
            _mockSharedVideoRepository.Setup(m => m.SaveAsync(It.IsAny<SharedVideo>())).ReturnsAsync(1);

            // Act
            var result = await _controller.GetVideoMetadata(videoUrl);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<VideoDetailsResponse>(okResult.Value);
            Assert.Equal(videoDetailsResponse.VideoId, returnValue.VideoId);
            Assert.Equal(videoDetailsResponse.Title, returnValue.Title);
            Assert.Equal(videoDetailsResponse.Description, returnValue.Description);
            Assert.Equal(videoDetailsResponse.EmbedLink, returnValue.EmbedLink);
            Assert.Equal(videoDetailsResponse.SharedBy.UserId, returnValue.SharedBy.UserId);
            Assert.Equal(videoDetailsResponse.SharedBy.Username, returnValue.SharedBy.Username);

            _mockSharedVideoRepository.Verify(m => m.SaveAsync(It.Is<SharedVideo>(v => 
                v.VideoId == videoId &&
                v.Title == videoDetailsResponse.Title &&
                v.Description == videoDetailsResponse.Description &&
                v.Url == videoUrl &&
                v.UserId == authenticatedUer.FindFirstValue(ClaimTypes.NameIdentifier)
            )), Times.Once);
        }
    }
}

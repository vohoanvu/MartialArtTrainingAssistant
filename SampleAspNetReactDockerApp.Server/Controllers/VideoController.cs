using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetReactDockerApp.Server.Data;
using SampleAspNetReactDockerApp.Server.Domain.YoutubeSharingService;
using SampleAspNetReactDockerApp.Server.Models;
using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace SampleAspNetReactDockerApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController(IYoutubeDataService youtubeDataService,
        ISharedVideoRepository sharedVideoRepository) : ControllerBase
    {
        private readonly IYoutubeDataService _youtubeDataService = youtubeDataService;
        private readonly ISharedVideoRepository _sharedVideoRepository = sharedVideoRepository;

        [HttpPost("metadata")]
        [AllowAnonymous]
        public async Task<ActionResult<VideoDetailsResponse>> GetVideoMetadata(UploadVideoRequest request)
        {
            var videoUrl = WebUtility.UrlDecode(request.VideoUrl);
            var videoId = YouTubeHelper.ExtractVideoId(videoUrl);
            if (videoId == null)
            {
                return BadRequest($"No valid video Id was found from this url {videoUrl}. Please double-check the url or the regex pattern!");
            }

            var videoDetailsResponse = await _youtubeDataService.GetVideoDetailsAsync(videoId);
            if (videoDetailsResponse is null)
            {
                return NotFound($"Video metadata was not found for this videoId {videoId}");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var video = new SharedVideo
            {
                VideoId = videoId,
                Title = videoDetailsResponse.Title,
                Description = videoDetailsResponse.Description,
                Url = videoUrl,
                DateShared = DateTime.Now.ToUniversalTime(),
                UserId = userId
            };

            var dbId = await _sharedVideoRepository.SaveAsync(video);

            videoDetailsResponse.SharedBy = new AppUserDto
            {
                UserId = userId,
                Username = User.Identity?.Name!
            };
            videoDetailsResponse.Id = dbId;

            return Ok(videoDetailsResponse);
        }

        [HttpGet("GetAll")]
        [AllowAnonymous]
        public async Task<ActionResult<List<VideoDetailsResponse>>> GetAll()
        {
            var sharedVideos = await _sharedVideoRepository.GetAllAsync();

            return Ok(sharedVideos.Select(v => new VideoDetailsResponse()
            {
                Id = v.Id,
                VideoId = v.VideoId,
                Title = v.Title,
                Description = v.Description,
                EmbedLink = $"https://www.youtube.com/embed/{v.VideoId}",
                SharedBy = new AppUserDto
                {
                    UserId = v.UserId,
                    Username = v.SharedBy.UserName!
                }
            }).ToList());
        }
    }

    public class UploadVideoRequest
    {
        [JsonPropertyName("videoUrl")]
        public string VideoUrl { get; set; }
    }
}

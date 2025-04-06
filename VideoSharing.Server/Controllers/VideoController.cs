using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoSharing.Server.Domain.YoutubeSharingService;
using VideoSharing.Server.Repository;
using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using SharedEntities.Data;
using SharedEntities.Models;
using Microsoft.AspNetCore.SignalR;
using VideoSharing.Server.Domain.GoogleCloudStorageService;
using Microsoft.EntityFrameworkCore;

namespace VideoSharing.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class VideoController(IYoutubeDataService youtubeDataService,
        ISharedVideoRepository sharedVideoRepository, IServiceProvider serviceProvider,
        IGoogleCloudStorageService googleCloudStorageService,
        IHubContext<VideoShareHub> hubContext) : ControllerBase
    {
        private readonly IYoutubeDataService _youtubeDataService = youtubeDataService;
        private readonly ISharedVideoRepository _sharedVideoRepository = sharedVideoRepository;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IGoogleCloudStorageService _gcsService = googleCloudStorageService;
        private readonly IHubContext<VideoShareHub> _hubContext = hubContext;

        [HttpPost("metadata")]
        [Authorize]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<ActionResult<VideoDetailsResponse>> GetVideoMetadata(UploadVideoRequest request)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var appUserEntity = dbContext.Users.Find(userId);

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
                Username = appUserEntity!.UserName!
            };
            videoDetailsResponse.Id = dbId;

            return Ok(videoDetailsResponse);
        }

        [HttpGet("GetAll")]
        [AllowAnonymous]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<ActionResult<List<VideoDetailsResponse>>> GetAll()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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

        [HttpPost("upload-sparring")]
        [Authorize]
        [RequestSizeLimit(100 * 1024 * 1024)]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<IActionResult> UploadSparringVideoAsync(IFormFile videoFile, [FromForm] string description)
        {
            if (videoFile == null || !IsValidVideoFormat(videoFile.ContentType))
                return BadRequest(new { Message = "Invalid video file" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);
            Console.WriteLine($"UserId: {userId}, Role Claim: {roleClaim}");

            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var appUserEntity = dbContext.Users.Include(u => u.Fighter)
                    .FirstOrDefault(u => u.Id == userId);
            if (appUserEntity?.Fighter == null)
            {
                Console.WriteLine("No fighter profile linked to user");
                return Forbid();
            }

            if (appUserEntity?.Fighter.Role != FighterRole.Student)
            {
                Console.WriteLine($"Role mismatch - Expected: Student (0), Actual: {appUserEntity?.Fighter.Role}");
                return Forbid();
            }

            using var stream = videoFile.OpenReadStream();
            var filePath = await _gcsService.UploadFileAsync(stream, videoFile.FileName, videoFile.ContentType);

            var uploadedVideo = new UploadedVideo
            {
                UserId = userId,
                FilePath = filePath,
                Description = description,
                UploadTimestamp = DateTime.UtcNow
            };

            dbContext.UploadedVideos.Add(uploadedVideo);
            await dbContext.SaveChangesAsync();

            var signedUrl = await _gcsService.GenerateSignedUrlAsync(filePath, TimeSpan.FromHours(1));
            await _hubContext.Clients.All.SendAsync(
                "ReceiveVideoSharedNotification",
                "New Sparring Video Uploaded!",
                uploadedVideo.Description,
                appUserEntity.UserName
            );

            return Ok(new { VideoId = uploadedVideo.Id, SignedUrl = signedUrl });
        }

        [HttpPost("upload-demonstration")]
        [Authorize]
        [RequestSizeLimit(100 * 1024 * 1024)]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<IActionResult> UploadDemonstrationAsync(IFormFile videoFile, [FromForm] string description)
        {
            if (videoFile == null || !IsValidVideoFormat(videoFile.ContentType))
                return BadRequest(new { Message = "Invalid video file" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);
            Console.WriteLine($"UserId: {userId}, Role Claim: {roleClaim}");

            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var appUserEntity = dbContext.Users.Include(u => u.Fighter)
                    .FirstOrDefault(u => u.Id == userId);
            if (appUserEntity?.Fighter == null)
            {
                Console.WriteLine("No fighter profile linked to user");
                return Forbid();
            }

            if (appUserEntity?.Fighter.Role != FighterRole.Instructor)
            {
                Console.WriteLine($"Role mismatch - Expected: Instructor (1), Actual: {appUserEntity?.Fighter.Role}");
                return Forbid();
            }

            using var stream = videoFile.OpenReadStream();
            var filePath = await _gcsService.UploadFileAsync(stream, videoFile.FileName, videoFile.ContentType);

            var uploadedVideo = new UploadedVideo
            {
                UserId = userId,
                FilePath = filePath,
                Description = description,
                UploadTimestamp = DateTime.UtcNow
            };

            dbContext.UploadedVideos.Add(uploadedVideo);
            await dbContext.SaveChangesAsync();

            var signedUrl = await _gcsService.GenerateSignedUrlAsync(filePath, TimeSpan.FromHours(1));
            await _hubContext.Clients.All.SendAsync(
                "ReceiveVideoSharedNotification",
                "New Demonstration Video Uploaded!",
                uploadedVideo.Description,
                appUserEntity.UserName
            );

            return Ok(new { VideoId = uploadedVideo.Id, SignedUrl = signedUrl });
        }

        [HttpDelete("delete-uploaded/{videoId}")]
        [Authorize]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<IActionResult> DeleteUploadedVideoAsync(int videoId)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var video = await dbContext.UploadedVideos.FindAsync(videoId);
            if (video == null)
            {
                return NotFound(new { Message = $"Video with ID {videoId} not found" });
            }

            // Delete from GCS
            await _gcsService.DeleteFileAsync(video.FilePath);

            // Remove from database
            dbContext.UploadedVideos.Remove(video);
            await dbContext.SaveChangesAsync();

            return Ok(new { Message = $"Video with ID {videoId} deleted successfully" });
        }

        [HttpGet("getall-uploaded")]
        [Authorize]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<ActionResult<List<UploadedVideo>>> GetAllUploadedVideosAsync()
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var videos = await dbContext.UploadedVideos.ToListAsync();
            return Ok(videos);
        }

        private bool IsValidVideoFormat(string contentType) =>
            contentType is "video/mp4" or "video/avi";
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class UploadVideoRequest
    {
        [JsonPropertyName("videoUrl")]
        public required string VideoUrl { get; set; }
    }
}

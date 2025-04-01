using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoSharing.Server.Domain.YoutubeSharingService;
using VideoSharing.Server.Repository;
using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using SharedEntities.Data;
using SharedEntities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using AutoMapper;
using VideoSharing.Server.Domain.GoogleCloudStorageService;

namespace VideoSharing.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController(IYoutubeDataService youtubeDataService,
        ISharedVideoRepository sharedVideoRepository, IServiceProvider serviceProvider,
        UserManager<AppUserEntity> userManager,
        MyDatabaseContext myDatabaseContext,
        IMapper mapper,
        IGoogleCloudStorageService googleCloudStorageService,
        IHubContext<VideoShareHub> hubContext) : ControllerBase
    {
        private readonly IYoutubeDataService _youtubeDataService = youtubeDataService;
        private readonly ISharedVideoRepository _sharedVideoRepository = sharedVideoRepository;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly UserManager<AppUserEntity> _userManager = userManager;
        private readonly MyDatabaseContext _dbContext = myDatabaseContext;
        private readonly IMapper _mapper = mapper;
        private readonly IGoogleCloudStorageService _gcsService = googleCloudStorageService;
        private readonly IHubContext<VideoShareHub> _hubContext = hubContext;

        [HttpPost("metadata")]
        [Authorize]
        public async Task<ActionResult<VideoDetailsResponse>> GetVideoMetadata(UploadVideoRequest request)
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

        [HttpPost("upload-sparring")]
        [Authorize]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> UploadSparringVideoAsync(IFormFile videoFile, [FromForm] string description)
        {
            if (videoFile == null || !IsValidVideoFormat(videoFile.ContentType))
                return BadRequest(new { Message = "Invalid video file" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);
            Console.WriteLine($"UserId: {userId}, Role Claim: {roleClaim}");

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("No user ID found in token");
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                Console.WriteLine("User not found in database");
                return Forbid();
            }

            if (user.Fighter == null)
            {
                Console.WriteLine("No fighter profile linked to user");
                return Forbid();
            }

            if (user.Fighter.Role != FighterRole.Student)
            {
                Console.WriteLine($"Role mismatch - Expected: Student (0), Actual: {user.Fighter.Role}");
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

            _dbContext.UploadedVideos.Add(uploadedVideo);
            await _dbContext.SaveChangesAsync();

            var signedUrl = await _gcsService.GenerateSignedUrlAsync(filePath, TimeSpan.FromHours(1));
            await _hubContext.Clients.All.SendAsync(
                "ReceiveVideoSharedNotification",
                "New Sparring Video Uploaded!",
                uploadedVideo.Description,
                user.UserName
            );

            return Ok(new { VideoId = uploadedVideo.Id, SignedUrl = signedUrl });
        }

        [HttpPost("upload-demonstration")]
        [Authorize]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> UploadDemonstrationAsync(IFormFile videoFile, [FromForm] string description)
        {
            if (videoFile == null || !IsValidVideoFormat(videoFile.ContentType))
                return BadRequest(new { Message = "Invalid video file" });

            var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userManagerService = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<UserManager<AppUserEntity>>();
            var user = await userManagerService.FindByIdAsync(instructorId);
            if (user?.Fighter?.Role != FighterRole.Instructor)
                return Forbid();

            using var stream = videoFile.OpenReadStream();
            var filePath = await _gcsService.UploadFileAsync(stream, videoFile.FileName, videoFile.ContentType);

            var demonstration = new Demonstration
            {
                InstructorId = instructorId,
                FilePath = filePath,
                Description = description,
                UploadTimestamp = DateTime.UtcNow
            };

            _dbContext.Demonstrations.Add(demonstration);
            await _dbContext.SaveChangesAsync();

            var signedUrl = await _gcsService.GenerateSignedUrlAsync(filePath, TimeSpan.FromHours(1));
            await _hubContext.Clients.All.SendAsync(
                "ReceiveVideoSharedNotification",
                "New Demonstration Video Uploaded!",
                demonstration.Description,
                user.UserName
            );

            return Ok(new { DemonstrationId = demonstration.Id, SignedUrl = signedUrl });
        }

        private bool IsValidVideoFormat(string contentType) =>
            contentType is "video/mp4" or "video/avi";
    }

    public class UploadVideoRequest
    {
        [JsonPropertyName("videoUrl")]
        public string VideoUrl { get; set; }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoSharing.Server.Domain.YoutubeSharingService;
using VideoSharing.Server.Repository;
using System.Net;
using System.Security.Claims;
using SharedEntities.Data;
using SharedEntities.Models;
using Microsoft.AspNetCore.SignalR;
using VideoSharing.Server.Domain.GoogleCloudStorageService;
using Microsoft.EntityFrameworkCore;
using VideoSharing.Server.Models.Dtos;
using VideoSharing.Server.Domain.GeminiService;

namespace VideoSharing.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController(IYoutubeDataService youtubeDataService,
        ISharedVideoRepository sharedVideoRepository, IServiceProvider serviceProvider,
        IGoogleCloudStorageService googleCloudStorageService,
        IHubContext<VideoShareHub> hubContext, ILogger<VideoController> logger) : ControllerBase
    {
        private readonly IYoutubeDataService _youtubeDataService = youtubeDataService;
        private readonly ISharedVideoRepository _sharedVideoRepository = sharedVideoRepository;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IGoogleCloudStorageService _gcsService = googleCloudStorageService;
        private readonly IHubContext<VideoShareHub> _hubContext = hubContext;
        private readonly ILogger<VideoController> _logger = logger;

        [HttpPost("metadata")]
        [Authorize]
        public async Task<ActionResult<VideoDetailsResponse>> GetVideoMetadata(SharingVideoRequest request)
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
        [RequestSizeLimit(300 * 1024 * 1024)]
        public async Task<IActionResult> UploadSparringVideoAsync(IFormFile videoFile, [FromForm] UploadVideoRequest request)
        {
            if (videoFile.Length > 300 * 1024 * 1024)
                return BadRequest(new { Message = "Video file exceeds 300 MB limit." });

            if (videoFile == null || !IsValidVideoFormat(videoFile.ContentType))
                return BadRequest(new { Message = "Invalid video file" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();

            // Calculate hash of the video file
            using var stream = videoFile.OpenReadStream();
            var videoHash = await _gcsService.CalculateFileHashAsync(stream);

            // Check for duplicate video
            var existingVideo = await dbContext.UploadedVideos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.FileHash == videoHash);

            if (existingVideo != null)
            {
                return Conflict(new
                {
                    Message = "Duplicate video detected. This video has already been uploaded.",
                    VideoId = existingVideo.Id,
                    SignedUrl = await _gcsService.GenerateSignedUrlAsync(existingVideo.FilePath, TimeSpan.FromHours(1))
                });
            }

            // Reset stream position for upload
            stream.Position = 0;

            // Upload video to Google Cloud Storage
            var filePath = await _gcsService.UploadFileAsync(stream, videoFile.FileName, videoFile.ContentType);

            var uploadedVideo = new UploadedVideo
            {
                UserId = userId,
                FilePath = filePath,
                Description = request.Description,
                StudentIdentifier = request.StudentIdentifier,
                MartialArt = request.MartialArt,
                UploadTimestamp = DateTime.UtcNow,
                FileHash = videoHash
            };

            dbContext.UploadedVideos.Add(uploadedVideo);
            await dbContext.SaveChangesAsync();

            var signedUrl = await _gcsService.GenerateSignedUrlAsync(filePath, TimeSpan.FromHours(1));
            await _hubContext.Clients.All.SendAsync(
                "ReceiveVideoSharedNotification",
                "New Sparring Video Uploaded!",
                uploadedVideo.Description,
                User.Identity?.Name
            );

            return Ok(new { VideoId = uploadedVideo.Id, SignedUrl = signedUrl });
        }

        [HttpPost("upload-demonstration")]
        [Authorize]
        [RequestSizeLimit(300 * 1024 * 1024)]
        public async Task<IActionResult> UploadDemonstrationAsync(IFormFile videoFile, [FromForm] string description)
        {
            if (videoFile.Length > 300 * 1024 * 1024)
                return BadRequest(new { Message = "Video file exceeds 300 MB limit." });

            if (videoFile == null || !IsValidVideoFormat(videoFile.ContentType))
                return BadRequest(new { Message = "Invalid video file" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();

            // Calculate hash of the video file
            using var stream = videoFile.OpenReadStream();
            var videoHash = await _gcsService.CalculateFileHashAsync(stream);

            // Check for duplicate video
            var existingVideo = await dbContext.UploadedVideos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.FileHash == videoHash);

            if (existingVideo != null)
            {
                return Conflict(new
                {
                    Message = "Duplicate video detected. This video has already been uploaded.",
                    VideoId = existingVideo.Id,
                    SignedUrl = await _gcsService.GenerateSignedUrlAsync(existingVideo.FilePath, TimeSpan.FromHours(1))
                });
            }

            // Reset stream position for upload
            stream.Position = 0;

            // Upload video to Google Cloud Storage
            var filePath = await _gcsService.UploadFileAsync(stream, videoFile.FileName, videoFile.ContentType);

            var uploadedVideo = new UploadedVideo
            {
                UserId = userId,
                FilePath = filePath,
                Description = description,
                UploadTimestamp = DateTime.UtcNow,
                FileHash = videoHash
            };

            dbContext.UploadedVideos.Add(uploadedVideo);
            await dbContext.SaveChangesAsync();

            var signedUrl = await _gcsService.GenerateSignedUrlAsync(filePath, TimeSpan.FromHours(1));
            await _hubContext.Clients.All.SendAsync(
                "ReceiveVideoSharedNotification",
                "New Demonstration Video Uploaded!",
                uploadedVideo.Description,
                User.Identity?.Name
            );

            return Ok(new { VideoId = uploadedVideo.Id, SignedUrl = signedUrl });
        }

        [HttpDelete("delete-uploaded/{videoId}")]
        [Authorize]
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
        public async Task<ActionResult<List<UploadedVideoDto>>> GetAllUploadedVideosAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            var videos = await (
                from v in dbContext.UploadedVideos.AsNoTracking()
                join ai in dbContext.AiAnalysisResults.AsNoTracking()
                    on v.Id equals ai.VideoId into aiGroup
                select new UploadedVideoDto
                {
                    Id = v.Id,
                    UserId = v.UserId,
                    MartialArt = v.MartialArt.ToString(),
                    FilePath = v.FilePath,
                    UploadTimestamp = v.UploadTimestamp,
                    Description = v.Description,
                    AiAnalysisResult = aiGroup.Select(a => a.AnalysisJson).FirstOrDefault(),
                }
            ).ToListAsync();

            return Ok(videos);
        }

        [HttpGet("{videoId}")]
        [Authorize]
        public async Task<IActionResult> GetUploadedVideoAsync(int videoId)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var video = await dbContext.UploadedVideos.Include(v => v.AppUser).ThenInclude(u => u.Fighter)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null)
            {
                return NotFound(new { Message = $"Video with ID {videoId} not found" });
            }

            var AiAnalysisResult = await dbContext.AiAnalysisResults
                .Where(a => a.VideoId == videoId)
                .Select(a => a.AnalysisJson)
                .FirstOrDefaultAsync();

            var signedUrl = await _gcsService.GenerateSignedUrlAsync(video.FilePath, TimeSpan.FromHours(1));

            return Ok(new UploadedVideoDto
            {
                Id = video.Id,
                UserId = video.UserId,
                FilePath = video.FilePath,
                UploadTimestamp = video.UploadTimestamp,
                Description = video.Description,
                AiAnalysisResult = AiAnalysisResult,
                SignedUrl = signedUrl,
                FighterId = video.AppUser.FighterId,
            });
        }

        [HttpGet("import-ai/{videoId}")]
        [Authorize]
        public async Task<IActionResult> ImportAiAnalysis(int videoId)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            
            var video = await dbContext.UploadedVideos.FindAsync(videoId);
            if (video == null)
            {
                return NotFound(new { Message = $"Video with ID {videoId} not found" });
            }

            var aiAnalysisResult = await dbContext.AiAnalysisResults
                .Where(a => a.VideoId == videoId)
                .Select(a => a.AnalysisJson)
                .FirstOrDefaultAsync();

            if (aiAnalysisResult == null)
            {
                return NotFound(new { Message = $"AI analysis result for video ID {videoId} not found" });
            }

            try
            {
                var service = serviceProvider.GetRequiredService<AiAnalysisProcessorService>();
                await service.ProcessAnalysisJsonAsync(aiAnalysisResult, videoId);

                return Ok(new { Message = "AI analysis processed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI analysis for video ID {VideoId}", videoId);
                return StatusCode(500, new { Message = "An error occurred while processing the AI analysis", Details = ex.Message });
            }
        }

        private bool IsValidVideoFormat(string contentType) =>
            contentType is "video/mp4" or "video/avi" or "video/mov" or "video/mpeg" or "video/webm";
    }
}

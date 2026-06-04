using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoAnalysis.Server.Domain.YoutubeSharingService;
using VideoAnalysis.Server.Repository;
using System.Net;
using System.Security.Claims;
using SharedEntities.Data;
using SharedEntities.Models;
using Microsoft.AspNetCore.SignalR;
using VideoAnalysis.Server.Domain.GoogleCloudStorageService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hangfire;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Helpers;
using VideoAnalysis.Server.Models.Dtos;
using VideoAnalysis.Server.Domain.GeminiService;

namespace VideoAnalysis.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController(IYoutubeDataService youtubeDataService,
        ISharedVideoRepository sharedVideoRepository, IServiceProvider serviceProvider,
        IGoogleCloudStorageService googleCloudStorageService,
        IHubContext<VideoShareHub> hubContext, ILogger<VideoController> logger,
        IOptions<TranscoderOptions> transcoderOptions) : ControllerBase
    {
        private readonly IYoutubeDataService _youtubeDataService = youtubeDataService;
        private readonly ISharedVideoRepository _sharedVideoRepository = sharedVideoRepository;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IGoogleCloudStorageService _gcsService = googleCloudStorageService;
        private readonly IHubContext<VideoShareHub> _hubContext = hubContext;
        private readonly ILogger<VideoController> _logger = logger;
        private readonly TranscoderOptions _transcoderOptions = transcoderOptions.Value;

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

            var video = new VideoMetadata()
            {
                YoutubeVideoId = videoId,
                Title = videoDetailsResponse.Title,
                Description = videoDetailsResponse.Description,
                Url = videoUrl,
                UploadedAt = DateTime.Now.ToUniversalTime(),
                UserId = userId,
                Type = VideoType.Shared,
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
                VideoId = v.YoutubeVideoId!,
                Title = v.Title!,
                Description = v.Description!,
                EmbedLink = $"https://www.youtube.com/embed/{v.YoutubeVideoId}",
                SharedBy = new AppUserDto
                {
                    UserId = v.UserId,
                    Username = v.AppUser.UserName!
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
            var existingVideo = await dbContext.Videos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.FileHash == videoHash);

            if (existingVideo != null)
            {
                return Conflict(new
                {
                    Message = "Duplicate video detected. This video has already been uploaded.",
                    VideoId = existingVideo.Id,
                    SignedUrl = await _gcsService.GenerateSignedUrlAsync(existingVideo.FilePath!, TimeSpan.FromHours(1))
                });
            }

            // Reset stream position for upload
            stream.Position = 0;

            // Upload video to Google Cloud Storage
            var filePath = await _gcsService.UploadFileAsync(stream, videoFile.FileName, videoFile.ContentType);

            var uploadedVideo = new VideoMetadata
            {
                UserId = userId,
                FilePath = filePath,
                Description = request.Description,
                StudentIdentifier = request.StudentIdentifier,
                MartialArt = request.MartialArt,
                UploadedAt = DateTime.UtcNow,
                FileHash = videoHash,
                Type = VideoType.StudentUpload,
            };
            var appUserEntity = dbContext.Users.Find(userId);

            dbContext.Videos.Add(uploadedVideo);
            await dbContext.SaveChangesAsync();

            var signedUrl = await _gcsService.GenerateSignedUrlAsync(filePath, TimeSpan.FromHours(1));
            await _hubContext.Clients.All.SendAsync(
                "ReceiveVideoSharedNotification",
                "New Sparring Video Uploaded!",
                uploadedVideo.Description,
                appUserEntity?.UserName
            );

            return Ok(new { VideoId = uploadedVideo.Id, SignedUrl = signedUrl });
        }

        /// <summary>
        /// Returns a signed URL the client uses to PUT the video DIRECTLY to GCS, bypassing
        /// Cloudflare's 100 MB body cap and the app server entirely. Creates the VideoMetadata row
        /// up front; the client uploads the bytes, then triggers analysis with the returned VideoId.
        /// </summary>
        [HttpPost("upload-url")]
        [Authorize]
        public async Task<IActionResult> RequestUploadUrlAsync([FromBody] UploadUrlRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest(new { Message = "FileName is required." });
            if (!IsValidVideoFormat(request.ContentType))
                return BadRequest(new { Message = "Invalid video format. Allowed: mp4, avi, mov, mpeg, webm." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();

            var martialArt = Enum.TryParse<MartialArt>(request.MartialArt, ignoreCase: true, out var parsed)
                ? parsed
                : MartialArt.BrazilianJiuJitsu_GI;

            // Same object-name convention as UploadFileAsync; the signer encodes it into the URL.
            var objectName = $"{Guid.NewGuid()}_{request.FileName}";
            var gcsPath = $"gs://{_gcsService.BucketName}/{objectName}";

            var uploadedVideo = new VideoMetadata
            {
                UserId = userId,
                FilePath = gcsPath,
                Description = request.Description,
                StudentIdentifier = request.StudentIdentifier,
                MartialArt = martialArt,
                UploadedAt = DateTime.UtcNow,
                Type = VideoType.StudentUpload,
            };
            dbContext.Videos.Add(uploadedVideo);
            await dbContext.SaveChangesAsync();

            // 2h window: a multi-GB upload must START before this expires (GCS validates at request receipt).
            var uploadUrl = await _gcsService.GenerateUploadUrlAsync(objectName, TimeSpan.FromHours(2));

            _logger.LogInformation("Issued direct-upload URL for VideoId {VideoId} ({Object})", uploadedVideo.Id, objectName);
            return Ok(new { VideoId = uploadedVideo.Id, UploadUrl = uploadUrl, GcsPath = gcsPath });
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
            var existingVideo = await dbContext.Videos
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.FileHash == videoHash);

            if (existingVideo != null)
            {
                return Conflict(new
                {
                    Message = "Duplicate video detected. This video has already been uploaded.",
                    VideoId = existingVideo.Id,
                    SignedUrl = await _gcsService.GenerateSignedUrlAsync(existingVideo.FilePath!, TimeSpan.FromHours(1))
                });
            }

            // Reset stream position for upload
            stream.Position = 0;

            // Upload video to Google Cloud Storage
            var filePath = await _gcsService.UploadFileAsync(stream, videoFile.FileName, videoFile.ContentType);

            var uploadedVideo = new VideoMetadata
            {
                UserId = userId,
                FilePath = filePath,
                Description = description,
                UploadedAt = DateTime.UtcNow,
                FileHash = videoHash,
                Type = VideoType.Demonstration,
            };

            dbContext.Videos.Add(uploadedVideo);
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
            var aiAnalysisId = dbContext.AiAnalysisResults.FirstOrDefault(a => a.VideoId == videoId)?.Id;
            await dbContext.Techniques
                .Where(t => t.AiAnalysisResultId == aiAnalysisId)
                .ExecuteDeleteAsync();
            await dbContext.Drills.Where(d => d.AiAnalysisResultId == aiAnalysisId)
                .ExecuteDeleteAsync();
                
            var video = await dbContext.Videos.FindAsync(videoId);
            if (video == null)
            {
                return NotFound(new { Message = $"Video with ID {videoId} not found" });
            }

            // Remove from database first
            dbContext.Videos.Remove(video);
            await dbContext.SaveChangesAsync();

            // then delete from GCS, might throw exception if file was already deleted
            await _gcsService.DeleteFileAsync(video.FilePath!);

            // Best-effort cleanup of the transcoded playback copy (don't fail the delete if it's missing).
            if (!string.IsNullOrEmpty(video.PlaybackFilePath))
            {
                try { await _gcsService.DeleteFileAsync(video.PlaybackFilePath); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete transcoded copy for video {VideoId}", videoId); }
            }

            return Ok(new { Message = $"Video with ID {videoId} deleted successfully" });
        }

        [HttpGet("getall-uploaded")]
        [Authorize]
        public async Task<ActionResult<List<UploadedVideoDto>>> GetAllUploadedVideosAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            var videos = await (
                from v in dbContext.Videos.AsNoTracking()
                where (v.Type == VideoType.StudentUpload || v.Type == VideoType.Demonstration)
                && v.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)
                join ai in dbContext.AiAnalysisResults.AsNoTracking()
                    on v.Id equals ai.VideoId into aiGroup
                select new UploadedVideoDto
                {
                    Id = v.Id,
                    UserId = v.UserId,
                    MartialArt = v.MartialArt.ToString(),
                    FilePath = v.FilePath!,
                    UploadTimestamp = v.UploadedAt,
                    Description = v.Description,
                    AiAnalysisResult = aiGroup.Select(a => a.AnalysisJson).FirstOrDefault(),
                    FighterName = v.AppUser.Fighter!.FighterName,
                    TranscodeStatus = v.TranscodeStatus.ToString()
                }
            ).ToListAsync();

            return Ok(videos);
        }

        [HttpGet("{videoId}")]
        [Authorize]
        public async Task<IActionResult> GetUploadedVideoAsync(int videoId)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var video = await dbContext.Videos.Include(v => v.AppUser).ThenInclude(u => u.Fighter)
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

            // Serve the H.264 playback copy when transcoding has completed (HEVC tapes only render in
            // a browser once converted); otherwise fall back to the original upload.
            var playbackSource = video.TranscodeStatus == TranscodeStatus.Ready && !string.IsNullOrEmpty(video.PlaybackFilePath)
                ? video.PlaybackFilePath!
                : video.FilePath!;
            var signedUrl = await _gcsService.GenerateSignedUrlAsync(playbackSource, TimeSpan.FromHours(1));

            return Ok(new UploadedVideoDto
            {
                Id = video.Id,
                UserId = video.UserId,
                FilePath = video.FilePath!,
                UploadTimestamp = video.UploadedAt,
                Description = video.Description,
                AiAnalysisResult = AiAnalysisResult,
                SignedUrl = signedUrl,
                FighterId = video.AppUser.FighterId,
                FighterName = video.AppUser.Fighter?.FighterName ?? string.Empty,
                StudentIdentifier = video.StudentIdentifier,
                TranscodeStatus = video.TranscodeStatus.ToString()
            });
        }

        /// <summary>
        /// Manually enqueues a playback transcode (H.264/AAC) for an already-uploaded video — used for
        /// existing tapes or to retry a failed conversion. Returns 202 and the new status; no-ops with
        /// 200 if a transcode is already in progress or complete.
        /// </summary>
        [HttpPost("{videoId}/transcode")]
        [Authorize]
        public async Task<IActionResult> RequestTranscodeAsync(int videoId)
        {
            if (!_transcoderOptions.Enabled)
                return BadRequest(new { Message = "Playback transcoding is disabled." });

            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            var video = await dbContext.Videos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null)
                return NotFound(new { Message = $"Video with ID {videoId} not found" });
            if (string.IsNullOrEmpty(video.FilePath))
                return BadRequest(new { Message = "Video has no uploaded file to transcode." });
            if (video.TranscodeStatus == TranscodeStatus.Ready)
                return Ok(new { Message = "A playback version is already available.", VideoId = videoId, TranscodeStatus = video.TranscodeStatus.ToString() });

            // Atomically claim the transcode: a single conditional UPDATE flips None/Failed→Processing only
            // if no transcode is already in flight/done. Concurrent requests (double-click, retry, or a race
            // with the analyze-v2 auto-trigger) update 0 rows on all but the winner, so exactly one Hangfire
            // job is enqueued — no duplicate, billable GCP Transcoder jobs writing the same output.
            var claimed = await dbContext.Videos
                .Where(v => v.Id == videoId
                    && v.TranscodeStatus != TranscodeStatus.Processing
                    && v.TranscodeStatus != TranscodeStatus.Ready)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.TranscodeStatus, TranscodeStatus.Processing));
            if (claimed == 0)
                return Ok(new { Message = "Transcode already in progress.", VideoId = videoId, TranscodeStatus = TranscodeStatus.Processing.ToString() });

            BackgroundJob.Enqueue<VideoTranscodeBackgroundJobService>(
                job => job.ProcessTranscodeAsync(videoId));

            _logger.LogInformation("Manual transcode enqueued for VideoId {VideoId}", videoId);
            return Accepted(new { Message = "Playback conversion is processing", VideoId = videoId, TranscodeStatus = TranscodeStatus.Processing.ToString() });
        }

        [HttpGet("import-ai/{videoId}")]
        [Authorize]
        public async Task<IActionResult> ImportAiAnalysis(int videoId)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            
            var video = await dbContext.Videos.FindAsync(videoId);
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


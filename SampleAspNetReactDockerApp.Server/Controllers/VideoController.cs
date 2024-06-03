using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetReactDockerApp.Server.Data;
using SampleAspNetReactDockerApp.Server.Helpers;
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
        private readonly object _trainingPlanRepository;
        private readonly object _userProfileRepository;

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

        // Service to upload sparring videos
        [HttpPost("upload-video")]
        public async Task<IActionResult> UploadSparringVideoAsync(IFormFile videoFile)
        {
            // Logic to upload video to Azure Blob Storage
            // Implement the logic to upload the video file to Azure Blob Storage
            // You can use the Azure.Storage.Blobs package to interact with Azure Blob Storage
            // Here's an example of how you can upload the video file to a container named "videos":
            var connectionString = "<your Azure Blob Storage connection string>";
            var containerName = "videos";
            var blobName = Guid.NewGuid().ToString();
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            using (var stream = videoFile.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }
            return Ok();
        }

        // Service to analyze sparring video and generate training plan
        [HttpPost("generate-plan")]
        public IActionResult GenerateTrainingPlan(VideoAnalysisRequestDto analysisRequest)
        {
            // Logic to process video and generate training plan
            // Implement the logic to analyze the sparring video and generate a training plan
            // You can use any video analysis library or service to perform the analysis
            // Here's an example of how you can generate a training plan:
            var videoUrl = analysisRequest.VideoUrl;
            var trainingPlan = new TrainingPlan();
            // Perform video analysis and generate the training plan
            // ...
            return Ok(trainingPlan);
        }

        // Service to retrieve a specific training plan
        [HttpGet("training-plan/{planId}")]
        public IActionResult GetTrainingPlan(Guid planId)
        {
            // Logic to retrieve a training plan by ID
            // Implement the logic to retrieve the training plan from the database or any other storage
            // Here's an example of how you can retrieve a training plan:
            var trainingPlan = _trainingPlanRepository.GetById(planId);
            if (trainingPlan == null)
            {
                return NotFound();
            }
            return Ok(trainingPlan);
        }

        // Service to update user profile
        [HttpPut("update-profile")]
        public IActionResult UpdateUserProfile(UserProfileDto profileDetails)
        {
            // Logic to update user profile information
            // Implement the logic to update the user profile information in the database or any other storage
            // Here's an example of how you can update the user profile:
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userProfile = _userProfileRepository.GetByUserId(userId);
            if (userProfile == null)
            {
                return NotFound();
            }
            userProfile.FirstName = profileDetails.FirstName;
            userProfile.LastName = profileDetails.LastName;
            // Update other profile details
            // ...
            _userProfileRepository.Update(userProfile);
            return Ok();
        }

        public class UploadVideoRequest
        {
            [JsonPropertyName("videoUrl")]
            public string VideoUrl { get; set; }
        }
    }
}

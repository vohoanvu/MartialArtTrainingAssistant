using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.GeminiService;
using VideoSharing.Server.Models.Dtos;

namespace VideoSharing.Server.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class GeminiController : ControllerBase
    {
        private readonly IGeminiVisionService _geminiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GeminiController> _logger;

        public GeminiController(IGeminiVisionService geminiService, IServiceProvider serviceProvider, ILogger<GeminiController> logger)
        {
            _geminiService = geminiService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [HttpPost("analyze")]
        [Authorize]
        public async Task<IActionResult> AnalyzeVideo([FromBody] VideoAnalysisRequest request)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            if (!await dbContext.UploadedVideos.AnyAsync(v => v.Id == request.VideoId))
                return BadRequest("Invalid VideoId.");

            var uploadedVideo = await dbContext.UploadedVideos.FindAsync(request.VideoId);

            try
            {
                // Call the Gemini Vision API asynchronously
                var visionAnalysisResult = await _geminiService.AnalyzeVideoAsync(uploadedVideo!.FilePath, uploadedVideo.MartialArt.ToString(), uploadedVideo.StudentIdentifier);

                // Validate the response is valid JSON
                string? structuredJson = ValidateStructuredJson(visionAnalysisResult.AnalysisJson);
                if (structuredJson == null)
                    return StatusCode(500, "Invalid or empty JSON response from the API.");

                var aiResult = new AiAnalysisResult
                {
                    VideoId = request.VideoId,
                    AnalysisJson = structuredJson ?? string.Empty
                };

                dbContext.AiAnalysisResults.Add(aiResult);
                await dbContext.SaveChangesAsync();

                return Ok(aiResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing video with path {Path}", uploadedVideo!.FilePath);
                return StatusCode(500, $"Error analyzing video: {ex.Message}");
            }
        }

        [HttpGet("{videoId}/feedback")]
        [Authorize]
        public async Task<ActionResult> GeAIFeedback(int videoId)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            var video = await dbContext.UploadedVideos.FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null)
            {
                return NotFound(new { Message = $"Video with ID {videoId} not found" });
            }

            var aiAnalysisJson = await dbContext.AiAnalysisResults
                .Where(a => a.VideoId == videoId)
                .Select(a => a.AnalysisJson)
                .FirstOrDefaultAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aiAnalysisResultDto = JsonSerializer.Deserialize<AiAnalysisResultResponse>(aiAnalysisJson!, options)
                ?? throw new InvalidOperationException("Failed to deserialize JSON.");

            return Ok(aiAnalysisResultDto);
        }

        [HttpPost("{id}/feedback")]
        [Authorize]
        public async Task<ActionResult> SaveFeedback(int id, [FromBody] string feedback)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            return Ok();
        }

        private static string? ValidateStructuredJson(string apiResponseJson)
        {
            if (string.IsNullOrWhiteSpace(apiResponseJson))
                return null;

            try
            {
                // Validate that the response is valid JSON
                JsonDocument.Parse(apiResponseJson);
                return apiResponseJson;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
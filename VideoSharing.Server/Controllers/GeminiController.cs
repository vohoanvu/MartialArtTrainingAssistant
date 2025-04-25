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
                var visionAnalysisResult = await _geminiService.AnalyzeVideoAsync(uploadedVideo!.FilePath);

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

        [HttpGet("{id}/feedback")]
        [Authorize]
        public async Task<ActionResult> GetFeedback(int id)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            var humanFeedback = await dbContext.HumanFeedbacks.Where(f => f.VideoId == id).ToListAsync();
            var aiFeedback = await dbContext.AiFeedbacks.Where(f => f.VideoId == id).ToListAsync();
            return Ok(new { HumanFeedback = humanFeedback, AiFeedback = aiFeedback });
        }

        [HttpPost("{id}/feedback")]
        [Authorize]
        public async Task<ActionResult> AddFeedback(int id, [FromBody] HumanFeedback feedback)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            feedback.VideoId = id;
            feedback.InstructorId = userId;
            dbContext.HumanFeedbacks.Add(feedback);
            await dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFeedback), new { id }, feedback); 
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
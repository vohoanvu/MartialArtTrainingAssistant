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

        [HttpPost("analyze/{videoId}")]
        [Authorize]
        public async Task<IActionResult> AnalyzeVideoAsync(int videoId)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            if (!await dbContext.Videos.AnyAsync(v => v.Id == videoId))
                return BadRequest("Invalid VideoId.");

            var uploadedVideo = await dbContext.Videos.FindAsync(videoId);
            var appUserFighter = await dbContext.Users.Include(u => u.Fighter).FirstAsync(u => u.Id == uploadedVideo!.UserId);

            try
            {
                // Call the Gemini Vision API asynchronously
                var visionAnalysisResult = await _geminiService.AnalyzeVideoAsync(
                    uploadedVideo!.FilePath!,
                    uploadedVideo.MartialArt.ToString(),
                    uploadedVideo.StudentIdentifier ?? "Fighter in the video",
                    uploadedVideo.Description ?? "sparring tape",
                    appUserFighter.Fighter!.BelkRank.ToString()
                );

                // Validate the response is valid JSON
                string? structuredJson = ValidateStructuredJson(visionAnalysisResult.AnalysisJson);
                if (structuredJson == null)
                    return StatusCode(500, "Invalid or empty JSON response from the API.");

                // Deserialize JSON to extract Strengths, AreasForImprovement, and OverallDescription
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var analysis = JsonSerializer.Deserialize<AiAnalysisResultResponse>(structuredJson, options)
                    ?? throw new InvalidOperationException("Failed to deserialize JSON response.");

                var existingAiResult = await dbContext.AiAnalysisResults
                    .FirstOrDefaultAsync(a => a.VideoId == videoId);

                AiAnalysisResult aiResult;
                if (existingAiResult != null)
                {
                    // Update existing record
                    _logger.LogInformation("Updating existing AiAnalysisResult for VideoId {VideoId}.", videoId);
                    existingAiResult.AnalysisJson = structuredJson;
                    existingAiResult.Strengths = JsonSerializer.Serialize(analysis.Strengths ?? new List<Strength>());
                    existingAiResult.AreasForImprovement = JsonSerializer.Serialize(analysis.AreasForImprovement ?? new List<AreaForImprovement>());
                    existingAiResult.OverallDescription = analysis.OverallDescription;
                    aiResult = existingAiResult;
                }
                else
                {
                    // Create new record
                    _logger.LogInformation("Creating new AiAnalysisResult for VideoId {VideoId}.", videoId);
                    var newAiResult = new AiAnalysisResult
                    {
                        VideoId = videoId,
                        AnalysisJson = structuredJson,
                        Strengths = JsonSerializer.Serialize(analysis.Strengths ?? new List<Strength>()),
                        AreasForImprovement = JsonSerializer.Serialize(analysis.AreasForImprovement ?? new List<AreaForImprovement>()),
                        OverallDescription = analysis.OverallDescription,
                        Techniques = new List<Techniques>(),
                        Drills = new List<Drills>(),
                        GeneratedAt = DateTime.UtcNow,
                        UpdatedBy = uploadedVideo.UserId
                    };
                    dbContext.AiAnalysisResults.Add(newAiResult);
                    aiResult = newAiResult;
                }

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
        public async Task<ActionResult<AnalysisResultDto>> GetVideoAnalysisResult(int videoId)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            var video = await dbContext.Videos.FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null)
            {
                return NotFound(new { Message = $"Video with ID {videoId} not found" });
            }

            try
            {
                var aiAnalysisService = _serviceProvider.GetRequiredService<AiAnalysisProcessorService>();
                var aiAnalysisResultDto = await aiAnalysisService.GetAnalysisResultDtoByVideoId(videoId);

                return Ok(aiAnalysisResultDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Analysis not found for videoId {VideoId}", videoId);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating analysis for videoId {VideoId}", videoId);
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }

        [HttpPatch("{videoId}/analysis")]
        [Authorize]
        public async Task<ActionResult<AnalysisResultDto>> UpdateAnalysisAsync(int videoId, [FromBody] PartialAnalysisResultDto partialAnalysisDto)
        {
            if (partialAnalysisDto == null)
            {
                _logger.LogWarning("Received null partial analysis DTO for videoId {VideoId}", videoId);
                return BadRequest(new { error = "Request body cannot be null" });
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var aiAnalysisService = _serviceProvider.GetRequiredService<AiAnalysisProcessorService>();
                var updatedAnalysis = await aiAnalysisService.SavePartialAnalysisResultDtoByVideoId(videoId, partialAnalysisDto);
                _logger.LogInformation("Partial analysis updated successfully for videoId {VideoId} by user {UserId}", videoId, userId);
                return Ok(updatedAnalysis);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Analysis not found for videoId {VideoId}", videoId);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating analysis for videoId {VideoId}", videoId);
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }

        [HttpGet("session/{sessionId}/generate")]
        [Authorize]
        public async Task<IActionResult> GenerateCurriculumAsync(int sessionId)
        {
            using var scope = _serviceProvider.CreateScope();
            var curriculumService = scope.ServiceProvider.GetRequiredService<CurriculumRecommendationService>();

            try
            {
                var curriculum = await curriculumService.GenerateCurriculumAsync(sessionId);
                return Ok(curriculum);
            }
            catch (BadHttpRequestException ex)
            {
                _logger.LogWarning(ex, "Invalid sessionId {SessionId} for curriculum generation.", sessionId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating curriculum for sessionId {SessionId}", sessionId);
                return StatusCode(500, new { error = "An unexpected error occurred while generating the curriculum." });
            }
        }

        [HttpGet("{sessionId}/curriculum")]
        [Authorize]
        public async Task<IActionResult> GetCurriculumAsync(int sessionId)
        {
            using var scope = _serviceProvider.CreateScope();
            var curriculumService = scope.ServiceProvider.GetRequiredService<CurriculumRecommendationService>();

            try
            {
                var curriculum = await curriculumService.GetCurriculumJsonBlob(sessionId);
                return Ok(curriculum);
            }
            catch (BadHttpRequestException ex)
            {
                _logger.LogWarning(ex, "Invalid sessionId {SessionId} for curriculum generation.", sessionId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating curriculum for sessionId {SessionId}", sessionId);
                return StatusCode(500, new { error = "An unexpected error occurred while generating the curriculum." });
            }
        }

        [HttpPost("matchmaker/{sessionId}")]
        [Authorize]
        public async Task<IActionResult> SuggestStudentPairs(int sessionId, [FromBody] MatchMakerDto request)
        {
            using var scope = _serviceProvider.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
            if (request.StudentFighterIds == null || request.StudentFighterIds.Count == 0)
            {
                return BadRequest("StudentFighterIds cannot be null or empty.");
            }

            var studentFighters = await databaseContext.Fighters
                .Where(f => request.StudentFighterIds.Contains(f.Id))
                .ToListAsync();
            if (studentFighters.Count == 0)
            {
                return BadRequest("No valid students found for the given IDs.");
            }
            var instructor = await databaseContext.Fighters
                .FirstOrDefaultAsync(f => f.Id == request.InstructorFighterId);
            if (instructor == null)
            {
                return BadRequest("No valid instructor found for the given ID.");
            }

            var trainingSession = await databaseContext.TrainingSessions
                .Include(ts => ts.Students)
                .FirstOrDefaultAsync(ts => ts.Id == sessionId);
            if (trainingSession == null)
            {
                return NotFound("Training session not found.");
            }

            var pairMatchingAIResponse = await _geminiService.SuggestFighterPairs(studentFighters, trainingSession);
            if (pairMatchingAIResponse == null)
            {
                return StatusCode(500, "Failed to generate pairs.");
            }

            return Ok(pairMatchingAIResponse);
        }

        private static string? ValidateStructuredJson(string apiResponseJson)
        {
            if (string.IsNullOrWhiteSpace(apiResponseJson))
                return null;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var analysis = JsonSerializer.Deserialize<AiAnalysisResultResponse>(apiResponseJson, options);
                if (analysis == null)
                    return null;
                return apiResponseJson;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
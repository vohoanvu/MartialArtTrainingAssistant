using System.Security.Claims;
using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.GeminiService;
using VideoSharing.Server.Helpers;
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

            // Enqueue background job for video analysis; returns a jobId if needed.
            BackgroundJob.Enqueue<VideoAnalysisBackgroundJobService>(
                job => job.ProcessVideoAnalysisAsync(videoId)
            );

            // Return 202 Accepted to indicate the analysis is processing
            return Accepted(new { Message = "Video analysis is processing", VideoId = videoId });
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
                .AsNoTracking()
                .Where(f => request.StudentFighterIds.Contains(f.Id))
                .ToListAsync();
            if (studentFighters.Count == 0)
            {
                return BadRequest("No valid students found for the given IDs.");
            }
            var instructor = await databaseContext.Fighters.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == request.InstructorFighterId);
            if (instructor == null)
            {
                return BadRequest("No valid instructor found for the given ID.");
            }

            var trainingSession = await databaseContext.TrainingSessions
                .Include(ts => ts.Instructor)
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

            if (pairMatchingAIResponse.IsSuccessfullyParsed && pairMatchingAIResponse.SuggestedPairings?.Pairs.Count != 0)
            {
                trainingSession.RawFighterPairsJson = JsonSerializer.Serialize(pairMatchingAIResponse.SuggestedPairings);
                databaseContext.TrainingSessions.Update(trainingSession);
                await databaseContext.SaveChangesAsync();
            }

            return Ok(pairMatchingAIResponse);
        }

        // private static string? ValidateStructuredJson(string apiResponseJson)
        // {
        //     if (string.IsNullOrWhiteSpace(apiResponseJson))
        //         return null;

        //     try
        //     {
        //         var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        //         var analysis = JsonSerializer.Deserialize<AiAnalysisResultResponse>(apiResponseJson, options);
        //         if (analysis == null)
        //             return null;
        //         return apiResponseJson;
        //     }
        //     catch (JsonException)
        //     {
        //         return null;
        //     }
        // }
    }
}
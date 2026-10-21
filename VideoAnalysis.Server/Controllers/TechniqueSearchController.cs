using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedEntities.Data;
using VideoAnalysis.Server.Domain.AIServices;

namespace VideoAnalysis.Server.Controllers
{
    /// <summary>
    /// Technique resource search for class sessions: Gemini + Grounding with Google Search, with every YouTube
    /// result verified by the YouTube Data API (replaces the xAI Grok live search). Route kept for client compatibility.
    /// </summary>
    [Route("api/youtube")]
    [ApiController]
    [Authorize]
    public class TechniqueSearchController(ITechniqueResourceSearchService searchService, IServiceProvider serviceProvider) : ControllerBase
    {
        private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        private readonly ITechniqueResourceSearchService _searchService = searchService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        [HttpPost("search")]
        public async Task<IActionResult> SearchVideos([FromBody] VideoSearchRequest request)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
            try
            {
                var trainingSession = await dbContext.TrainingSessions.FindAsync(request.TrainingSessionId);
                if (trainingSession == null)
                {
                    return NotFound(new { message = "Training session not found" });
                }

                var results = await _searchService.SearchAsync(request.TechniqueName, trainingSession, HttpContext.RequestAborted);
                return Ok(JsonSerializer.Serialize(results, SnakeCase));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred during video search", details = ex.Message });
            }
        }
    }

    public class VideoSearchRequest
    {
        public required string TechniqueName { get; set; }
        public int TrainingSessionId { get; set; }
    }
}

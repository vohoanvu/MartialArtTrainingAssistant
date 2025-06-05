using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedEntities.Data;
using VideoSharing.Server.Domain.AIServices;

namespace VideoSharing.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GrokController(IXAIService searchService, IServiceProvider serviceProvider) : ControllerBase
    {
        private readonly IXAIService _searchService = searchService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        [HttpPost("search")]
        public async Task<IActionResult> SearchVideos([FromBody] GrokLiveSearchRequest request)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            try
            {
                var trainingSession = await dbContext.TrainingSessions.FindAsync(request.TrainingSessionId);
                if (trainingSession == null)
                {
                    return NotFound(new { message = "Training session not found" });
                }

                var markdownContent = await _searchService.SearchVideosAsync(request.TechniqueName, trainingSession);
                return Ok(markdownContent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred during video search", details = ex.Message });
            }
        }
    }

    /// <inheritdoc/>
    public class GrokLiveSearchRequest
    {
        /// <inheritdoc/>
        public required string TechniqueName { get; set; }
        /// <inheritdoc/>
        public int TrainingSessionId { get; set; }
    }
}
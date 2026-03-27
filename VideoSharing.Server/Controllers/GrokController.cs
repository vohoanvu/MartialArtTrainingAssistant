using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.YoutubeSharingService;

namespace VideoSharing.Server.Controllers
{
    [Route("api/youtube")]
    [ApiController]
    [Authorize]
    public class YoutubeSearchController(IYoutubeDataService youtubeDataService, IServiceProvider serviceProvider) : ControllerBase
    {
        private readonly IYoutubeDataService _youtubeDataService = youtubeDataService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        [HttpPost("search")]
        public async Task<IActionResult> SearchVideos([FromBody] VideoSearchRequest request)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            try
            {
                var trainingSession = await dbContext.TrainingSessions.FindAsync(request.TrainingSessionId);
                if (trainingSession == null)
                {
                    return NotFound(new { message = "Training session not found" });
                }

                var query = BuildSearchQuery(request.TechniqueName, trainingSession.MartialArt);
                var results = await _youtubeDataService.SearchVideosAsync(query);
                return Ok(JsonSerializer.Serialize(
                    new { youtube_videos = results },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred during video search", details = ex.Message });
            }
        }

        internal static string BuildSearchQuery(string techniqueName, MartialArt martialArt)
        {
            // Use text before colon if present (e.g. "Guard Retention: The Shin-Shield to Frames" → "Guard Retention")
            var name = techniqueName.Contains(':')
                ? techniqueName.Split(':')[0].Trim()
                : techniqueName;

            // Convert enum like "BrazilianJiuJitsu_GI" → "BJJ Gi"
            var artLabel = martialArt switch
            {
                MartialArt.BrazilianJiuJitsu_GI => "BJJ Gi",
                MartialArt.BrazilianJiuJitsu_NO_GI => "BJJ No-Gi",
                _ => Regex.Replace(martialArt.ToString(), "([a-z])([A-Z])", "$1 $2").Replace("_", " ")
            };

            return $"{name} {artLabel} tutorial";
        }
    }

    public class VideoSearchRequest
    {
        public required string TechniqueName { get; set; }
        public int TrainingSessionId { get; set; }
    }
}

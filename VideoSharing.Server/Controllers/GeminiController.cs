using Microsoft.AspNetCore.Mvc;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.GeminiService;

namespace VideoSharing.Server.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class GeminiController(IGeminiVisionService geminiService, IServiceProvider context) : ControllerBase
    {
        private readonly IGeminiVisionService _geminiService = geminiService;
        private readonly IServiceProvider _serviceProvider = context;

        public class VideoAnalysisRequest
        {
            public int VideoId { get; set; }
            public string VideoStoragePath { get; set; } = string.Empty;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeVideo([FromBody] VideoAnalysisRequest request)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            if(string.IsNullOrWhiteSpace(request.VideoStoragePath))
                return BadRequest("VideoStoragePath is required.");

            // Call the Gemini Vision API asynchronously
            var visionAnalysisResult = await _geminiService.AnalyzeVideoAsync(request.VideoStoragePath);

            // Store the analysis result in the database linked to the video.
            var aiResult = new AiAnalysisResult
            {
                VideoId = request.VideoId,
                AnalysisJson = visionAnalysisResult.AnalysisJson
            };

            dbContext.AiAnalysisResults.Add(aiResult);
            await dbContext.SaveChangesAsync();

            return Ok(aiResult);
        }
    }
}
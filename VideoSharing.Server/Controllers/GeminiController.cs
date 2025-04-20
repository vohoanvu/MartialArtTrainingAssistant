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
            public string VideoUrl { get; set; } = string.Empty;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeVideo([FromBody] VideoAnalysisRequest request)
        {
            var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();
            if(string.IsNullOrWhiteSpace(request.VideoUrl))
                return BadRequest("VideoUrl is required.");

            // Call the Gemini Vision API asynchronously
            var analysisResult = await _geminiService.AnalyzeVideoAsync(request.VideoUrl);

            // Store the analysis result in the database linked to the video.
            var aiResult = new AiAnalysisResult
            {
                VideoId = request.VideoId,
                AnalysisJson = analysisResult.AnalysisJson
            };

            dbContext.AiAnalysisResults.Add(aiResult);
            await dbContext.SaveChangesAsync();

            return Ok(analysisResult);
        }
    }
}
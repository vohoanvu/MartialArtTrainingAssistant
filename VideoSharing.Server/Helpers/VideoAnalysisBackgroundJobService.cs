using VideoSharing.Server.Domain.GeminiService;
using SharedEntities.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SharedEntities.Models;
using Microsoft.AspNetCore.SignalR;
using VideoSharing.Server.Domain.YoutubeSharingService;

namespace VideoSharing.Server.Helpers
{
    /// <inheritdoc/>
    public class VideoAnalysisBackgroundJobService
    {
        private readonly MyDatabaseContext _context;
        private readonly IGeminiVisionService _geminiService;
        private readonly ILogger<VideoAnalysisBackgroundJobService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<VideoAnalysisHub> _hubContext;

        public VideoAnalysisBackgroundJobService(
            MyDatabaseContext context,
            IGeminiVisionService geminiService,
            ILogger<VideoAnalysisBackgroundJobService> logger,
            IServiceProvider serviceProvider,
            IHubContext<VideoAnalysisHub> hubContext)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        /// <inheritdoc/>
        public async Task ProcessVideoAnalysisAsync(int videoId)
        {
            var uploadedVideo = await _context.Videos.FindAsync(videoId);
            var appUserFighter = await _context.Users.Include(u => u.Fighter).FirstAsync(u => u.Id == uploadedVideo!.UserId);

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
                {
                    _logger.LogError("Invalid JSON structure: {json}", visionAnalysisResult.AnalysisJson);
                    throw new InvalidOperationException("Invalid JSON structure returned from Gemini Vision API.");
                }

                // Deserialize JSON to extract Strengths, AreasForImprovement, and OverallDescription
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var analysis = JsonSerializer.Deserialize<AiAnalysisResultResponse>(structuredJson, options)
                    ?? throw new InvalidOperationException("Failed to deserialize JSON response.");

                var existingAiResult = await _context.AiAnalysisResults
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
                        Strengths = JsonSerializer.Serialize(analysis.Strengths ?? []),
                        AreasForImprovement = JsonSerializer.Serialize(analysis.AreasForImprovement ?? []),
                        OverallDescription = analysis.OverallDescription,
                        Techniques = [],
                        Drills = [],
                        GeneratedAt = DateTime.UtcNow,
                        UpdatedBy = uploadedVideo.UserId
                    };
                    _context.AiAnalysisResults.Add(newAiResult);
                    aiResult = newAiResult;
                }

                await _context.SaveChangesAsync();

                var analysisProcessorService = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AiAnalysisProcessorService>();
                await analysisProcessorService.ProcessAnalysisJsonAsync(aiResult.AnalysisJson, videoId);

                await _hubContext.Clients.All.SendAsync("AnalysisCompleted", videoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing video with path {Path}", uploadedVideo!.FilePath);
                throw new InvalidOperationException("Error analyzing video.", ex);
            }
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
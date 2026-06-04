using Hangfire;
using VideoAnalysis.Server.Domain.AIServices;

namespace VideoAnalysis.Server.Helpers
{
    /// <summary>
    /// Hangfire entry point for the agentic (v2) pipeline. Runs on a dedicated "vertex-pipeline"
    /// queue whose worker count caps concurrent Vertex jobs (see Program.cs).
    /// </summary>
    public class AgenticAnalysisBackgroundJobService
    {
        private readonly IAgenticPipelineService _pipeline;
        private readonly ILogger<AgenticAnalysisBackgroundJobService> _logger;

        public AgenticAnalysisBackgroundJobService(
            IAgenticPipelineService pipeline,
            ILogger<AgenticAnalysisBackgroundJobService> logger)
        {
            _pipeline = pipeline;
            _logger = logger;
        }

        [Queue("vertex-pipeline")]
        public async Task ProcessAgenticAnalysisAsync(int videoId)
        {
            _logger.LogInformation("Starting agentic analysis pipeline for video {VideoId}.", videoId);
            await _pipeline.RunPipelineAsync(videoId, CancellationToken.None);
        }
    }
}

using Hangfire;
using VideoAnalysis.Server.Domain.AccessControl;
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
        private readonly IAnalysisQuotaService _quota;
        private readonly ILogger<AgenticAnalysisBackgroundJobService> _logger;

        public AgenticAnalysisBackgroundJobService(
            IAgenticPipelineService pipeline,
            IAnalysisQuotaService quota,
            ILogger<AgenticAnalysisBackgroundJobService> logger)
        {
            _pipeline = pipeline;
            _quota = quota;
            _logger = logger;
        }

        // Attempts = 0: Hangfire's default 10 retries would re-run (and re-bill) the whole 4-phase Vertex
        // pipeline for one failure; VertexRestClient already retries transient errors per call. A failure is
        // final: the pipeline marks the result Failed + notifies, and the user's quota use is refunded here.
        [AutomaticRetry(Attempts = 0)]
        [Queue("vertex-pipeline")]
        public async Task ProcessAgenticAnalysisAsync(int videoId)
        {
            _logger.LogInformation("Starting agentic analysis pipeline for video {VideoId}.", videoId);
            try
            {
                await _pipeline.RunPipelineAsync(videoId, CancellationToken.None);
            }
            catch
            {
                try { await _quota.RefundAsync(videoId, AnalysisKinds.V2); }
                catch (Exception refundEx) { _logger.LogError(refundEx, "Quota refund failed for video {VideoId}.", videoId); }
                throw;
            }
        }
    }
}

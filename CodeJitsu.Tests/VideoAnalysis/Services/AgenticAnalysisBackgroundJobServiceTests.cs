using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using VideoAnalysis.Server.Domain.AccessControl;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Helpers;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

public class AgenticAnalysisBackgroundJobServiceTests
{
    private static (AgenticAnalysisBackgroundJobService job, Mock<IAgenticPipelineService> pipeline, Mock<IAnalysisQuotaService> quota) Create()
    {
        var pipeline = new Mock<IAgenticPipelineService>();
        var quota = new Mock<IAnalysisQuotaService>();
        var job = new AgenticAnalysisBackgroundJobService(pipeline.Object, quota.Object, new Mock<ILogger<AgenticAnalysisBackgroundJobService>>().Object);
        return (job, pipeline, quota);
    }

    [Fact]
    public async Task Should_RefundQuota_And_Rethrow_When_PipelineFails()
    {
        var (job, pipeline, quota) = Create();
        pipeline.Setup(p => p.RunPipelineAsync(9, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Vertex 400"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.ProcessAgenticAnalysisAsync(9));

        quota.Verify(q => q.RefundAsync(9, AnalysisKinds.V2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_NotRefund_When_PipelineSucceeds()
    {
        var (job, _, quota) = Create();

        await job.ProcessAgenticAnalysisAsync(9);

        quota.Verify(q => q.RefundAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Should_DisableHangfireRetries_SoAFailureIsNeverRebilled()
    {
        var method = typeof(AgenticAnalysisBackgroundJobService).GetMethod(nameof(AgenticAnalysisBackgroundJobService.ProcessAgenticAnalysisAsync))!;
        var retry = (AutomaticRetryAttribute)Attribute.GetCustomAttribute(method, typeof(AutomaticRetryAttribute))!;
        Assert.Equal(0, retry.Attempts);
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Domain.YoutubeSharingService;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

public class AgenticPipelineServiceTests
{
    private const string VisualDna = "A muscular athlete in a navy-blue gi with a white belt and red athletic tape on both hands.";

    private const string EventsJson = """
        {"events":[
          {"start_timestamp_ms":1000,"end_timestamp_ms":3000,"actor":"Student","technique_category":"Takedown","technique_name":"Double Leg","position_before":"Standing","position_after":"SideControl","outcome":"Successful","guard_type":null,"submission_type":null,"actions_description":"Shot a double leg","confidence":0.9},
          {"start_timestamp_ms":5000,"end_timestamp_ms":7000,"actor":"Opponent","technique_category":"Sweep","technique_name":"Scissor Sweep","position_before":"ClosedGuard","position_after":"Mount","outcome":"Successful","guard_type":"ClosedGuard","submission_type":null,"actions_description":"Swept from guard","confidence":0.4}
        ]}
        """;

    private const string CoachingJson = """
        {"match_summary":"A competitive match with strong takedowns.","technical_grade":78,"grade_label":"Solid Fundamentals","elite_tip":"Keep your elbows tight during the scramble.",
         "key_strengths":[{"title":"Strong takedowns","explanation":"Clean double leg entry","timestamp_start_ms":1000,"timestamp_end_ms":3000}],
         "critical_weaknesses":[{"title":"Guard retention","explanation":"Got swept from closed guard","timestamp_start_ms":5000,"timestamp_end_ms":7000,"severity":"Major","scoring_impact":"2 points conceded"}],
         "prescribed_drills":[
           {"drill_name":"Guard retention drill","instructions":"Retain guard against passing","goal":"Stop sweeps"},
           {"drill_name":"Takedown entries","instructions":"Drill doubles","goal":"Improve entries"},
           {"drill_name":"Sweep defense","instructions":"Maintain base","goal":"Defend sweeps"}
         ]}
        """;

    private static async Task<int> SeedAsync(MyDatabaseContext db, int videoId = 1)
    {
        var fighter = TestFixtures.CreateFighter(id: 1, beltColor: BeltColor.Blue);
        db.Fighters.Add(fighter);
        var user = TestFixtures.CreateAppUser(id: "user-1", fighterId: 1);
        db.Users.Add(user);
        var video = TestFixtures.CreateStudentUploadMetadata(id: videoId, userId: "user-1");
        db.Videos.Add(video);
        await db.SaveChangesAsync();
        return videoId;
    }

    private static (AgenticPipelineService svc, Mock<IVertexRestClient> vertex, List<string> sent)
        CreateService(MyDatabaseContext db, Action<Mock<IVertexRestClient>> configureVertex, VertexAiPipelineOptions? opts = null)
    {
        var vertex = new Mock<IVertexRestClient>();
        vertex.Setup(v => v.ModelResourcePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("projects/p/locations/us-central1/publishers/google/models/m");
        vertex.Setup(v => v.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(JsonElement));
        configureVertex(vertex);

        var sent = new List<string>();
        var clientProxy = new Mock<IClientProxy>();
        clientProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, args, _) =>
                sent.Add(args.Length > 1 ? $"{method}:{args[1]}" : method))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(clientProxy.Object);
        var hub = new Mock<IHubContext<VideoAnalysisHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        var svc = new AgenticPipelineService(
            vertex.Object, db,
            Options.Create(opts ?? new VertexAiPipelineOptions()),
            hub.Object,
            new Mock<ILogger<AgenticPipelineService>>().Object);
        return (svc, vertex, sent);
    }

    private static void SetupHappyPath(Mock<IVertexRestClient> vertex)
    {
        vertex.Setup(v => v.CreateCachedContentAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("projects/p/locations/us-central1/cachedContents/abc");
        vertex.Setup(v => v.DeleteCachedContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        vertex.SetupSequence(v => v.ExtractText(It.IsAny<JsonElement>()))
            .Returns(VisualDna)     // Phase 1
            .Returns(EventsJson)    // Phase 2
            .Returns(EventsJson)    // Phase 3 (verifier returns same shape)
            .Returns(CoachingJson); // Phase 4
    }

    [Fact]
    public async Task Should_PersistFullResult_When_PipelineSucceeds()
    {
        using var db = InMemoryDbContextFactory.Create();
        var videoId = await SeedAsync(db);
        var (svc, vertex, sent) = CreateService(db, SetupHappyPath);

        await svc.RunPipelineAsync(videoId, CancellationToken.None);

        var result = await db.AiAnalysisResults
            .Include(a => a.MatchEvents)
            .Include(a => a.CoachingReport!).ThenInclude(c => c.Strengths)
            .Include(a => a.CoachingReport!).ThenInclude(c => c.Weaknesses)
            .Include(a => a.CoachingReport!).ThenInclude(c => c.PrescribedDrills)
            .FirstAsync(a => a.VideoId == videoId);

        Assert.Equal(VisualDna, result.VisualDna);
        Assert.Equal(AnalysisPipelineStatus.Complete, result.PipelineStatus);
        Assert.Equal(2, result.MatchEvents.Count);
        Assert.Equal(78, result.TechnicalGrade);
        Assert.Equal("Solid Fundamentals", result.GradeLabel);
        Assert.False(string.IsNullOrEmpty(result.EliteTip));
        Assert.NotNull(result.CoachingReport);
        Assert.Single(result.CoachingReport!.Strengths);
        Assert.Single(result.CoachingReport!.Weaknesses);
        Assert.Equal(3, result.CoachingReport!.PrescribedDrills.Count);
        Assert.Equal(WeaknessSeverity.Major, result.CoachingReport!.Weaknesses.First().Severity);

        // Sequence index preserves model order; enums parsed from strings.
        var first = result.MatchEvents.OrderBy(e => e.SequenceIndex).First();
        Assert.Equal(EventActor.Student, first.Actor);
        Assert.Equal(EventOutcome.Successful, first.Outcome);
    }

    [Fact]
    public async Task Should_EmitStatusTransitions_InOrder()
    {
        using var db = InMemoryDbContextFactory.Create();
        var videoId = await SeedAsync(db);
        var (svc, _, sent) = CreateService(db, SetupHappyPath);

        await svc.RunPipelineAsync(videoId, CancellationToken.None);

        Assert.Contains("AnalysisStatusChanged:Profiling", sent);
        Assert.Contains("AnalysisStatusChanged:Logging", sent);
        Assert.Contains("AnalysisStatusChanged:Verifying", sent);
        Assert.Contains("AnalysisStatusChanged:Coaching", sent);
        Assert.Contains("AnalysisStatusChanged:Complete", sent);
        Assert.Contains(sent, s => s.StartsWith("AnalysisV2Completed"));
    }

    [Fact]
    public async Task Should_CreateAndDeleteCache_OnSuccess()
    {
        using var db = InMemoryDbContextFactory.Create();
        var videoId = await SeedAsync(db);
        var (svc, vertex, _) = CreateService(db, SetupHappyPath);

        await svc.RunPipelineAsync(videoId, CancellationToken.None);

        vertex.Verify(v => v.CreateCachedContentAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        vertex.Verify(v => v.DeleteCachedContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_CompleteViaFallback_When_CacheCreationFails()
    {
        using var db = InMemoryDbContextFactory.Create();
        var videoId = await SeedAsync(db);
        var (svc, vertex, _) = CreateService(db, v =>
        {
            v.Setup(x => x.CreateCachedContentAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("cache too small"));
            v.SetupSequence(x => x.ExtractText(It.IsAny<JsonElement>()))
                .Returns(VisualDna).Returns(EventsJson).Returns(EventsJson).Returns(CoachingJson);
        });

        await svc.RunPipelineAsync(videoId, CancellationToken.None);

        var result = await db.AiAnalysisResults.Include(a => a.MatchEvents).FirstAsync(a => a.VideoId == videoId);
        Assert.Equal(AnalysisPipelineStatus.Complete, result.PipelineStatus);
        Assert.Equal(2, result.MatchEvents.Count);
        // No cache was created → nothing to delete.
        vertex.Verify(v => v.DeleteCachedContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_MarkFailedAndCleanupCache_When_PhaseThrows()
    {
        using var db = InMemoryDbContextFactory.Create();
        var videoId = await SeedAsync(db);
        var (svc, vertex, sent) = CreateService(db, v =>
        {
            v.Setup(x => x.CreateCachedContentAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("projects/p/locations/us-central1/cachedContents/abc");
            v.Setup(x => x.DeleteCachedContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            v.SetupSequence(x => x.ExtractText(It.IsAny<JsonElement>()))
                .Returns(VisualDna)
                .Returns("this is not valid json");  // Phase 2 parse fails
        });

        await Assert.ThrowsAsync<JsonException>(() => svc.RunPipelineAsync(videoId, CancellationToken.None));

        var result = await db.AiAnalysisResults.FirstAsync(a => a.VideoId == videoId);
        Assert.Equal(AnalysisPipelineStatus.Failed, result.PipelineStatus);
        Assert.False(string.IsNullOrEmpty(result.PipelineError));
        Assert.Contains(sent, s => s.StartsWith("AnalysisV2Failed"));
        // Cache created in Phase 1 must still be cleaned up in finally.
        vertex.Verify(v => v.DeleteCachedContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

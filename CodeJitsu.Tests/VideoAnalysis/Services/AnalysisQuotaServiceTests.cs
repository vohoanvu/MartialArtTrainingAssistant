using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Domain.AccessControl;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

public class AnalysisQuotaServiceTests
{
    private const string UserId = "user-1";
    private const string AdminId = "admin-1";

    private static MyDatabaseContext SeedUsers()
    {
        var db = InMemoryDbContextFactory.Create();
        db.Users.Add(TestFixtures.CreateAppUser(id: UserId, userName: "coach", email: "coach@codejitsu.com", fighterId: 1));
        db.Users.Add(TestFixtures.CreateAppUser(id: AdminId, userName: "admin", email: "admin@tknq.io", fighterId: 2));
        db.SaveChanges();
        return db;
    }

    private static AnalysisQuotaService CreateService(MyDatabaseContext db, AnalysisQuotaOptions? options = null) =>
        new(db,
            new AdminAccessService(db, Options.Create(new AiConsoleOptions { AdminEmails = "admin@tknq.io" })),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(options ?? new AnalysisQuotaOptions()),
            new Mock<ILogger<AnalysisQuotaService>>().Object);

    [Fact]
    public async Task Should_AllowThreeAnalyses_ThenRefuseTheFourth_ByDefault()
    {
        using var db = SeedUsers();
        var svc = CreateService(db);

        for (var i = 1; i <= 3; i++)
            Assert.True((await svc.TryReserveAsync(UserId, i, AnalysisKinds.V2)).Allowed);

        var fourth = await svc.TryReserveAsync(UserId, 4, AnalysisKinds.V2);

        Assert.False(fourth.Allowed);
        Assert.Equal(3, fourth.Status.Used);
        Assert.Equal(0, fourth.Status.Remaining);
        Assert.True(fourth.Status.IsExhausted);
        Assert.Equal(3, db.AnalysisUsages.Count());                 // the refused attempt leaves no row
        Assert.NotNull(fourth.Status.ResetsAt);
        Assert.InRange(fourth.Status.ResetsAt!.Value, DateTime.UtcNow.AddHours(23.9), DateTime.UtcNow.AddHours(24.1));
    }

    [Fact]
    public async Task Should_GiveTheSlotBack_When_ARunIsRefunded()
    {
        using var db = SeedUsers();
        var svc = CreateService(db);
        for (var i = 1; i <= 3; i++) await svc.TryReserveAsync(UserId, i, AnalysisKinds.V2);

        await svc.RefundAsync(2, AnalysisKinds.V2);
        var status = await svc.GetStatusAsync(UserId);

        Assert.Equal(2, status.Used);
        Assert.Equal(1, status.Remaining);
        Assert.True((await svc.TryReserveAsync(UserId, 5, AnalysisKinds.V2)).Allowed);
    }

    [Fact]
    public async Task Should_NotCountUsesOlderThanTheWindow()
    {
        using var db = SeedUsers();
        for (var i = 0; i < 3; i++)
            db.AnalysisUsages.Add(new AnalysisUsage { UserId = UserId, VideoId = 100 + i, Kind = AnalysisKinds.V2, StartedAt = DateTime.UtcNow.AddHours(-25) });
        db.SaveChanges();
        var svc = CreateService(db);

        var status = await svc.GetStatusAsync(UserId);

        Assert.Equal(0, status.Used);
        Assert.Null(status.ResetsAt);
        Assert.True((await svc.TryReserveAsync(UserId, 1, AnalysisKinds.V2)).Allowed);
    }

    [Fact]
    public async Task Should_ExemptAdmins_ButStillRecordTheirUse()
    {
        using var db = SeedUsers();
        var svc = CreateService(db, new AnalysisQuotaOptions { MaxPerWindow = 1 });

        for (var i = 1; i <= 3; i++)
            Assert.True((await svc.TryReserveAsync(AdminId, i, AnalysisKinds.V2)).Allowed);

        var status = await svc.GetStatusAsync(AdminId);
        Assert.True(status.Exempt);
        Assert.False(status.IsExhausted);
        Assert.Equal(3, db.AnalysisUsages.Count(u => u.UserId == AdminId));
    }

    [Fact]
    public async Task Should_NotLimit_When_Disabled()
    {
        using var db = SeedUsers();
        var svc = CreateService(db, new AnalysisQuotaOptions { Enabled = false, MaxPerWindow = 1 });

        Assert.True((await svc.TryReserveAsync(UserId, 1, AnalysisKinds.V2)).Allowed);
        Assert.True((await svc.TryReserveAsync(UserId, 2, AnalysisKinds.V2)).Allowed);
        Assert.False((await svc.GetStatusAsync(UserId)).IsExhausted);
    }

    [Fact]
    public async Task Should_ApplyAndResetConsoleOverride()
    {
        using var db = SeedUsers();
        var svc = CreateService(db);

        var saved = await svc.SaveOverrideAsync(new AnalysisQuotaSettings { Enabled = true, MaxPerWindow = 1, WindowHours = 12 }, "admin@tknq.io");
        Assert.True(saved.IsOverridden);
        Assert.Equal(3, saved.Defaults.MaxPerWindow);
        Assert.True((await svc.TryReserveAsync(UserId, 1, AnalysisKinds.V2)).Allowed);
        var second = await svc.TryReserveAsync(UserId, 2, AnalysisKinds.V2);
        Assert.False(second.Allowed);
        Assert.Equal(12, second.Status.WindowHours);

        var reset = await svc.ResetAsync();
        Assert.False(reset.IsOverridden);
        Assert.True((await svc.TryReserveAsync(UserId, 2, AnalysisKinds.V2)).Allowed);   // back to 3 per 24h
    }

    [Fact]
    public async Task Should_RefuseEveryone_When_LimitIsZero()
    {
        using var db = SeedUsers();
        var svc = CreateService(db, new AnalysisQuotaOptions { MaxPerWindow = 0 });

        Assert.False((await svc.TryReserveAsync(UserId, 1, AnalysisKinds.V2)).Allowed);
        Assert.True((await svc.TryReserveAsync(AdminId, 1, AnalysisKinds.V2)).Allowed);
    }

    [Theory]
    [InlineData(-1, 24, "maxPerWindow")]
    [InlineData(1001, 24, "maxPerWindow")]
    [InlineData(3, 0, "windowHours")]
    [InlineData(3, 721, "windowHours")]
    public void Should_RejectOutOfRangeSettings(int max, int hours, string field)
    {
        var errors = AnalysisQuotaService.Validate(new AnalysisQuotaSettings { Enabled = true, MaxPerWindow = max, WindowHours = hours });
        Assert.Contains(field, errors.Keys);
    }

    // ── In-progress guard ────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReportInProgress_While_PipelineIsRunning()
    {
        using var db = SeedUsers();
        db.AiAnalysisResults.Add(NewResult(videoId: 7, AnalysisPipelineStatus.Logging, DateTime.UtcNow.AddMinutes(-2)));
        db.SaveChanges();

        Assert.True(await CreateService(db).IsAnalysisInProgressAsync(7));
    }

    [Fact]
    public async Task Should_ReportInProgress_When_ChargedButNotYetStarted()
    {
        using var db = SeedUsers();
        var svc = CreateService(db);
        await svc.TryReserveAsync(UserId, 7, AnalysisKinds.V2);       // queued: no result row yet

        Assert.True(await svc.IsAnalysisInProgressAsync(7));
    }

    [Fact]
    public async Task Should_NotReportInProgress_For_StaleFinishedOrRefundedRuns()
    {
        using var db = SeedUsers();
        db.AiAnalysisResults.Add(NewResult(videoId: 1, AnalysisPipelineStatus.Profiling, DateTime.UtcNow.AddHours(-2)));   // crashed long ago
        db.AiAnalysisResults.Add(NewResult(videoId: 2, AnalysisPipelineStatus.Complete, DateTime.UtcNow));                // finished after its charge
        db.AnalysisUsages.Add(new AnalysisUsage { UserId = UserId, VideoId = 2, Kind = AnalysisKinds.V2, StartedAt = DateTime.UtcNow.AddMinutes(-10) });
        db.AnalysisUsages.Add(new AnalysisUsage { UserId = UserId, VideoId = 3, Kind = AnalysisKinds.V2, StartedAt = DateTime.UtcNow, RefundedAt = DateTime.UtcNow });
        db.SaveChanges();
        var svc = CreateService(db);

        Assert.False(await svc.IsAnalysisInProgressAsync(1));
        Assert.False(await svc.IsAnalysisInProgressAsync(2));
        Assert.False(await svc.IsAnalysisInProgressAsync(3));
    }

    private static AiAnalysisResult NewResult(int videoId, AnalysisPipelineStatus status, DateTime lastUpdated) => new()
    {
        VideoId = videoId,
        AnalysisJson = "{}",
        Strengths = "[]",
        AreasForImprovement = "[]",
        Techniques = [],
        Drills = [],
        GeneratedAt = DateTime.UtcNow,
        UpdatedBy = UserId,
        PipelineStatus = status,
        LastUpdatedAt = lastUpdated,
    };
}

using Microsoft.EntityFrameworkCore;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Models.Dtos;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

public class AgenticAnalysisServiceTests
{
    private static async Task<AiAnalysisResult> SeedCompleteAnalysisAsync(MyDatabaseContext db, int videoId = 1)
    {
        var result = new AiAnalysisResult
        {
            VideoId = videoId,
            AnalysisJson = "{}",
            Strengths = "[]",
            AreasForImprovement = "[]",
            PipelineStatus = AnalysisPipelineStatus.Complete,
            VisualDna = "Athlete in white gi.",
            TechnicalGrade = 80,
            GradeLabel = "Solid",
            EliteTip = "Tip",
            MatchSummary = "Summary",
            MatchEvents =
            [
                new MatchEvent { StartTimestampMs = 5000, EndTimestampMs = 6000, Actor = EventActor.Opponent, TechniqueCategory = "Sweep", Outcome = EventOutcome.Successful, Confidence = 0.4, SequenceIndex = 1 },
                new MatchEvent { StartTimestampMs = 1000, EndTimestampMs = 2000, Actor = EventActor.Student, TechniqueCategory = "Takedown", Outcome = EventOutcome.Successful, Confidence = 0.9, SequenceIndex = 0 },
            ],
            CoachingReport = new CoachingReport
            {
                MatchSummary = "Summary",
                TechnicalGrade = 80,
                GradeLabel = "Solid",
                EliteTip = "Tip",
                Strengths = [new CoachingStrength { Title = "Takedowns", Explanation = "Good", SortOrder = 0 }],
                Weaknesses = [new CoachingWeakness { Title = "Guard", Severity = WeaknessSeverity.Critical, ScoringImpact = "2 pts", SortOrder = 0 }],
                PrescribedDrills = [new PrescribedDrill { DrillName = "Drill A", Goal = "Goal", SortOrder = 0 }],
            },
        };
        db.AiAnalysisResults.Add(result);
        await db.SaveChangesAsync();
        return result;
    }

    [Fact]
    public async Task ReadService_Should_MapAndOrderEvents_BySequenceIndex()
    {
        using var db = InMemoryDbContextFactory.Create();
        await SeedCompleteAnalysisAsync(db);
        var read = new AgenticAnalysisReadService(db);

        var dto = await read.GetAnalysisV2DtoByVideoId(1);

        Assert.Equal("Athlete in white gi.", dto.VisualDna);
        Assert.Equal("Complete", dto.PipelineStatus);
        Assert.Equal(2, dto.MatchEvents.Count);
        Assert.Equal("Student", dto.MatchEvents[0].Actor);        // SequenceIndex 0 first
        Assert.Equal("Takedown", dto.MatchEvents[0].TechniqueCategory);
        Assert.NotNull(dto.CoachingReport);
        Assert.Equal("Critical", dto.CoachingReport!.CriticalWeaknesses[0].Severity);
        Assert.Single(dto.CoachingReport!.PrescribedDrills);
    }

    [Fact]
    public async Task ReadService_Should_Throw_When_NoV2Analysis()
    {
        using var db = InMemoryDbContextFactory.Create();
        var read = new AgenticAnalysisReadService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => read.GetAnalysisV2DtoByVideoId(999));
    }

    [Fact]
    public async Task WriteService_Should_UpsertEvents_AndReplaceCoachingReport()
    {
        using var db = InMemoryDbContextFactory.Create();
        await SeedCompleteAnalysisAsync(db);
        var write = new AgenticAnalysisWriteService(db);

        var dto = new AnalysisV2Dto
        {
            VideoId = 1,
            VisualDna = "Edited DNA",
            MatchEvents =
            [
                new MatchEventDto { StartTimestampMs = 100, EndTimestampMs = 200, Actor = "Student", TechniqueCategory = "Pass", Outcome = "Partial", Confidence = 0.7 },
            ],
            CoachingReport = new CoachingReportDto
            {
                MatchSummary = "Edited summary",
                TechnicalGrade = 95,
                GradeLabel = "Excellent",
                EliteTip = "New tip",
                KeyStrengths = [new CoachingStrengthDto { Title = "S1" }, new CoachingStrengthDto { Title = "S2" }],
                CriticalWeaknesses = [new CoachingWeaknessDto { Title = "W1", Severity = "Minor" }],
                PrescribedDrills = [new PrescribedDrillDto { DrillName = "D1" }],
            },
        };

        var updated = await write.SaveAnalysisV2(1, dto, userId: "editor-1");

        Assert.Equal("Edited DNA", updated.VisualDna);
        Assert.Single(updated.MatchEvents);                       // 2 → 1 (delete handled)
        Assert.Equal("Pass", updated.MatchEvents[0].TechniqueCategory);
        Assert.Equal(95, updated.CoachingReport!.TechnicalGrade);
        Assert.Equal(2, updated.CoachingReport!.KeyStrengths.Count);
        Assert.Equal("Minor", updated.CoachingReport!.CriticalWeaknesses[0].Severity);

        // Mirrored headline fields updated on the analysis row.
        var row = await db.AiAnalysisResults.AsNoTracking().FirstAsync(a => a.VideoId == 1);
        Assert.Equal(95, row.TechnicalGrade);
        Assert.Equal("Excellent", row.GradeLabel);
    }

    [Fact]
    public async Task WriteService_Should_Throw_When_NoAnalysisRow()
    {
        using var db = InMemoryDbContextFactory.Create();
        var write = new AgenticAnalysisWriteService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => write.SaveAnalysisV2(999, new AnalysisV2Dto { VideoId = 999 }, null));
    }
}

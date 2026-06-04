using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Models.Dtos;

namespace VideoAnalysis.Server.Domain.AIServices
{
    /// <summary>Reads agentic (v2) analysis results and maps them to the frontend DTO.</summary>
    public class AgenticAnalysisReadService
    {
        private readonly MyDatabaseContext _context;

        public AgenticAnalysisReadService(MyDatabaseContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns the v2 analysis for a video. Throws <see cref="InvalidOperationException"/> when no
        /// v2 analysis exists (the controller maps this to 404 so the frontend falls back to legacy).
        /// </summary>
        public async Task<AnalysisV2Dto> GetAnalysisV2DtoByVideoId(int videoId)
        {
            var result = await _context.AiAnalysisResults
                .AsSplitQuery()
                .Include(a => a.MatchEvents)
                .Include(a => a.CoachingReport!).ThenInclude(c => c.Strengths)
                .Include(a => a.CoachingReport!).ThenInclude(c => c.Weaknesses)
                .Include(a => a.CoachingReport!).ThenInclude(c => c.PrescribedDrills)
                .FirstOrDefaultAsync(a => a.VideoId == videoId);

            // Treat "never ran the v2 pipeline" as not-found so the page can fall back to legacy.
            if (result == null || (result.PipelineStatus == AnalysisPipelineStatus.NotStarted && result.CoachingReport == null && result.MatchEvents.Count == 0))
            {
                throw new InvalidOperationException($"No v2 analysis found for video {videoId}.");
            }

            return MapToDto(result);
        }

        public static AnalysisV2Dto MapToDto(AiAnalysisResult result) => new()
        {
            Id = result.Id,
            VideoId = result.VideoId,
            VisualDna = result.VisualDna,
            PipelineStatus = result.PipelineStatus.ToString(),
            TechnicalGrade = result.TechnicalGrade,
            GradeLabel = result.GradeLabel,
            EliteTip = result.EliteTip,
            MatchSummary = result.MatchSummary,
            MatchEvents = result.MatchEvents
                .OrderBy(e => e.SequenceIndex)
                .Select(e => new MatchEventDto
                {
                    Id = e.Id,
                    StartTimestampMs = e.StartTimestampMs,
                    EndTimestampMs = e.EndTimestampMs,
                    Actor = e.Actor.ToString(),
                    TechniqueCategory = e.TechniqueCategory,
                    TechniqueName = e.TechniqueName,
                    PositionBefore = e.PositionBefore,
                    PositionAfter = e.PositionAfter,
                    Outcome = e.Outcome.ToString(),
                    GuardType = e.GuardType,
                    SubmissionType = e.SubmissionType,
                    ActionsDescription = e.ActionsDescription,
                    Confidence = e.Confidence,
                })
                .ToList(),
            CoachingReport = result.CoachingReport == null ? null : new CoachingReportDto
            {
                Id = result.CoachingReport.Id,
                MatchSummary = result.CoachingReport.MatchSummary,
                TechnicalGrade = result.CoachingReport.TechnicalGrade,
                GradeLabel = result.CoachingReport.GradeLabel,
                EliteTip = result.CoachingReport.EliteTip,
                KeyStrengths = result.CoachingReport.Strengths
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new CoachingStrengthDto
                    {
                        Id = s.Id, Title = s.Title, Explanation = s.Explanation,
                        TimestampStartMs = s.TimestampStartMs, TimestampEndMs = s.TimestampEndMs,
                    }).ToList(),
                CriticalWeaknesses = result.CoachingReport.Weaknesses
                    .OrderBy(w => w.SortOrder)
                    .Select(w => new CoachingWeaknessDto
                    {
                        Id = w.Id, Title = w.Title, Explanation = w.Explanation,
                        TimestampStartMs = w.TimestampStartMs, TimestampEndMs = w.TimestampEndMs,
                        Severity = w.Severity.ToString(), ScoringImpact = w.ScoringImpact,
                    }).ToList(),
                PrescribedDrills = result.CoachingReport.PrescribedDrills
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new PrescribedDrillDto
                    {
                        Id = d.Id, DrillName = d.DrillName, Instructions = d.Instructions, Goal = d.Goal,
                    }).ToList(),
            },
        };
    }
}

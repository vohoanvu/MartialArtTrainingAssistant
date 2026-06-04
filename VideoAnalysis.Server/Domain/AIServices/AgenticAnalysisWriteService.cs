using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Models.Dtos;

namespace VideoAnalysis.Server.Domain.AIServices
{
    /// <summary>
    /// Persists user edits to a v2 analysis (full editable parity). Match events and coaching-report
    /// children are reconciled wholesale from the incoming DTO — whatever the client sends becomes the
    /// new truth, which cleanly handles add/edit/delete in one call.
    /// </summary>
    public class AgenticAnalysisWriteService
    {
        private readonly MyDatabaseContext _context;

        public AgenticAnalysisWriteService(MyDatabaseContext context)
        {
            _context = context;
        }

        public async Task<AnalysisV2Dto> SaveAnalysisV2(int videoId, AnalysisV2Dto dto, string? userId)
        {
            var result = await _context.AiAnalysisResults
                .Include(a => a.MatchEvents)
                .Include(a => a.CoachingReport!).ThenInclude(c => c.Strengths)
                .Include(a => a.CoachingReport!).ThenInclude(c => c.Weaknesses)
                .Include(a => a.CoachingReport!).ThenInclude(c => c.PrescribedDrills)
                .FirstOrDefaultAsync(a => a.VideoId == videoId)
                ?? throw new InvalidOperationException($"No analysis found for video {videoId}.");

            // ── Headline scalar fields ──
            if (dto.VisualDna != null) result.VisualDna = dto.VisualDna;
            result.LastUpdatedAt = DateTime.UtcNow;
            result.UpdatedBy = userId ?? result.UpdatedBy;

            // ── Match events: wholesale replace, ordered ──
            if (dto.MatchEvents != null)
            {
                _context.MatchEvents.RemoveRange(result.MatchEvents);
                int i = 0;
                foreach (var e in dto.MatchEvents)
                {
                    result.MatchEvents.Add(new MatchEvent
                    {
                        AiAnalysisResultId = result.Id,
                        StartTimestampMs = e.StartTimestampMs,
                        EndTimestampMs = e.EndTimestampMs,
                        Actor = ParseEnum(e.Actor, EventActor.Student),
                        TechniqueCategory = string.IsNullOrWhiteSpace(e.TechniqueCategory) ? "Transition" : e.TechniqueCategory,
                        TechniqueName = e.TechniqueName,
                        PositionBefore = e.PositionBefore,
                        PositionAfter = e.PositionAfter,
                        Outcome = ParseEnum(e.Outcome, EventOutcome.InProgress),
                        GuardType = e.GuardType,
                        SubmissionType = e.SubmissionType,
                        ActionsDescription = e.ActionsDescription,
                        Confidence = e.Confidence,
                        SequenceIndex = i++,
                    });
                }
            }

            // ── Coaching report: replace children, update scalars ──
            if (dto.CoachingReport != null)
            {
                if (result.CoachingReport != null) _context.CoachingReports.Remove(result.CoachingReport);

                var cr = dto.CoachingReport;
                var report = new CoachingReport
                {
                    AiAnalysisResultId = result.Id,
                    MatchSummary = cr.MatchSummary ?? string.Empty,
                    TechnicalGrade = Math.Clamp(cr.TechnicalGrade, 0, 100),
                    GradeLabel = cr.GradeLabel,
                    EliteTip = cr.EliteTip,
                    Strengths = (cr.KeyStrengths ?? []).Select((s, idx) => new CoachingStrength
                    {
                        Title = s.Title, Explanation = s.Explanation,
                        TimestampStartMs = s.TimestampStartMs, TimestampEndMs = s.TimestampEndMs, SortOrder = idx,
                    }).ToList(),
                    Weaknesses = (cr.CriticalWeaknesses ?? []).Select((w, idx) => new CoachingWeakness
                    {
                        Title = w.Title, Explanation = w.Explanation,
                        TimestampStartMs = w.TimestampStartMs, TimestampEndMs = w.TimestampEndMs,
                        Severity = ParseEnum(w.Severity, WeaknessSeverity.Major), ScoringImpact = w.ScoringImpact, SortOrder = idx,
                    }).ToList(),
                    PrescribedDrills = (cr.PrescribedDrills ?? []).Select((d, idx) => new PrescribedDrill
                    {
                        DrillName = d.DrillName, Instructions = d.Instructions, Goal = d.Goal, SortOrder = idx,
                    }).ToList(),
                };
                result.CoachingReport = report;

                // keep mirrored headline fields in sync
                result.MatchSummary = report.MatchSummary;
                result.TechnicalGrade = report.TechnicalGrade;
                result.GradeLabel = report.GradeLabel;
                result.EliteTip = report.EliteTip;
            }

            await _context.SaveChangesAsync();

            // Re-read for a clean, ordered DTO with fresh Ids.
            return await new AgenticAnalysisReadService(_context).GetAnalysisV2DtoByVideoId(videoId);
        }

        private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct =>
            Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
    }
}

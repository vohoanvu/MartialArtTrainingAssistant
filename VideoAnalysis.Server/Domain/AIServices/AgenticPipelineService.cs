using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.StaticFiles;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Domain.YoutubeSharingService;
using VideoAnalysis.Server.Models.Dtos;

namespace VideoAnalysis.Server.Domain.AIServices
{
    public interface IAgenticPipelineService
    {
        /// <summary>Runs the full 4-phase agentic analysis pipeline for an uploaded video.</summary>
        Task RunPipelineAsync(int videoId, CancellationToken ct);
    }

    /// <summary>
    /// Orchestrates the 4-phase agentic VLM pipeline (Auto-Profiler → Event Logger → Event Verifier
    /// → Head Coach), sharing a single Vertex context cache of the video across phases when possible.
    /// Emits per-phase status over SignalR and persists results to the v2 entity tables.
    /// </summary>
    public class AgenticPipelineService : IAgenticPipelineService
    {
        private readonly IVertexRestClient _vertex;
        private readonly MyDatabaseContext _context;
        private readonly VertexAiPipelineOptions _options;
        private readonly IHubContext<VideoAnalysisHub> _hub;
        private readonly ILogger<AgenticPipelineService> _logger;

        private static readonly JsonSerializerOptions ParseOptions = new() { PropertyNameCaseInsensitive = true };

        public AgenticPipelineService(
            IVertexRestClient vertex,
            MyDatabaseContext context,
            IOptions<VertexAiPipelineOptions> options,
            IHubContext<VideoAnalysisHub> hub,
            ILogger<AgenticPipelineService> logger)
        {
            _vertex = vertex;
            _context = context;
            _options = options.Value;
            _hub = hub;
            _logger = logger;
        }

        public async Task RunPipelineAsync(int videoId, CancellationToken ct)
        {
            var video = await _context.Videos.FirstOrDefaultAsync(v => v.Id == videoId, ct)
                ?? throw new InvalidOperationException($"Video {videoId} not found.");
            var appUser = await _context.Users.Include(u => u.Fighter).FirstAsync(u => u.Id == video.UserId, ct);

            var fileUri = video.FilePath ?? throw new InvalidOperationException($"Video {videoId} has no FilePath.");
            var mimeType = DetermineMimeType(fileUri);
            var studentIdentifier = video.StudentIdentifier ?? "the most active athlete in the video";

            var result = await GetOrCreateResultAsync(videoId, video.UserId, ct);

            string? cacheName = null;
            try
            {
                // ── Phase 1: Auto-Profiler (+ context cache creation) ─────────
                await UpdateStatusAsync(result, AnalysisPipelineStatus.Profiling, videoId, ct);
                cacheName = await TryCreateCacheAsync(fileUri, mimeType, videoId, ct);
                result.ContextCacheName = cacheName;
                await _context.SaveChangesAsync(ct);

                var visualDna = await RunProfilerAsync(cacheName, fileUri, mimeType, studentIdentifier, ct);
                result.VisualDna = visualDna;
                await _context.SaveChangesAsync(ct);

                // ── Phase 2: Event Logger ─────────────────────────────────────
                await UpdateStatusAsync(result, AnalysisPipelineStatus.Logging, videoId, ct);
                var rawEvents = await RunEventLoggerAsync(cacheName, fileUri, mimeType, visualDna, ct);

                // ── Phase 3: Event Verifier ───────────────────────────────────
                await UpdateStatusAsync(result, AnalysisPipelineStatus.Verifying, videoId, ct);
                var verifiedEvents = await RunVerifierAsync(cacheName, fileUri, mimeType, rawEvents, visualDna, ct);

                PersistMatchEvents(result, verifiedEvents);
                result.AnalysisJson = JsonSerializer.Serialize(new VertexEventsResponse { Events = verifiedEvents });
                await _context.SaveChangesAsync(ct);

                // ── Phase 4: Head Coach ───────────────────────────────────────
                await UpdateStatusAsync(result, AnalysisPipelineStatus.Coaching, videoId, ct);
                var report = await RunHeadCoachAsync(cacheName, fileUri, mimeType, verifiedEvents, visualDna, appUser.Fighter, ct);

                PersistCoachingReport(result, report);
                await UpdateStatusAsync(result, AnalysisPipelineStatus.Complete, videoId, ct);
                await _hub.Clients.All.SendAsync("AnalysisV2Completed", videoId, ct);
                _logger.LogInformation("Agentic pipeline complete for video {VideoId}.", videoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agentic pipeline failed for video {VideoId}.", videoId);
                result.PipelineStatus = AnalysisPipelineStatus.Failed;
                result.PipelineError = ex.Message;
                await _context.SaveChangesAsync(CancellationToken.None);
                await _hub.Clients.All.SendAsync("AnalysisV2Failed", videoId, ex.Message, CancellationToken.None);
                throw;
            }
            finally
            {
                if (cacheName != null)
                {
                    try { await _vertex.DeleteCachedContentAsync(cacheName, _options.Location, CancellationToken.None); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Context cache cleanup failed for {CacheName}", cacheName); }
                    result.ContextCacheName = null;
                    await _context.SaveChangesAsync(CancellationToken.None);
                }
            }
        }

        // ── Persistence helpers ──────────────────────────────────────────────

        private async Task<AiAnalysisResult> GetOrCreateResultAsync(int videoId, string userId, CancellationToken ct)
        {
            var existing = await _context.AiAnalysisResults.FirstOrDefaultAsync(a => a.VideoId == videoId, ct);
            if (existing != null) return existing;

            var created = new AiAnalysisResult
            {
                VideoId = videoId,
                AnalysisJson = "{}",
                Strengths = "[]",
                AreasForImprovement = "[]",
                Techniques = [],
                Drills = [],
                GeneratedAt = DateTime.UtcNow,
                UpdatedBy = userId,
                PipelineStatus = AnalysisPipelineStatus.NotStarted,
            };
            _context.AiAnalysisResults.Add(created);
            await _context.SaveChangesAsync(ct);
            return created;
        }

        private async Task UpdateStatusAsync(AiAnalysisResult result, AnalysisPipelineStatus status, int videoId, CancellationToken ct)
        {
            result.PipelineStatus = status;
            result.LastUpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            await _hub.Clients.All.SendAsync("AnalysisStatusChanged", videoId, status.ToString(), ct);
        }

        private void PersistMatchEvents(AiAnalysisResult result, List<VertexEvent> events)
        {
            var existing = _context.MatchEvents.Where(e => e.AiAnalysisResultId == result.Id);
            _context.MatchEvents.RemoveRange(existing);

            int i = 0;
            foreach (var e in events)
            {
                _context.MatchEvents.Add(new MatchEvent
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

        private void PersistCoachingReport(AiAnalysisResult result, VertexCoachingReport report)
        {
            var existing = _context.CoachingReports
                .Include(r => r.Strengths)
                .Include(r => r.Weaknesses)
                .Include(r => r.PrescribedDrills)
                .FirstOrDefault(r => r.AiAnalysisResultId == result.Id);
            if (existing != null) _context.CoachingReports.Remove(existing);

            // Enforce the PRD's hard limits in code (responseSchema min/maxItems is best-effort).
            var strengths = report.KeyStrengths.Take(3).ToList();
            var weaknesses = report.CriticalWeaknesses.Take(3).ToList();
            var drills = report.PrescribedDrills.Take(3).ToList();

            var entity = new CoachingReport
            {
                AiAnalysisResultId = result.Id,
                MatchSummary = report.MatchSummary ?? string.Empty,
                TechnicalGrade = Math.Clamp((int)Math.Round(report.TechnicalGrade), 0, 100),
                GradeLabel = report.GradeLabel,
                EliteTip = report.EliteTip,
                Strengths = strengths.Select((s, idx) => new CoachingStrength
                {
                    Title = s.Title, Explanation = s.Explanation,
                    TimestampStartMs = s.TimestampStartMs, TimestampEndMs = s.TimestampEndMs, SortOrder = idx,
                }).ToList(),
                Weaknesses = weaknesses.Select((w, idx) => new CoachingWeakness
                {
                    Title = w.Title, Explanation = w.Explanation,
                    TimestampStartMs = w.TimestampStartMs, TimestampEndMs = w.TimestampEndMs,
                    Severity = ParseEnum(w.Severity, WeaknessSeverity.Major), ScoringImpact = w.ScoringImpact, SortOrder = idx,
                }).ToList(),
                PrescribedDrills = drills.Select((d, idx) => new PrescribedDrill
                {
                    DrillName = d.DrillName, Instructions = d.Instructions, Goal = d.Goal, SortOrder = idx,
                }).ToList(),
            };
            _context.CoachingReports.Add(entity);

            // Mirror headline fields onto the analysis row for cheap list reads.
            result.MatchSummary = entity.MatchSummary;
            result.TechnicalGrade = entity.TechnicalGrade;
            result.GradeLabel = entity.GradeLabel;
            result.EliteTip = entity.EliteTip;
        }

        // ── Phase implementations ────────────────────────────────────────────

        private async Task<string?> TryCreateCacheAsync(string fileUri, string mimeType, int videoId, CancellationToken ct)
        {
            // Caching is only reusable across phases when they share one model.
            var cacheModel = _options.EventLogger.ModelId;
            bool eligible = _options.Profiler.ModelId == cacheModel
                && _options.Verifier.ModelId == cacheModel
                && _options.HeadCoach.ModelId == cacheModel;
            if (!eligible)
            {
                _logger.LogInformation("Context caching skipped for video {VideoId}: phases use differing models.", videoId);
                return null;
            }

            try
            {
                // Matches the proven MyCoach pipeline: cache holds only the video (no videoMetadata/fps —
                // fps is a prompt instruction), expiry via expireTime, on the global endpoint.
                var expireTime = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.ContextCacheTtlMinutes)).ToString("O");
                var body = new
                {
                    model = _vertex.ModelResourcePath(cacheModel, _options.Location),
                    displayName = $"analysis-{videoId}",
                    expireTime,
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new { fileData = new { mimeType, fileUri } },
                            },
                        },
                    },
                };
                return await _vertex.CreateCachedContentAsync(body, _options.Location, ct);
            }
            catch (Exception ex)
            {
                // Short videos may fall below the cache token minimum; fall back to per-phase fileData.
                _logger.LogWarning(ex, "Context cache creation failed for video {VideoId}; falling back to per-phase fileData.", videoId);
                return null;
            }
        }

        private async Task<string> RunProfilerAsync(string? cacheName, string fileUri, string mimeType, string studentIdentifier, CancellationToken ct)
        {
            var prompt = PipelinePrompts.Profiler(studentIdentifier);
            var body = BuildBody(cacheName, fileUri, mimeType, prompt, BuildTextGenerationConfig(_options.Profiler));
            var response = await _vertex.GenerateContentAsync(_options.Profiler.ModelId, _options.Location, body, ct);
            return _vertex.ExtractText(response).Trim();
        }

        private async Task<List<VertexEvent>> RunEventLoggerAsync(string? cacheName, string fileUri, string mimeType, string visualDna, CancellationToken ct)
        {
            var fps = _options.EventLogger.Fps > 0 ? _options.EventLogger.Fps : 3;
            var genConfig = BuildJsonGenerationConfig(_options.EventLogger, PipelineSchemas.EventsSchema());

            var events = await EventLoggerCallAsync(cacheName, fileUri, mimeType, PipelinePrompts.EventLogger(visualDna, fps), genConfig, ct);
            if (events.Count == 0)
            {
                // Proven robustness step: a simplified prompt when the structured pass yields nothing.
                _logger.LogWarning("Event Logger returned 0 events — retrying with simplified prompt.");
                events = await EventLoggerCallAsync(cacheName, fileUri, mimeType, PipelinePrompts.EventLoggerFallback(), genConfig, ct);
            }
            return events;
        }

        private async Task<List<VertexEvent>> EventLoggerCallAsync(string? cacheName, string fileUri, string mimeType, string prompt, object genConfig, CancellationToken ct)
        {
            var body = BuildBody(cacheName, fileUri, mimeType, prompt, genConfig);
            var response = await _vertex.GenerateContentAsync(_options.EventLogger.ModelId, _options.Location, body, ct);
            return ParseEvents(_vertex.ExtractText(response));
        }

        private async Task<List<VertexEvent>> RunVerifierAsync(string? cacheName, string fileUri, string mimeType, List<VertexEvent> rawEvents, string visualDna, CancellationToken ct)
        {
            if (rawEvents.Count == 0) return rawEvents;
            var eventsJson = JsonSerializer.Serialize(new VertexEventsResponse { Events = rawEvents });
            var prompt = PipelinePrompts.Verifier(visualDna, eventsJson);
            var genConfig = BuildJsonGenerationConfig(_options.Verifier, PipelineSchemas.EventsSchema());
            var body = BuildBody(cacheName, fileUri, mimeType, prompt, genConfig);
            var response = await _vertex.GenerateContentAsync(_options.Verifier.ModelId, _options.Location, body, ct);
            var verified = ParseEvents(_vertex.ExtractText(response));
            // Defensive: if the verifier returned nothing usable, keep the raw events rather than losing data.
            return verified.Count > 0 ? verified : rawEvents;
        }

        private async Task<VertexCoachingReport> RunHeadCoachAsync(string? cacheName, string fileUri, string mimeType, List<VertexEvent> events, string visualDna, Fighter? fighter, CancellationToken ct)
        {
            var eventsJson = JsonSerializer.Serialize(new VertexEventsResponse { Events = events });
            var prompt = PipelinePrompts.HeadCoach(visualDna, eventsJson, fighter);
            var genConfig = BuildJsonGenerationConfig(_options.HeadCoach, PipelineSchemas.CoachingReportSchema());
            var body = BuildBody(cacheName, fileUri, mimeType, prompt, genConfig);
            var response = await _vertex.GenerateContentAsync(_options.HeadCoach.ModelId, _options.Location, body, ct);
            var text = CleanJsonFromMarkdown(_vertex.ExtractText(response));
            return JsonSerializer.Deserialize<VertexCoachingReport>(text, ParseOptions)
                ?? throw new InvalidOperationException("Head Coach returned unparseable coaching report JSON.");
        }

        // ── Request body builder ─────────────────────────────────────────────

        private static object BuildBody(string? cacheName, string fileUri, string mimeType, string prompt, object generationConfig)
        {
            if (cacheName != null)
            {
                // Video lives in the cache; only the per-phase prompt is sent (proven MyCoach pattern).
                return new
                {
                    cachedContent = cacheName,
                    contents = new[]
                    {
                        new { role = "user", parts = new object[] { new { text = prompt } } },
                    },
                    generationConfig,
                    safetySettings = VertexRestClient.AllSafetyOff(),
                };
            }

            // Fallback when no cache could be created: reference the gs:// video directly (no videoMetadata).
            return new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { fileData = new { mimeType, fileUri } },
                            new { text = prompt },
                        },
                    },
                },
                generationConfig,
                safetySettings = VertexRestClient.AllSafetyOff(),
            };
        }

        private static object BuildJsonGenerationConfig(PhaseModelOptions phase, JsonNode responseSchema) => new
        {
            temperature = phase.Temperature,
            topP = 1.0f,
            maxOutputTokens = phase.MaxOutputTokens,
            responseMimeType = "application/json",
            responseSchema,
            thinkingConfig = new { thinkingBudget = phase.ThinkingBudget },
        };

        private static object BuildTextGenerationConfig(PhaseModelOptions phase) => new
        {
            temperature = phase.Temperature,
            topP = 1.0f,
            maxOutputTokens = phase.MaxOutputTokens,
            thinkingConfig = new { thinkingBudget = phase.ThinkingBudget },
        };

        // ── Parsing utilities ────────────────────────────────────────────────

        private List<VertexEvent> ParseEvents(string text)
        {
            var clean = CleanJsonFromMarkdown(text);
            var parsed = JsonSerializer.Deserialize<VertexEventsResponse>(clean, ParseOptions);
            return parsed?.Events ?? [];
        }

        private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct =>
            Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

        private string DetermineMimeType(string pathOrUri)
        {
            string extension;
            if (pathOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUri.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                extension = Path.GetExtension(new Uri(pathOrUri).AbsolutePath);
            }
            else
            {
                extension = Path.GetExtension(pathOrUri);
            }

            var provider = new FileExtensionContentTypeProvider();
            if (string.IsNullOrEmpty(extension) || !provider.TryGetContentType(extension, out var contentType))
            {
                _logger.LogWarning("Could not determine MIME type for '{PathOrUri}'. Defaulting to 'video/mp4'.", pathOrUri);
                contentType = "video/mp4";
            }
            return contentType;
        }

        private static string CleanJsonFromMarkdown(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return string.Empty;
            var trimmed = rawJson.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = trimmed.IndexOf('\n');
                if (firstNewline >= 0) trimmed = trimmed[(firstNewline + 1)..];
                if (trimmed.EndsWith("```", StringComparison.Ordinal)) trimmed = trimmed[..^3];
            }
            return trimmed.Trim();
        }
    }
}

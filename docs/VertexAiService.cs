using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCoach.Application.Exceptions;
using MyCoach.Application.Interfaces;
using MyCoach.Application.Pipeline.Models;

namespace MyCoach.Infrastructure.AI;

public class VertexAiService : IVertexAiService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleCredential _credential;
    private readonly string _projectId;
    private readonly IConfiguration _config;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VertexAiService> _logger;

    public VertexAiService(HttpClient httpClient, IConfiguration configuration, TimeProvider timeProvider, ILogger<VertexAiService> logger)
    {
        _httpClient = httpClient;
        _projectId = configuration["VertexAi:ProjectId"]
            ?? throw new InvalidOperationException("VertexAi:ProjectId is not configured.");
        _credential = GoogleCredential.GetApplicationDefault();
        _config = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var tokenAccess = _credential as ITokenAccess;
        return await tokenAccess!.GetAccessTokenForRequestAsync();
    }

    private string PipelineConfig(string key, string defaultValue) =>
        _config[$"VertexAi:Pipeline:{key}"] ?? defaultValue;

    private float PipelineConfigFloat(string key, float defaultValue) =>
        float.TryParse(_config[$"VertexAi:Pipeline:{key}"], out var val) ? val : defaultValue;

    private int PipelineConfigInt(string key, int defaultValue) =>
        int.TryParse(_config[$"VertexAi:Pipeline:{key}"], out var val) ? val : defaultValue;

    private int CacheTtlMinutes =>
        int.TryParse(_config["VertexAi:Pipeline:ContextCacheTtlMinutes"], out var ttl) ? ttl : 30;

    private string ModelPath(string modelId) =>
        $"projects/{_projectId}/locations/global/publishers/google/models/{modelId}";

    private string GenerateContentUrl(string modelId) =>
        $"/v1/{ModelPath(modelId)}:generateContent";

    private string CreateCacheUrl() =>
        $"/v1/projects/{_projectId}/locations/global/cachedContents";

    private string DeleteCacheUrl(string cacheName) =>
        $"/v1/{cacheName}";

    // ── Phase 1: Auto-Profiler + Context Cache Creation ─────────────────

    public async Task<(string CacheName, string VisualDna)> CreateCacheAndGenerateVisualDnaAsync(
        string videoGcsUri,
        string studentIdentifier,
        CancellationToken ct)
    {
        _logger.LogInformation("Phase 1: Creating context cache for {VideoUri}", videoGcsUri);

        var modelId = PipelineConfig("Phase1_AutoProfiler:ModelId", "gemini-3.1-pro-preview");
        var expireTime = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(CacheTtlMinutes);

        var cacheBody = new
        {
            model = ModelPath(modelId),
            displayName = $"mycoach-pipeline-{Guid.NewGuid():N}",
            expireTime = expireTime.ToString("O"),
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { fileData = new { mimeType = "video/mp4", fileUri = videoGcsUri } }
                    }
                }
            }
        };

        var cacheResponse = await PostJsonAsync(CreateCacheUrl(), cacheBody, ct);
        var cacheName = cacheResponse.GetProperty("name").GetString()
            ?? throw new InvalidOperationException("Cache creation response missing 'name' field.");

        _logger.LogInformation("Phase 1: Cache created: {CacheName}, TTL: {Ttl}m", cacheName, CacheTtlMinutes);

        var phase1Prompt = $"""
            You are a Forensic Video Analyst specializing in combat sports footage.
            Watch the opening segment of this video. Locate the athlete matching this description:
            '{studentIdentifier}'.

            Generate a hyper-detailed, immutable 'Visual DNA' profile of this specific athlete.
            Include ALL of the following when visible:
            - Gi/rashguard: color, brand, sleeve length, any distinguishing wear/damage
            - Belt: color, stripe count, knot style
            - Physical: hair color/style, skin tone, body type, approximate height relative to opponent
            - Distinguishing marks: visible team patches and their locations, sponsor logos,
              ankle supports, knee braces, athletic tape on fingers or joints, ear guards
            - Movement signature: dominant stance (orthodox/southpaw), posture tendency

            Return ONLY a single descriptive paragraph. No bullet points. No headers. No analysis.
            """;

        var request = new
        {
            cachedContent = cacheName,
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = phase1Prompt } }
                }
            },
            generationConfig = new
            {
                temperature = PipelineConfigFloat("Phase1_AutoProfiler:Temperature", 0.1f),
                maxOutputTokens = 1024,
                thinkingConfig = new { thinkingBudget = PipelineConfigInt("Phase1_AutoProfiler:ThinkingBudgetTokens", 8192) }
            }
        };

        var response = await PostJsonAsync(GenerateContentUrl(modelId), request, ct);
        var visualDna = ExtractText(response);

        _logger.LogInformation("Phase 1 complete. Visual DNA ({Length} chars): {Preview}",
            visualDna.Length, visualDna[..Math.Min(200, visualDna.Length)]);
        return (cacheName, visualDna);
    }

    // ── Phase 2: Event Logger (full video) ──────────────────────────────

    public async Task<List<RawMatchEvent>> LogEventsAsync(
        string cacheName,
        string visualDna,
        CancellationToken ct)
    {
        _logger.LogInformation("Phase 2: Logging events from full video");

        var modelId = PipelineConfig("Phase2_EventLogger:ModelId", "gemini-3.1-pro-preview");
        var thinkingBudget = PipelineConfigInt("Phase2_EventLogger:ThinkingBudgetTokens", 16384);

        var events = await RunEventLoggerAsync(cacheName, visualDna, modelId, thinkingBudget, BuildPrimaryEventLoggerPrompt(visualDna), ct);

        if (events.Count == 0)
        {
            _logger.LogWarning("Phase 2: Primary prompt returned 0 events — retrying with simplified prompt");
            events = await RunEventLoggerAsync(cacheName, visualDna, modelId, thinkingBudget, BuildFallbackEventLoggerPrompt(), ct);
            _logger.LogInformation("Phase 2 fallback: {Count} events logged", events.Count);
        }

        _logger.LogInformation("Phase 2 complete: {Count} events logged", events.Count);
        return events;
    }

    private string BuildPrimaryEventLoggerPrompt(string visualDna)
    {
        var fpsRate = PipelineConfigInt("Phase2_EventLogger:FpsRate", 3);

        return $"""
            You are a meticulous grappling play-by-play logger and an expert BJJ black belt instructor.
            Watch this complete BJJ match video from start to finish and log every significant action.

            STUDENT IDENTIFICATION:
            The student athlete matches this visual profile:
            {visualDna}
            Every 30 seconds of video, re-confirm which athlete is the student. If uncertain,
            set confidence to 0.3 or lower for those events.

            WHAT TO LOG:
            - Every positional change (e.g., standing to guard, guard to mount)
            - Every technique attempt — successful or failed (takedowns, sweeps, passes, submissions, escapes)
            - Every transition and scramble
            - Actions by BOTH the student and their opponent
            You MUST log at least one event for every 30 seconds of video. A typical 2-minute match
            should produce 8-20 events minimum.

            HOW TO LOG:
            - Use absolute video timestamps in milliseconds from the start of the video
            - Attribute each action to "Student" or "Opponent" based on the visual profile above
            - Assess your confidence (0.0 to 1.0) as the minimum of: actor attribution accuracy,
              technique classification accuracy, and position identification accuracy
            - Analyze at a minimum of {fpsRate} frames per second — do not skip frames during scrambles

            IMPORTANT: You must return a JSON object with an "events" array. The array must NOT be empty.
            Even if the video is unclear, log what you can observe with lower confidence scores.
            """;
    }

    private static string BuildFallbackEventLoggerPrompt() => """
        Watch this BJJ match video and describe every action you see, in order.

        For each action, provide:
        - When it happens (start and end timestamps in milliseconds)
        - Who does it: "Student" (the person who was identified in the previous analysis) or "Opponent"
        - What type of action it is (e.g., Takedown, Pass, Sweep, Submission, Escape, Transition, Control)
        - What specific technique is used (e.g., Double Leg, Armbar, Knee Slice)
        - What position they were in before and after the action
        - Whether it was successful, failed, or partial
        - A brief description of what happened
        - How confident you are (0.0 to 1.0)

        You MUST return at least one event. Describe everything you can observe in the video.
        """;

    private async Task<List<RawMatchEvent>> RunEventLoggerAsync(
        string cacheName,
        string visualDna,
        string modelId,
        int thinkingBudget,
        string prompt,
        CancellationToken ct)
    {
        var request = new
        {
            cachedContent = cacheName,
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = PipelineConfigFloat("Phase2_EventLogger:Temperature", 0.2f),
                maxOutputTokens = 65535,
                topP = 1.0f,
                responseMimeType = "application/json",
                responseSchema = BuildEventLoggerSchema(),
                thinkingConfig = new { thinkingBudget }
            }
        };

        JsonElement response;
        try
        {
            response = await PostJsonAsync(GenerateContentUrl(modelId), request, ct);
        }
        catch (HttpRequestException ex) when (IsCacheExpired(ex))
        {
            throw new ContextCacheExpiredException(cacheName);
        }

        var json = ExtractText(response);

        _logger.LogInformation("Phase 2 raw response ({Length} chars): {Preview}",
            json.Length, json[..Math.Min(500, json.Length)]);

        var parsed = JsonSerializer.Deserialize<EventLoggerResponse>(json, JsonOptions);
        return parsed?.Events ?? [];
    }

    // ── Phase 3: Event Verifier ─────────────────────────────────────────

    public async Task<List<RawMatchEvent>> VerifyEventsAsync(
        string cacheName,
        string visualDna,
        List<RawMatchEvent> events,
        CancellationToken ct)
    {
        _logger.LogInformation("Phase 3: Verifying {Count} events", events.Count);

        // Must match the cache model (Phase 1) — Vertex AI requires cached content
        // model to match inference model. Cannot use flash-lite with a pro cache.
        var modelId = PipelineConfig("Phase3_EventVerifier:ModelId", "gemini-3.1-pro-preview");
        var thinkingBudget = PipelineConfigInt("Phase3_EventVerifier:ThinkingBudgetTokens", 4096);
        var eventsJson = JsonSerializer.Serialize(new { events }, JsonOptions);

        var prompt = $"""
            You are a BJJ match identity verification specialist. You have a list of timestamped
            events from a BJJ match and the Visual DNA profile of the student athlete.

            For each event, examine the video at the specified timestamp and verify:
            1. Is the "actor" field correct? Does the person performing this action match the Visual DNA?
            2. Are "position_before" and "position_after" accurate for what you see?
            3. Is the "technique_category" correct?

            You may CORRECT any field you find inaccurate. LOWER the confidence score for any
            event where verification is ambiguous (camera angle, athletes obscured).
            If an actor was wrong, swap it. If a position was misidentified, correct it.

            Do NOT add new events. Do NOT remove events. Only correct existing ones.

            STUDENT VISUAL DNA:
            {visualDna}

            EVENTS TO VERIFY:
            {eventsJson}

            Return the complete event list with any corrections applied.
            """;

        var request = new
        {
            cachedContent = cacheName,
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = PipelineConfigFloat("Phase3_EventVerifier:Temperature", 0.1f),
                maxOutputTokens = 65535,
                responseMimeType = "application/json",
                responseSchema = BuildEventLoggerSchema(),
                thinkingConfig = new { thinkingBudget }
            }
        };

        JsonElement response;
        try
        {
            response = await PostJsonAsync(GenerateContentUrl(modelId), request, ct);
        }
        catch (HttpRequestException ex) when (IsCacheExpired(ex))
        {
            throw new ContextCacheExpiredException(cacheName);
        }

        var json = ExtractText(response);

        _logger.LogInformation("Phase 3 raw response ({Length} chars): {Preview}",
            json.Length, json[..Math.Min(300, json.Length)]);

        var parsed = JsonSerializer.Deserialize<EventLoggerResponse>(json, JsonOptions);
        var verified = parsed?.Events ?? events;

        _logger.LogInformation("Phase 3 complete: {Count} events verified", verified.Count);
        return verified;
    }

    // ── Phase 4: Head Coach (video-aware) ───────────────────────────────

    public async Task<CoachingResult> GenerateCoachingReportAsync(
        string cacheName,
        string verifiedEventsJson,
        string? beltLevel,
        string? experienceLevel,
        string? primaryGoal,
        int? staminaMinutes,
        decimal? heightCm,
        decimal? weightKg,
        CancellationToken ct)
    {
        _logger.LogInformation("Phase 4: Generating coaching report");

        var modelId = PipelineConfig("Phase4_HeadCoach:ModelId", "gemini-3.1-pro-preview");
        var thinkingBudget = PipelineConfigInt("Phase4_HeadCoach:ThinkingBudgetTokens", 16384);

        var heightMeters = heightCm.HasValue ? $"{heightCm.Value / 100m:F2}" : "unknown";
        var weightStr = weightKg.HasValue ? $"{weightKg.Value:F1}" : "unknown";

        var studentProfile = $"""
            Belt level: {beltLevel ?? "unknown"}
            Experience: {experienceLevel ?? "unknown"}
            Stamina: {staminaMinutes?.ToString() ?? "unknown"} minutes
            Primary goal: {primaryGoal ?? "not specified"}
            Height: {heightMeters} meter
            Weight: {weightStr} kilogram
            """;

        var phase4Prompt = $"""
            You are an expert Brazilian Jiu-Jitsu Head Coach and IBJJF Black Belt competitor.
            You have access to both the match video and a structured event log verified for accuracy.

            Review your student's BJJ match and produce actionable coaching feedback.
            Prioritize score-losing technical errors. Reference specific moments in the video
            using millisecond timestamps so the mobile app can seek directly to them.

            VERIFIED EVENT LOG:
            {verifiedEventsJson}

            STUDENT PROFILE:
            {studentProfile}

            Return structured coaching feedback with:
            1. match_summary: A narrative paragraph summarizing the match flow and outcome.
            2. key_strengths (max 3): Specific things done well, with video timestamps (ms).
            3. critical_weaknesses (max 3): Specific errors with video timestamp ranges (ms),
               severity level (Critical/Major/Minor), and IBJJF scoring impact where applicable.
            4. prescribed_drills (exactly 3): Targeted positional sparring drills for the weaknesses.
            5. technical_grade: 0-100 score reflecting overall technical execution.
            6. grade_label: One-line descriptor (e.g., "Solid Fundamentals").
            7. elite_tip: ONE highly specific "1% detail" a black belt would notice.
            """;

        var request = new
        {
            cachedContent = cacheName,
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = phase4Prompt } }
                }
            },
            generationConfig = new
            {
                temperature = PipelineConfigFloat("Phase4_HeadCoach:Temperature", 0.4f),
                maxOutputTokens = 8192,
                responseMimeType = "application/json",
                responseSchema = BuildCoachingSchema(),
                thinkingConfig = new { thinkingBudget }
            }
        };

        JsonElement response;
        try
        {
            response = await PostJsonAsync(GenerateContentUrl(modelId), request, ct);
        }
        catch (HttpRequestException ex) when (IsCacheExpired(ex))
        {
            throw new ContextCacheExpiredException(cacheName);
        }

        var json = ExtractText(response);
        var parsed = JsonSerializer.Deserialize<CoachingJsonResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse coaching report response.");

        _logger.LogInformation("Phase 4 complete. Grade: {Grade} ({Label})", parsed.TechnicalGrade, parsed.GradeLabel);

        return new CoachingResult(
            parsed.MatchSummary,
            parsed.TechnicalGrade,
            parsed.GradeLabel,
            parsed.EliteTip,
            parsed.KeyStrengths.Select(s => new CoachingStrength(s.Title, s.Explanation, s.TimestampStartMs, s.TimestampEndMs)).ToList(),
            parsed.CriticalWeaknesses.Select(w => new CoachingWeakness(w.Title, w.Explanation, w.TimestampStartMs, w.TimestampEndMs, w.Severity, w.ScoringImpact)).ToList(),
            parsed.PrescribedDrills.Select(d => new CoachingDrill(d.DrillName, d.Instructions, d.Goal)).ToList());
    }

    // ── Cache Deletion ──────────────────────────────────────────────────

    public async Task DeleteCacheAsync(string cacheName, CancellationToken ct)
    {
        _logger.LogInformation("Deleting context cache {CacheName}", cacheName);
        await DeleteAsync(DeleteCacheUrl(cacheName), ct);
    }

    // ── Phase 2.8 v2 (ADR-054 §5) — single-shot structured doc extraction ─

    public async Task<string> ExtractStructuredAsync(
        string gcsUri,
        string mimeType,
        string promptText,
        object responseSchema,
        CancellationToken ct)
    {
        // Use the same fast model the rest of the pipeline uses. No context
        // cache here — extraction is one-and-done; caching a 1-file payload
        // for 30 minutes would burn quota with no upside. The thinking budget
        // is small (4k) because the task is structured-output extraction, not
        // multi-step reasoning.
        var modelId = PipelineConfig("DocumentExtractor:ModelId", "gemini-3.1-pro-preview");
        var thinkingBudget = PipelineConfigInt("DocumentExtractor:ThinkingBudgetTokens", 4096);

        _logger.LogInformation(
            "Document extraction: model={ModelId} gcsUri={GcsUri} mimeType={MimeType}",
            modelId, gcsUri, mimeType);

        var request = new
        {
            // No cachedContent — direct fileData inline. Vertex AI accepts a
            // gs:// URI as fileData; ADC service-account auth fetches it.
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { fileData = new { fileUri = gcsUri, mimeType } },
                        new { text = promptText },
                    },
                },
            },
            generationConfig = new
            {
                temperature = PipelineConfigFloat("DocumentExtractor:Temperature", 0.0f),
                maxOutputTokens = 65535,
                topP = 1.0f,
                responseMimeType = "application/json",
                responseSchema,
                thinkingConfig = new { thinkingBudget },
            },
        };

        var response = await PostJsonAsync(GenerateContentUrl(modelId), request, ct);
        var text = ExtractText(response);

        _logger.LogInformation(
            "Document extraction response ({Length} chars): {Preview}",
            text.Length, text[..Math.Min(500, text.Length)]);

        return text;
    }

    // ── HTTP Helpers ─────────────────────────────────────────────────────

    private async Task<JsonElement> PostJsonAsync(string url, object body, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body, options: JsonOptions);

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Vertex AI request failed with {(int)response.StatusCode}: {errorBody[..Math.Min(500, errorBody.Length)]}",
                null,
                response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct);
    }

    private async Task DeleteAsync(string url, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync();
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Cache delete failed with {StatusCode}: {Body}",
                (int)response.StatusCode, body[..Math.Min(200, body.Length)]);
        }
    }

    private static string ExtractText(JsonElement response)
    {
        return response
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Response contained no text in candidates[0].content.parts[0].text");
    }

    private static bool IsCacheExpired(HttpRequestException ex) =>
        ex.StatusCode == HttpStatusCode.NotFound;

    // ── Schemas ─────────────────────────────────────────────────────────

    private static object BuildEventLoggerSchema() => new
    {
        type = "OBJECT",
        required = new[] { "events" },
        properties = new
        {
            events = new
            {
                type = "ARRAY",
                description = "List of all match events observed in the video, in chronological order",
                items = new
                {
                    type = "OBJECT",
                    required = new[]
                    {
                        "start_timestamp_ms", "end_timestamp_ms", "actor",
                        "technique_category", "technique_name",
                        "position_before", "position_after",
                        "outcome", "actions_description", "confidence"
                    },
                    properties = new Dictionary<string, object>
                    {
                        ["start_timestamp_ms"] = new { type = "INTEGER", description = "Absolute milliseconds from start of video when this action begins" },
                        ["end_timestamp_ms"] = new { type = "INTEGER", description = "Absolute milliseconds from start of video when this action ends" },
                        ["actor"] = new { type = "STRING", description = "Who performs this action", @enum = new[] { "Student", "Opponent" } },
                        ["technique_category"] = new
                        {
                            type = "STRING",
                            description = "Category of technique. Use one of: Takedown, Submission, Sweep, Pass, Escape, Transition, Control, Defense, GuardPull, Scramble, StandUp, GripFight"
                        },
                        ["technique_name"] = new { type = "STRING", description = "Specific technique name, e.g. 'Double Leg', 'Armbar from Guard', 'Knee Slice Pass'" },
                        ["position_before"] = new
                        {
                            type = "STRING",
                            description = "Position at the start of this action. Use one of: Standing, OpenGuard, ClosedGuard, HalfGuard, SideControl, Mount, BackControl, Turtle, KneeOnBelly, NorthSouth, FiftyFifty, Crucifix, Scramble"
                        },
                        ["position_after"] = new
                        {
                            type = "STRING",
                            description = "Position at the end of this action. Use one of: Standing, OpenGuard, ClosedGuard, HalfGuard, SideControl, Mount, BackControl, Turtle, KneeOnBelly, NorthSouth, FiftyFifty, Crucifix, Scramble"
                        },
                        ["outcome"] = new
                        {
                            type = "STRING",
                            description = "Result of this action",
                            @enum = new[] { "Successful", "Failed", "Partial", "Countered", "InProgress" }
                        },
                        ["guard_type"] = new { type = "STRING", nullable = true, description = "Specific guard variant if position is a guard (e.g. 'De La Riva', 'Spider', 'Butterfly'). Null when not in guard." },
                        ["submission_type"] = new { type = "STRING", nullable = true, description = "Specific submission name when technique_category is Submission (e.g. 'Rear Naked Choke', 'Armbar'). Null otherwise." },
                        ["actions_description"] = new { type = "STRING", description = "Free-text description of what happened during this event" },
                        ["confidence"] = new { type = "NUMBER", description = "Confidence score 0.0-1.0, the minimum of actor attribution, technique classification, and position identification confidence" }
                    }
                }
            }
        }
    };

    private static object BuildCoachingSchema() => new
    {
        type = "OBJECT",
        required = new[] { "match_summary", "key_strengths", "critical_weaknesses", "prescribed_drills", "technical_grade", "grade_label", "elite_tip" },
        properties = new Dictionary<string, object>
        {
            ["match_summary"] = new { type = "STRING" },
            ["technical_grade"] = new { type = "NUMBER" },
            ["grade_label"] = new { type = "STRING" },
            ["elite_tip"] = new { type = "STRING" },
            ["key_strengths"] = new
            {
                type = "ARRAY",
                items = new
                {
                    type = "OBJECT",
                    required = new[] { "title", "explanation", "timestamp_start_ms", "timestamp_end_ms" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "STRING" },
                        ["explanation"] = new { type = "STRING" },
                        ["timestamp_start_ms"] = new { type = "INTEGER" },
                        ["timestamp_end_ms"] = new { type = "INTEGER" }
                    }
                }
            },
            ["critical_weaknesses"] = new
            {
                type = "ARRAY",
                items = new
                {
                    type = "OBJECT",
                    required = new[] { "title", "explanation", "timestamp_start_ms", "timestamp_end_ms", "severity" },
                    properties = new Dictionary<string, object>
                    {
                        ["title"] = new { type = "STRING" },
                        ["explanation"] = new { type = "STRING" },
                        ["timestamp_start_ms"] = new { type = "INTEGER" },
                        ["timestamp_end_ms"] = new { type = "INTEGER" },
                        ["severity"] = new { type = "STRING", @enum = new[] { "Critical", "Major", "Minor" } },
                        ["scoring_impact"] = new { type = "STRING", nullable = true }
                    }
                }
            },
            ["prescribed_drills"] = new
            {
                type = "ARRAY",
                items = new
                {
                    type = "OBJECT",
                    required = new[] { "drill_name", "instructions", "goal" },
                    properties = new Dictionary<string, object>
                    {
                        ["drill_name"] = new { type = "STRING" },
                        ["instructions"] = new { type = "STRING" },
                        ["goal"] = new { type = "STRING" }
                    }
                }
            }
        }
    };

    // ── JSON Deserialization Models ──────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private record EventLoggerResponse(List<RawMatchEvent> Events);

    private record CoachingJsonResponse(
        string MatchSummary,
        decimal TechnicalGrade,
        string GradeLabel,
        string? EliteTip,
        List<CoachingJsonStrength> KeyStrengths,
        List<CoachingJsonWeakness> CriticalWeaknesses,
        List<CoachingJsonDrill> PrescribedDrills);

    private record CoachingJsonStrength(string Title, string Explanation, long TimestampStartMs, long TimestampEndMs);
    private record CoachingJsonWeakness(string Title, string Explanation, long TimestampStartMs, long TimestampEndMs, string Severity, string? ScoringImpact);
    private record CoachingJsonDrill(string DrillName, string Instructions, string Goal);
}

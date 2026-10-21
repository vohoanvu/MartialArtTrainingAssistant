using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SharedEntities.Models;
using VideoAnalysis.Server.Domain.YoutubeSharingService;
using VideoAnalysis.Server.Models.Dtos;

namespace VideoAnalysis.Server.Domain.AIServices
{
    public interface ITechniqueResourceSearchService
    {
        /// <summary>Finds instructional videos + web resources for a technique taught in <paramref name="session"/>.</summary>
        Task<TechniqueSearchResult> SearchAsync(string techniqueName, TrainingSession session, CancellationToken ct = default);
    }

    /// <summary>
    /// Technique resource search — the replacement for the xAI Grok live search. Gemini on Vertex AI runs
    /// with Grounding with Google Search to pick instructional YouTube videos and web resources; every YouTube
    /// link it returns is then confirmed (exists, public, embeddable) with the YouTube Data API, because a
    /// model-written video id is not trustworthy on its own (Grok's embed links were frequently dead). Falls
    /// back to a plain YouTube keyword search when Vertex fails or returns too few playable videos.
    /// </summary>
    public class TechniqueResourceSearchService : ITechniqueResourceSearchService
    {
        public const string SourceGrounded = "vertex-grounded";
        public const string SourceYoutubeFallback = "youtube-fallback";
        private const int MaxWebResources = 10;

        private static readonly Regex YoutubeIdPattern = new(
            @"(?:youtube\.com/(?:watch\?(?:[^#\s]*&)?v=|embed/|shorts/|live/)|youtu\.be/)([A-Za-z0-9_-]{11})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BareYoutubeId = new(@"^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);

        private static readonly JsonSerializerOptions ParseOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        private readonly IVertexRestClient _vertex;
        private readonly IAiModelConfigService _modelConfig;
        private readonly IYoutubeDataService _youtube;
        private readonly ILogger<TechniqueResourceSearchService> _logger;

        public TechniqueResourceSearchService(
            IVertexRestClient vertex,
            IAiModelConfigService modelConfig,
            IYoutubeDataService youtube,
            ILogger<TechniqueResourceSearchService> logger)
        {
            _vertex = vertex;
            _modelConfig = modelConfig;
            _youtube = youtube;
            _logger = logger;
        }

        public async Task<TechniqueSearchResult> SearchAsync(string techniqueName, TrainingSession session, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(techniqueName))
                throw new ArgumentException("Technique name cannot be empty.", nameof(techniqueName));

            var settings = await _modelConfig.GetEffectiveAsync(AiFeatureKeys.TechniqueSearch, ct);
            var search = settings.Search ?? new AiSearchSettings();
            var keywordQuery = BuildKeywordQuery(techniqueName, session.MartialArt);

            TechniqueSearchResult result;
            try
            {
                result = await GroundedSearchAsync(techniqueName, session, settings, search, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Grounded technique search failed for '{Technique}'; falling back to YouTube keyword search.", techniqueName);
                result = new TechniqueSearchResult { Source = SourceYoutubeFallback };
            }

            // Top up with a plain keyword search so instructors always get something playable.
            var minimumVideos = Math.Min(3, search.MaxResults);
            if (result.YoutubeVideos.Count < minimumVideos)
            {
                try
                {
                    var extra = await _youtube.SearchVideosAsync(keywordQuery, search.MaxResults);
                    var seen = result.YoutubeVideos.Select(v => v.VideoId).ToHashSet();
                    result.YoutubeVideos.AddRange(extra
                        .Where(v => seen.Add(v.VideoId))
                        .Take(search.MaxResults - result.YoutubeVideos.Count)
                        .Select(v => ToVideo(v, whyRelevant: null)));
                }
                catch (Exception ex) when (result.YoutubeVideos.Count > 0 || result.WebResources.Count > 0)
                {
                    // Keep the grounded results we already have rather than failing the whole search.
                    _logger.LogWarning(ex, "YouTube keyword top-up failed for '{Technique}'.", techniqueName);
                }
            }

            return result;
        }

        private async Task<TechniqueSearchResult> GroundedSearchAsync(
            string techniqueName, TrainingSession session, AiFeatureSettings settings, AiSearchSettings search, CancellationToken ct)
        {
            var model = GeminiModelCatalog.Resolve(settings.ModelId);
            var body = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new object[] { new { text = BuildPrompt(techniqueName, session, search.MaxResults) } } },
                },
                // Grounding with Google Search. No responseSchema: structured output combined with the search
                // tool isn't documented for every model, so JSON is requested in the prompt and parsed leniently.
                tools = new object[] { new { googleSearch = new { } } },
                generationConfig = GeminiModelCatalog.BuildGenerationConfig(
                    model, settings.ThinkingLevel, settings.ThinkingBudget, settings.Temperature, settings.MaxOutputTokens),
            };

            var response = await _vertex.GenerateContentAsync(model.Id, settings.Location, body, ct);
            var parsed = ParseModelJson(_vertex.ExtractText(response));
            var grounding = ReadGrounding(response);

            // ── YouTube: extract ids from whatever the model wrote, then verify ──
            var candidates = new List<(string Id, ModelVideo Video)>();
            foreach (var v in parsed.YoutubeVideos)
            {
                var id = ExtractYoutubeId(v.Url) ?? ExtractYoutubeId(v.VideoId);
                if (id != null && candidates.All(c => c.Id != id)) candidates.Add((id, v));
            }
            candidates = candidates.Take(search.MaxResults).ToList();

            List<TechniqueSearchVideo> videos;
            if (search.VerifyWithYouTube)
            {
                var playable = await _youtube.GetPlayableVideosAsync(candidates.Select(c => c.Id).ToList());
                var why = candidates.ToDictionary(c => c.Id, c => c.Video.WhyRelevant);
                videos = playable.Select(v => ToVideo(v, why.GetValueOrDefault(v.VideoId))).ToList();
                _logger.LogInformation("Technique search '{Technique}': {Verified}/{Proposed} model-proposed YouTube videos verified playable.",
                    techniqueName, videos.Count, candidates.Count);
            }
            else
            {
                videos = candidates.Select(c => new TechniqueSearchVideo
                {
                    VideoId = c.Id,
                    Title = c.Video.Title ?? string.Empty,
                    Description = c.Video.Description ?? string.Empty,
                    EmbedLink = $"https://www.youtube.com/embed/{c.Id}",
                    PublicationDate = c.Video.PublicationDate ?? string.Empty,
                    ThumbnailUrl = $"https://img.youtube.com/vi/{c.Id}/mqdefault.jpg",
                    ChannelTitle = c.Video.Channel,
                    WhyRelevant = c.Video.WhyRelevant,
                }).ToList();
            }

            return new TechniqueSearchResult
            {
                Source = SourceGrounded,
                ModelId = model.Id,
                YoutubeVideos = videos,
                WebResources = SelectWebResources(parsed.WebResources, grounding.Sources),
                WebSearchQueries = grounding.Queries,
                SearchEntryPointHtml = grounding.SearchEntryPointHtml,
            };
        }

        // ── Prompt ───────────────────────────────────────────────────────────

        private static string BuildPrompt(string techniqueName, TrainingSession session, int maxResults)
        {
            var art = MartialArtLabel(session.MartialArt);
            return $$"""
                You are helping a {{art}} instructor prepare a {{session.TargetLevel}}-level class ({{session.Capacity}} students, {{session.Duration}} hours).
                Use Google Search to find high-quality instructional content for the technique "{{techniqueName}}" in {{art}} (or the closest related techniques).

                1. Up to {{maxResults}} YouTube instructional videos. Prefer reputable instructors/channels, clear technical breakdowns, and videos from the last 5 years.
                   Only include videos you actually found via search — copy the exact youtube.com/watch?v= URL. Never invent or guess a video id.
                2. Up to {{MaxWebResources}} other web resources (articles, technique breakdowns, instructionals). Mark is_free = true only if the content is accessible without payment or login.

                Respond with ONLY a JSON object (no prose, no markdown fences) of this shape:
                {
                  "youtube_videos": [ { "url": "https://www.youtube.com/watch?v=...", "title": "...", "channel": "...", "publication_date": "YYYY-MM-DD", "why_relevant": "one sentence on why this helps teach the technique" } ],
                  "web_resources": [ { "title": "...", "url": "https://...", "description": "1-2 sentences", "is_free": true } ]
                }
                """;
        }

        // ── Response parsing ─────────────────────────────────────────────────

        private ModelSearchOutput ParseModelJson(string text)
        {
            var trimmed = text.Trim();
            int start = trimmed.IndexOf('{'), end = trimmed.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                _logger.LogWarning("Grounded search returned no JSON object.");
                return new ModelSearchOutput();
            }
            try
            {
                return JsonSerializer.Deserialize<ModelSearchOutput>(trimmed[start..(end + 1)], ParseOptions) ?? new ModelSearchOutput();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Grounded search returned unparseable JSON.");
                return new ModelSearchOutput();
            }
        }

        private static (List<string> Queries, string? SearchEntryPointHtml, List<(string Uri, string? Title)> Sources) ReadGrounding(JsonElement response)
        {
            var queries = new List<string>();
            var sources = new List<(string, string?)>();
            string? entryPoint = null;

            if (response.ValueKind != JsonValueKind.Object
                || !response.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0
                || !candidates[0].TryGetProperty("groundingMetadata", out var meta))
                return (queries, entryPoint, sources);

            if (meta.TryGetProperty("webSearchQueries", out var q) && q.ValueKind == JsonValueKind.Array)
                queries.AddRange(q.EnumerateArray().Select(e => e.GetString()).OfType<string>());
            if (meta.TryGetProperty("searchEntryPoint", out var sep) && sep.TryGetProperty("renderedContent", out var html))
                entryPoint = html.GetString();
            if (meta.TryGetProperty("groundingChunks", out var chunks) && chunks.ValueKind == JsonValueKind.Array)
            {
                foreach (var chunk in chunks.EnumerateArray())
                {
                    if (chunk.TryGetProperty("web", out var web) && web.TryGetProperty("uri", out var uri) && uri.GetString() is { } u)
                        sources.Add((u, web.TryGetProperty("title", out var t) ? t.GetString() : null));
                }
            }
            return (queries, entryPoint, sources);
        }

        /// <summary>
        /// Keeps model-proposed resources whose domain appears among the grounding sources (Vertex reports each
        /// source's domain as the chunk title), so a hallucinated URL can't slip through. If the model's list
        /// yields nothing grounded, the grounding sources themselves are returned.
        /// </summary>
        private static List<TechniqueSearchWebResource> SelectWebResources(List<ModelWebResource> proposed, List<(string Uri, string? Title)> sources)
        {
            var groundedDomains = sources
                .Select(s => s.Title?.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t) && t.Contains('.'))
                .Select(t => t!.StartsWith("www.") ? t[4..] : t!)
                .ToHashSet();

            var kept = proposed
                .Where(r => Uri.TryCreate(r.Url, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                    && ExtractYoutubeId(r.Url) == null
                    && groundedDomains.Any(d => HostMatches(uri.Host, d)))
                .GroupBy(r => r.Url)
                .Select(g => g.First())
                .Take(MaxWebResources)
                .Select(r => new TechniqueSearchWebResource { Title = r.Title ?? r.Url!, Url = r.Url!, Description = r.Description, IsFree = ReadBool(r.IsFree) })
                .ToList();

            if (kept.Count > 0) return kept;

            return sources
                .Where(s => !(s.Title ?? string.Empty).Contains("youtube", StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => s.Uri)
                .Select(g => g.First())
                .Take(MaxWebResources)
                .Select(s => new TechniqueSearchWebResource { Title = s.Title ?? s.Uri, Url = s.Uri })
                .ToList();
        }

        // Models sometimes write "true"/"yes" instead of a JSON bool; tolerate that rather than fail the parse.
        private static bool? ReadBool(JsonElement? value) => value?.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => value.Value.GetString()?.Trim().ToLowerInvariant() switch
            {
                "true" or "yes" or "free" => true,
                "false" or "no" or "paid" => false,
                _ => null,
            },
            _ => null,
        };

        private static bool HostMatches(string host, string domain)
        {
            host = host.ToLowerInvariant();
            return host == domain || host.EndsWith("." + domain, StringComparison.Ordinal);
        }

        public static string? ExtractYoutubeId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            if (BareYoutubeId.IsMatch(trimmed)) return trimmed;
            var match = YoutubeIdPattern.Match(trimmed);
            return match.Success ? match.Groups[1].Value : null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        public static string BuildKeywordQuery(string techniqueName, MartialArt martialArt)
        {
            // Use text before colon if present (e.g. "Guard Retention: The Shin-Shield to Frames" → "Guard Retention")
            var name = techniqueName.Contains(':') ? techniqueName.Split(':')[0].Trim() : techniqueName;
            return $"{name} {MartialArtLabel(martialArt)} tutorial";
        }

        private static string MartialArtLabel(MartialArt martialArt) => martialArt switch
        {
            MartialArt.BrazilianJiuJitsu_GI => "BJJ Gi",
            MartialArt.BrazilianJiuJitsu_NO_GI => "BJJ No-Gi",
            _ => Regex.Replace(martialArt.ToString(), "([a-z])([A-Z])", "$1 $2").Replace("_", " "),
        };

        private static TechniqueSearchVideo ToVideo(YoutubeSearchResult v, string? whyRelevant) => new()
        {
            VideoId = v.VideoId,
            Title = v.Title,
            Description = v.Description,
            EmbedLink = v.EmbedLink,
            PublicationDate = v.PublishedAt,
            ThumbnailUrl = v.ThumbnailUrl,
            ChannelTitle = v.ChannelTitle,
            WhyRelevant = whyRelevant,
        };

        private sealed class ModelSearchOutput
        {
            public List<ModelVideo> YoutubeVideos { get; set; } = [];
            public List<ModelWebResource> WebResources { get; set; } = [];
        }

        private sealed class ModelVideo
        {
            public string? Url { get; set; }
            public string? VideoId { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Channel { get; set; }
            public string? PublicationDate { get; set; }
            public string? WhyRelevant { get; set; }
        }

        private sealed class ModelWebResource
        {
            public string? Title { get; set; }
            public string? Url { get; set; }
            public string? Description { get; set; }
            public JsonElement? IsFree { get; set; }
        }
    }

    /// <summary>Response of <c>POST api/youtube/search</c> (serialized snake_case).</summary>
    public class TechniqueSearchResult
    {
        public required string Source { get; set; }
        public string? ModelId { get; set; }
        public List<TechniqueSearchVideo> YoutubeVideos { get; set; } = [];
        public List<TechniqueSearchWebResource> WebResources { get; set; } = [];
        public List<string> WebSearchQueries { get; set; } = [];

        /// <summary>Google Search Suggestions widget (HTML/CSS) — must be displayed unmodified alongside grounded results.</summary>
        public string? SearchEntryPointHtml { get; set; }
    }

    public class TechniqueSearchVideo
    {
        public required string VideoId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string EmbedLink { get; set; }
        public required string PublicationDate { get; set; }
        public required string ThumbnailUrl { get; set; }
        public string? ChannelTitle { get; set; }
        public string? WhyRelevant { get; set; }
    }

    public class TechniqueSearchWebResource
    {
        public required string Title { get; set; }
        public required string Url { get; set; }
        public string? Description { get; set; }
        public bool? IsFree { get; set; }
    }
}

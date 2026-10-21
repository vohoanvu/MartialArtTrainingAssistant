using System.Text.RegularExpressions;
using SharedEntities.Models;
using VideoAnalysis.Server.Domain.YoutubeSharingService;

namespace VideoAnalysis.Server.Domain.AIServices
{
    public interface ITechniqueResourceSearchService
    {
        /// <summary>Finds instructional videos for a technique taught in <paramref name="session"/>.</summary>
        Task<TechniqueSearchResult> SearchAsync(string techniqueName, TrainingSession session, CancellationToken ct = default);
    }

    /// <summary>
    /// Technique video search: a plain YouTube Data API keyword search. Gemini + Grounding with Google Search was
    /// removed (Oct 2026) because grounded calls on Gemini 4 Argon took 40–120 s and the request timed out at the
    /// 60 s Nginx/Cloudflare gateway (504), and grounded answers carried redirect links instead of YouTube ids.
    /// </summary>
    public class TechniqueResourceSearchService : ITechniqueResourceSearchService
    {
        public const string SourceYoutube = "youtube";
        public const int MaxResults = 8;

        private readonly IYoutubeDataService _youtube;

        public TechniqueResourceSearchService(IYoutubeDataService youtube)
        {
            _youtube = youtube;
        }

        public async Task<TechniqueSearchResult> SearchAsync(string techniqueName, TrainingSession session, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(techniqueName))
                throw new ArgumentException("Technique name cannot be empty.", nameof(techniqueName));

            var videos = await _youtube.SearchVideosAsync(BuildKeywordQuery(techniqueName, session.MartialArt), MaxResults);
            return new TechniqueSearchResult
            {
                Source = SourceYoutube,
                YoutubeVideos = videos.Select(ToVideo).ToList(),
            };
        }

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

        private static TechniqueSearchVideo ToVideo(YoutubeSearchResult v) => new()
        {
            VideoId = v.VideoId,
            Title = v.Title,
            Description = v.Description,
            EmbedLink = v.EmbedLink,
            PublicationDate = v.PublishedAt,
            ThumbnailUrl = v.ThumbnailUrl,
            ChannelTitle = v.ChannelTitle,
        };
    }

    /// <summary>Response of <c>POST api/youtube/search</c> (serialized snake_case).</summary>
    public class TechniqueSearchResult
    {
        public required string Source { get; set; }
        public string? ModelId { get; set; }
        public List<TechniqueSearchVideo> YoutubeVideos { get; set; } = [];
        public List<TechniqueSearchWebResource> WebResources { get; set; } = [];
        public List<string> WebSearchQueries { get; set; } = [];

        /// <summary>Google Search Suggestions widget (HTML/CSS); only set for grounded results, so always null now.</summary>
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

using Google;

namespace VideoAnalysis.Server.Domain.YoutubeSharingService
{
    public interface IYoutubeDataService
    {
        Task<VideoDetailsResponse> GetVideoDetailsAsync(string videoId);
        Task<List<YoutubeSearchResult>> SearchVideosAsync(string query, int maxResults = 10);
    }

    public class YoutubeDataService : IYoutubeDataService
    {
        private readonly IYoutubeServiceWrapper _youtubeServiceWrapper;

        public YoutubeDataService(IYoutubeServiceWrapper youtubeService)
        {
            _youtubeServiceWrapper = youtubeService;
        }

        public async Task<VideoDetailsResponse?> GetVideoDetailsAsync(string videoId)
        {
            try
            {
                var videoRequest = _youtubeServiceWrapper.List("snippet");
                videoRequest.VideoId = videoId;

                var response = await videoRequest.ExecuteAsync();

                if (response.Items.Count > 0)
                {
                    var video = response.Items[0];
                    return new VideoDetailsResponse
                    {
                        VideoId = videoId,
                        Title = video.Snippet.Title,
                        Description = video.Snippet.Description,
                        EmbedLink = $"https://www.youtube.com/embed/{videoId}",
                    };
                }
            }
            catch (Exception ex)
            {
                throw new VideoDetailsException($"Failed to get video details for video ID {videoId}", ex);
            }

            return null;
        }

        public async Task<List<YoutubeSearchResult>> SearchVideosAsync(string query, int maxResults = 10)
        {
            try
            {
                var searchRequest = _youtubeServiceWrapper.Search("snippet");
                searchRequest.Q = query;
                searchRequest.Type = "video";
                searchRequest.MaxResults = maxResults;
                searchRequest.Order = "relevance";

                var response = await searchRequest.ExecuteAsync();

                if (response.Items == null || response.Items.Count == 0)
                    return new List<YoutubeSearchResult>();

                return response.Items
                    .Where(item => item.Id?.VideoId != null)
                    .Select(item => new YoutubeSearchResult
                    {
                        VideoId = item.Id.VideoId,
                        Title = item.Snippet?.Title ?? string.Empty,
                        Description = item.Snippet?.Description ?? string.Empty,
                        EmbedLink = $"https://www.youtube.com/embed/{item.Id.VideoId}",
                        PublishedAt = item.Snippet?.PublishedAtDateTimeOffset?.ToString("o") ?? string.Empty,
                        ThumbnailUrl = item.Snippet?.Thumbnails?.Medium?.Url
                            ?? item.Snippet?.Thumbnails?.Default__?.Url
                            ?? string.Empty,
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new VideoDetailsException($"Failed to search YouTube videos for query '{query}'", ex);
            }
        }
    }

    public class YoutubeSearchResult
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EmbedLink { get; set; }
        public string PublishedAt { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class VideoDetailsResponse
    {
        public int Id { get; set; }
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EmbedLink { get; set; }
        public AppUserDto SharedBy { get; set; }
    }

    public class AppUserDto
    {
        public string UserId { get; set; }

        public string Username { get; set; }
    }

    public class VideoDetailsException : Exception
    {
        public VideoDetailsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}


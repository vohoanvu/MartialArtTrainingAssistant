using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace SampleAspNetReactDockerApp.Server.Helpers
{
    public interface IYoutubeDataService
    {
        Task<VideoDetailsResponse> GetVideoDetailsAsync(string videoId);
    }

    public class YoutubeDataService : IYoutubeDataService
    {
        private readonly YouTubeService _youtubeService;

        public YoutubeDataService(IConfiguration configuration)
        {
            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = configuration["YOUTUBE_API_KEY"],
                ApplicationName = GetType().ToString()
            });
        }

        public async Task<VideoDetailsResponse?> GetVideoDetailsAsync(string videoId)
        {
            var videoRequest = _youtubeService.Videos.List("snippet");
            videoRequest.Id = videoId;

            var response = await videoRequest.ExecuteAsync();

            if (response.Items.Count > 0)
            {
                var video = response.Items[0];
                return new VideoDetailsResponse
                {
                    Id = videoId,
                    Title = video.Snippet.Title,
                    Description = video.Snippet.Description,
                    EmbedLink = $"https://www.youtube.com/embed/{videoId}",
                };
            }

            return null;
        }
    }

    public class VideoDetailsResponse
    {
        public string Id { get; set; }

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
}

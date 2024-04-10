using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace SampleAspNetReactDockerApp.Server.Helpers
{
    public class YoutubeDataService
    {
        private readonly YouTubeService _youtubeService;

        public YoutubeDataService(IConfiguration configuration)
        {
            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = configuration["YoutubeApiKey"],
                ApplicationName = this.GetType().ToString()
            });
        }

        public async Task<VideoDetailsResponse> GetVideoDetailsAsync(string videoId)
        {
            var videoRequest = _youtubeService.Videos.List("snippet");
            videoRequest.Id = videoId;

            var response = await videoRequest.ExecuteAsync();

            if (response.Items.Count > 0)
            {
                var video = response.Items[0];
                return new VideoDetailsResponse
                {
                    Title = video.Snippet.Title,
                    Description = video.Snippet.Description
                };
            }

            return new VideoDetailsResponse();
        }
    }

    public class VideoDetailsResponse
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }
}

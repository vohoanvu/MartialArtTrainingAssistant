using Google;

namespace VideoSharing.Server.Domain.YoutubeSharingService
{
    public interface IYoutubeDataService
    {
        Task<VideoDetailsResponse> GetVideoDetailsAsync(string videoId);
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

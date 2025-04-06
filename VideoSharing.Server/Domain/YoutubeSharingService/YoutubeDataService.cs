using Google;

namespace VideoSharing.Server.Domain.YoutubeSharingService
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IYoutubeDataService
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task<VideoDetailsResponse> GetVideoDetailsAsync(string videoId);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class YoutubeDataService : IYoutubeDataService
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private readonly IYoutubeServiceWrapper _youtubeServiceWrapper;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public YoutubeDataService(IYoutubeServiceWrapper youtubeService)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            _youtubeServiceWrapper = youtubeService;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<VideoDetailsResponse?> GetVideoDetailsAsync(string videoId)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class VideoDetailsResponse
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public int Id { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string VideoId { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string Title { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string Description { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string EmbedLink { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public AppUserDto SharedBy { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AppUserDto
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string UserId { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string Username { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class VideoDetailsException : Exception
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public VideoDetailsException(string message, Exception innerException)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            : base(message, innerException)
        {
        }
    }
}

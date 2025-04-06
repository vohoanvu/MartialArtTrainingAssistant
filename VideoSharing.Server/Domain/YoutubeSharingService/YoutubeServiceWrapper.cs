using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace VideoSharing.Server.Domain.YoutubeSharingService
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IYoutubeServiceWrapper
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        IListRequestWrapper List(string part);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class YoutubeServiceWrapper : IYoutubeServiceWrapper
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private readonly YouTubeService _youTubeService;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public YoutubeServiceWrapper(YouTubeService youTubeService)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            _youTubeService = youTubeService;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public IListRequestWrapper List(string part)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            return new ListRequestWrapper(_youTubeService.Videos.List(part));
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IListRequestWrapper
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Repeatable<string> VideoId { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task<VideoListResponse> ExecuteAsync();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ListRequestWrapper : IListRequestWrapper
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private readonly VideosResource.ListRequest _listRequest;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ListRequestWrapper(VideosResource.ListRequest listRequest)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            _listRequest = listRequest;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Repeatable<string> VideoId
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            get { return _listRequest.Id; }
            set { _listRequest.Id = value; }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<VideoListResponse> ExecuteAsync()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            return await _listRequest.ExecuteAsync();
        }
    }
}

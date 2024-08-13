using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace VideoSharing.Server.Domain.YoutubeSharingService
{
    public interface IYoutubeServiceWrapper
    {
        IListRequestWrapper List(string part);
    }

    public class YoutubeServiceWrapper : IYoutubeServiceWrapper
    {
        private readonly YouTubeService _youTubeService;

        public YoutubeServiceWrapper(YouTubeService youTubeService)
        {
            _youTubeService = youTubeService;
        }

        public IListRequestWrapper List(string part)
        {
            return new ListRequestWrapper(_youTubeService.Videos.List(part));
        }
    }

    public interface IListRequestWrapper
    {
        Repeatable<string> VideoId { get; set; }
        Task<VideoListResponse> ExecuteAsync();
    }

    public class ListRequestWrapper : IListRequestWrapper
    {
        private readonly VideosResource.ListRequest _listRequest;

        public ListRequestWrapper(VideosResource.ListRequest listRequest)
        {
            _listRequest = listRequest;
        }

        public Repeatable<string> VideoId
        {
            get { return _listRequest.Id; }
            set { _listRequest.Id = value; }
        }

        public async Task<VideoListResponse> ExecuteAsync()
        {
            return await _listRequest.ExecuteAsync();
        }
    }
}

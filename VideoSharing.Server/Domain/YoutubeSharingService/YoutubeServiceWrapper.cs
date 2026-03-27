using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace VideoSharing.Server.Domain.YoutubeSharingService
{
    public interface IYoutubeServiceWrapper
    {
        IListRequestWrapper List(string part);
        ISearchRequestWrapper Search(string part);
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

        public ISearchRequestWrapper Search(string part)
        {
            return new SearchRequestWrapper(_youTubeService.Search.List(part));
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

    public interface ISearchRequestWrapper
    {
        string Q { get; set; }
        string Type { get; set; }
        int? MaxResults { get; set; }
        string Order { get; set; }
        Task<SearchListResponse> ExecuteAsync();
    }

    public class SearchRequestWrapper : ISearchRequestWrapper
    {
        private readonly SearchResource.ListRequest _searchRequest;

        public SearchRequestWrapper(SearchResource.ListRequest searchRequest)
        {
            _searchRequest = searchRequest;
        }

        public string Q
        {
            get { return _searchRequest.Q; }
            set { _searchRequest.Q = value; }
        }

        public string Type
        {
            get { return _searchRequest.Type?.FirstOrDefault(); }
            set { _searchRequest.Type = new Repeatable<string>(new[] { value }); }
        }

        public int? MaxResults
        {
            get { return (int?)_searchRequest.MaxResults; }
            set { _searchRequest.MaxResults = (uint?)value; }
        }

        public string Order
        {
            get { return _searchRequest.Order?.ToString(); }
            set
            {
                if (Enum.TryParse<SearchResource.ListRequest.OrderEnum>(value, ignoreCase: true, out var parsed))
                    _searchRequest.Order = parsed;
            }
        }

        public async Task<SearchListResponse> ExecuteAsync()
        {
            return await _searchRequest.ExecuteAsync();
        }
    }
}

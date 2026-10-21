using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Models;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Domain.YoutubeSharingService;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

public class TechniqueResourceSearchServiceTests
{
    private static YoutubeSearchResult Yt(string id) => new()
    {
        VideoId = id,
        Title = $"Video {id}",
        Description = "desc",
        EmbedLink = $"https://www.youtube.com/embed/{id}",
        PublishedAt = "2025-01-01",
        ThumbnailUrl = $"https://img.youtube.com/vi/{id}/mqdefault.jpg",
        ChannelTitle = "Channel",
    };

    [Fact]
    public async Task Should_SearchYoutubeByKeyword()
    {
        var youtube = new Mock<IYoutubeDataService>();
        youtube.Setup(y => y.SearchVideosAsync("Guard Retention BJJ Gi tutorial", TechniqueResourceSearchService.MaxResults))
            .ReturnsAsync([Yt("CCCCCCCCCCC"), Yt("DDDDDDDDDDD")]);
        var svc = new TechniqueResourceSearchService(youtube.Object);

        var result = await svc.SearchAsync("Guard Retention: The Shin-Shield to Frames", TestFixtures.CreateTrainingSession());

        Assert.Equal(TechniqueResourceSearchService.SourceYoutube, result.Source);
        Assert.Equal(["CCCCCCCCCCC", "DDDDDDDDDDD"], result.YoutubeVideos.Select(v => v.VideoId));
        Assert.Equal("Channel", result.YoutubeVideos[0].ChannelTitle);
        Assert.Empty(result.WebResources);
        Assert.Null(result.SearchEntryPointHtml);
    }

    [Fact]
    public async Task Should_RejectEmptyTechniqueName()
    {
        var svc = new TechniqueResourceSearchService(new Mock<IYoutubeDataService>().Object);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.SearchAsync(" ", TestFixtures.CreateTrainingSession()));
    }

    [Theory]
    [InlineData("Guard Retention: The Shin-Shield to Frames", MartialArt.BrazilianJiuJitsu_GI, "Guard Retention BJJ Gi tutorial")]
    [InlineData("Heel Hook", MartialArt.BrazilianJiuJitsu_NO_GI, "Heel Hook BJJ No-Gi tutorial")]
    public void Should_BuildKeywordQuery(string technique, MartialArt art, string expected)
    {
        Assert.Equal(expected, TechniqueResourceSearchService.BuildKeywordQuery(technique, art));
    }
}

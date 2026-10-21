using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Models;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Domain.YoutubeSharingService;
using VideoAnalysis.Server.Models.Dtos;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

public class TechniqueResourceSearchServiceTests
{
    // Grounded Vertex response: model JSON text + groundingMetadata (camelCase, as Vertex returns it).
    private static readonly JsonElement GroundedResponse = JsonSerializer.SerializeToElement(new
    {
        modelVersion = "gemini-4-argon",
        candidates = new[]
        {
            new
            {
                content = new { parts = new[] { new { text = "" } } },
                groundingMetadata = new
                {
                    webSearchQueries = new[] { "armbar from guard tutorial" },
                    searchEntryPoint = new { renderedContent = "<div class=\"chips\">armbar</div>" },
                    groundingChunks = new object[]
                    {
                        new { web = new { uri = "https://vertexaisearch.cloud.google.com/grounding-api-redirect/a", title = "bjjfanatics.com" } },
                        new { web = new { uri = "https://vertexaisearch.cloud.google.com/grounding-api-redirect/b", title = "youtube.com" } },
                    },
                },
            },
        },
    });

    private const string ModelJson = """
        ```json
        {
          "youtube_videos": [
            { "url": "https://www.youtube.com/watch?v=AAAAAAAAAAA", "title": "Armbar 1", "why_relevant": "Clear breakdown" },
            { "url": "https://youtu.be/BBBBBBBBBBB", "title": "Armbar 2 (hallucinated)" },
            { "url": "https://www.youtube.com/watch?v=AAAAAAAAAAA", "title": "duplicate" }
          ],
          "web_resources": [
            { "title": "Armbar guide", "url": "https://www.bjjfanatics.com/blogs/armbar", "description": "Guide", "is_free": "yes" },
            { "title": "Invented site", "url": "https://not-in-grounding.example/armbar", "is_free": true }
          ]
        }
        ```
        """;

    private static (TechniqueResourceSearchService svc, Mock<IVertexRestClient> vertex, Mock<IYoutubeDataService> youtube)
        CreateService(AiSearchSettings? search = null)
    {
        var modelConfig = new Mock<IAiModelConfigService>();
        modelConfig.Setup(m => m.GetEffectiveAsync(AiFeatureKeys.TechniqueSearch, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiFeatureSettings
            {
                ModelId = "gemini-4-argon",
                Location = "global",
                ThinkingLevel = "LOW",
                Search = search ?? new AiSearchSettings { MaxResults = 8, VerifyWithYouTube = true },
            });

        var vertex = new Mock<IVertexRestClient>();
        var youtube = new Mock<IYoutubeDataService>();
        var svc = new TechniqueResourceSearchService(vertex.Object, modelConfig.Object, youtube.Object,
            new Mock<ILogger<TechniqueResourceSearchService>>().Object);
        return (svc, vertex, youtube);
    }

    private static YoutubeSearchResult Yt(string id, string title = "t") => new()
    {
        VideoId = id, Title = title, Description = "d", EmbedLink = $"https://www.youtube.com/embed/{id}",
        PublishedAt = "2024-01-01T00:00:00+00:00", ThumbnailUrl = "thumb", ChannelTitle = "Channel",
    };

    [Fact]
    public async Task Should_VerifyModelVideos_And_KeepOnlyGroundedWebResources()
    {
        var (svc, vertex, youtube) = CreateService();
        object? sentBody = null;
        vertex.Setup(v => v.GenerateContentAsync("gemini-4-argon", "global", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, object, CancellationToken>((_, _, b, _) => sentBody = b)
            .ReturnsAsync(GroundedResponse);
        vertex.Setup(v => v.ExtractText(It.IsAny<JsonElement>())).Returns(ModelJson);
        // Only AAA… exists on YouTube; BBB… was hallucinated. Duplicate AAA… is collapsed before lookup.
        youtube.Setup(y => y.GetPlayableVideosAsync(It.Is<IReadOnlyList<string>>(ids => ids.SequenceEqual(new[] { "AAAAAAAAAAA", "BBBBBBBBBBB" }))))
            .ReturnsAsync([Yt("AAAAAAAAAAA", "Real title from YouTube")]);
        youtube.Setup(y => y.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync([Yt("CCCCCCCCCCC"), Yt("AAAAAAAAAAA")]);

        var result = await svc.SearchAsync("Armbar", TestFixtures.CreateTrainingSession());

        Assert.Equal(TechniqueResourceSearchService.SourceGrounded, result.Source);
        Assert.Equal("gemini-4-argon", result.ModelId);
        // Verified video uses YouTube's authoritative title + keeps the model's rationale; then topped up to 3? (only 1 extra unique).
        Assert.Equal("Real title from YouTube", result.YoutubeVideos[0].Title);
        Assert.Equal("Clear breakdown", result.YoutubeVideos[0].WhyRelevant);
        Assert.Equal(["AAAAAAAAAAA", "CCCCCCCCCCC"], result.YoutubeVideos.Select(v => v.VideoId));
        // Web resource on a grounded domain kept ("yes" parsed as free); invented domain dropped.
        var resource = Assert.Single(result.WebResources);
        Assert.Equal("https://www.bjjfanatics.com/blogs/armbar", resource.Url);
        Assert.True(resource.IsFree);
        Assert.Equal("<div class=\"chips\">armbar</div>", result.SearchEntryPointHtml);
        Assert.Equal(["armbar from guard tutorial"], result.WebSearchQueries);

        // Request: Google Search grounding tool + Argon thinking level, no budget.
        var body = JsonSerializer.SerializeToElement(sentBody);
        Assert.True(body.GetProperty("tools")[0].TryGetProperty("googleSearch", out _));
        Assert.Equal("LOW", body.GetProperty("generationConfig").GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString());
    }

    [Fact]
    public async Task Should_FallBackToYoutubeSearch_When_VertexFails()
    {
        var (svc, vertex, youtube) = CreateService();
        vertex.Setup(v => v.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Vertex AI request failed with 400", null, HttpStatusCode.BadRequest));
        youtube.Setup(y => y.SearchVideosAsync("Guard Retention BJJ Gi tutorial", 8))
            .ReturnsAsync([Yt("CCCCCCCCCCC"), Yt("DDDDDDDDDDD")]);

        var result = await svc.SearchAsync("Guard Retention: The Shin-Shield to Frames", TestFixtures.CreateTrainingSession());

        Assert.Equal(TechniqueResourceSearchService.SourceYoutubeFallback, result.Source);
        Assert.Equal(2, result.YoutubeVideos.Count);
        Assert.Empty(result.WebResources);
    }

    [Fact]
    public async Task Should_KeepGroundedResults_When_TopUpSearchFails()
    {
        var (svc, vertex, youtube) = CreateService();
        vertex.Setup(v => v.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GroundedResponse);
        vertex.Setup(v => v.ExtractText(It.IsAny<JsonElement>())).Returns(ModelJson);
        youtube.Setup(y => y.GetPlayableVideosAsync(It.IsAny<IReadOnlyList<string>>())).ReturnsAsync([Yt("AAAAAAAAAAA")]);
        youtube.Setup(y => y.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new InvalidOperationException("quota"));

        var result = await svc.SearchAsync("Armbar", TestFixtures.CreateTrainingSession());

        Assert.Single(result.YoutubeVideos);
    }

    [Fact]
    public async Task Should_SkipYoutubeVerification_When_Disabled()
    {
        var (svc, vertex, youtube) = CreateService(new AiSearchSettings { MaxResults = 2, VerifyWithYouTube = false });
        vertex.Setup(v => v.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GroundedResponse);
        vertex.Setup(v => v.ExtractText(It.IsAny<JsonElement>())).Returns(ModelJson);

        var result = await svc.SearchAsync("Armbar", TestFixtures.CreateTrainingSession());

        youtube.Verify(y => y.GetPlayableVideosAsync(It.IsAny<IReadOnlyList<string>>()), Times.Never);
        Assert.Equal(["AAAAAAAAAAA", "BBBBBBBBBBB"], result.YoutubeVideos.Select(v => v.VideoId));
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?feature=share&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?t=10", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/123", null)]
    [InlineData(null, null)]
    public void Should_ExtractYoutubeId(string? input, string? expected)
    {
        Assert.Equal(expected, TechniqueResourceSearchService.ExtractYoutubeId(input));
    }

    [Theory]
    [InlineData("Guard Retention: The Shin-Shield to Frames", MartialArt.BrazilianJiuJitsu_GI, "Guard Retention BJJ Gi tutorial")]
    [InlineData("Heel Hook", MartialArt.BrazilianJiuJitsu_NO_GI, "Heel Hook BJJ No-Gi tutorial")]
    public void Should_BuildKeywordQuery(string technique, MartialArt art, string expected)
    {
        Assert.Equal(expected, TechniqueResourceSearchService.BuildKeywordQuery(technique, art));
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using SharedEntities.Models;
using VideoAnalysis.Server.Domain.AIServices;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

/// <summary>
/// Tests for XAIService.SearchVideosAsync.
/// The constructor reads environment variables for API key and endpoint;
/// these tests set them before constructing the service.
/// </summary>
public class XAIServiceTests : IDisposable
{
    private const string FakeApiKey = "test-api-key";
    private const string FakeEndpoint = "https://api.x.ai/v1/chat/completions";

    private readonly Mock<ILogger<IXAIService>> _loggerMock;

    public XAIServiceTests()
    {
        _loggerMock = new Mock<ILogger<IXAIService>>();
        Environment.SetEnvironmentVariable("XAIGROK_API_KEY", FakeApiKey);
        Environment.SetEnvironmentVariable("XAIGROK_ENDPOINT", FakeEndpoint);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("XAIGROK_API_KEY", null);
        Environment.SetEnvironmentVariable("XAIGROK_ENDPOINT", null);
    }

    private static TrainingSession BuildSession() => new()
    {
        Id = 1,
        InstructorId = 1,
        TrainingDate = DateTime.UtcNow,
        Capacity = 10,
        Duration = 1.5,
        Status = SessionStatus.Active,
        TargetLevel = TargetLevel.Beginner,
        MartialArt = MartialArt.BrazilianJiuJitsu_GI
    };

    private static HttpClient BuildHttpClient(HttpStatusCode statusCode, string responseJson)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseJson);
        return new HttpClient(handler);
    }

    private static string BuildValidApiResponse(string contentText) =>
        JsonSerializer.Serialize(new XAILiveSearchApiResponse
        {
            Id = "test-id",
            Object = "chat.completion",
            Model = "grok-4-1-fast-reasoning",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message { Role = "assistant", Content = contentText },
                    FinishReason = "stop"
                }
            },
            Usage = new Usage { TotalTokens = 100 }
        });

    [Fact]
    public async Task Should_ReturnSearchResults_When_ValidTechniqueAndSessionProvided()
    {
        // Arrange
        const string expectedContent = """{"youtube_videos":[],"free_videos":[],"paid_resources":[]}""";
        var httpClient = BuildHttpClient(HttpStatusCode.OK, BuildValidApiResponse(expectedContent));
        var service = new XAIService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.SearchVideosAsync("Armbar", BuildSession());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedContent, result);
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_TechniqueNameIsEmpty()
    {
        // Arrange
        var httpClient = BuildHttpClient(HttpStatusCode.OK, "{}");
        var service = new XAIService(httpClient, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SearchVideosAsync(string.Empty, BuildSession()));
    }

    [Fact]
    public async Task Should_ThrowInvalidOperationException_When_ApiReturnsErrorStatusCode()
    {
        // Arrange
        var httpClient = BuildHttpClient(HttpStatusCode.Unauthorized, "Unauthorized");
        var service = new XAIService(httpClient, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SearchVideosAsync("Triangle Choke", BuildSession()));
    }

    [Fact]
    public async Task Should_ThrowInvalidOperationException_When_ResponseJsonIsInvalid()
    {
        // Arrange
        var httpClient = BuildHttpClient(HttpStatusCode.OK, "not-valid-json{{{{");
        var service = new XAIService(httpClient, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SearchVideosAsync("Kimura", BuildSession()));
    }

    // ---- Private helper: FakeHttpMessageHandler ----

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}


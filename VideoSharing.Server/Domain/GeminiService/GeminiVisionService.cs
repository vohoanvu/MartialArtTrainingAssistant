using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VideoSharing.Server.Domain.GeminiService
{
    public class GeminiVisionSettings
    {
        public string ApiEndpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    // Request sent to Gemini Vision API.
    public class GeminiRequest
    {
        public string VideoUrl { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
    }

    // Response returned from Gemini Vision API.
    public class GeminiAnalysisResult
    {
        public string AnalysisJson { get; set; } = string.Empty;
        // Add additional properties as needed
    }

    public interface IGeminiVisionService
    {
        Task<GeminiAnalysisResult> AnalyzeVideoAsync(string videoUrl);
    }

    public class GeminiVisionService : IGeminiVisionService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiVisionSettings _settings;

        public GeminiVisionService(HttpClient httpClient, IOptions<GeminiVisionSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<GeminiAnalysisResult> AnalyzeVideoAsync(string videoUrl)
        {
            var requestObj = new GeminiRequest
            {
                VideoUrl = videoUrl,
                Prompt = GetCustomPrompt()
            };

            var json = JsonSerializer.Serialize(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            // Optionally add API key header if needed
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("ApiKey");
                _httpClient.DefaultRequestHeaders.Add("ApiKey", _settings.ApiKey);
            }
            var response = await _httpClient.PostAsync(_settings.ApiEndpoint, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiAnalysisResult>();
            return result ?? new GeminiAnalysisResult();
        }

        private string GetCustomPrompt() => "Analyze the video for quality and performance.";
    }
}
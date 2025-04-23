using System.Text;
using System.Text.Json;

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
    public class GeminiVisionResult
    {
        public string AnalysisJson { get; set; } = string.Empty;
        // Add additional properties as needed
    }

    public interface IGeminiVisionService
    {
        Task<GeminiVisionResult> AnalyzeVideoAsync(string videoUrl);
    }

    public class GeminiVisionService : IGeminiVisionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint;
        private readonly string _apiKey;

        public GeminiVisionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiEndpoint = configuration["GeminiVision:ApiEndpoint"] 
                           ?? throw new ArgumentNullException("GeminiVision:ApiEndpoint");
            _apiKey = configuration["GeminiVision:ApiKey"] ?? string.Empty;
        }

        public async Task<GeminiVisionResult> AnalyzeVideoAsync(string videoUrl)
        {
            var requestObj = new GeminiRequest
            {
                VideoUrl = videoUrl,
                Prompt = GetCustomPrompt()
            };

            var json = JsonSerializer.Serialize(requestObj);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Optionally add API key header if needed
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("ApiKey");
                _httpClient.DefaultRequestHeaders.Add("ApiKey", _apiKey);
            }

            var response = await _httpClient.PostAsync(_apiEndpoint, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiVisionResult>();
            return result ?? new GeminiVisionResult();
        }

        private string GetCustomPrompt() => @"
            Analyze this martial arts sparring video. Provide a detailed description of the techniques used (e.g., strikes, grapples, submissions), evaluate their execution, and suggest specific improvements. 
            Include tailored drills to practice, formatted as a JSON object with fields: technique_identified, textual_description, strengths, areas_for_improvement, suggested_drills.
            Structure the output as a JSON object for easy parsing using the following format as example:
            {
                ""techniques_identified"": [
                    {
                        ""name"": ""Rear Naked Choke"",
                        ""description"": ""A chokehold applied from behind the opponent."",
                        ""timestamp"": ""00:01:23""
                    },
                    {
                        ""name"": ""Armbar"",
                        ""description"": ""A joint lock that hyperextends the elbow."",
                        ""timestamp"": ""00:02:45""
                    }
                ],
                ""textual_description"": ""User applies a rear naked choke but struggles with leg control..."",
                ""strengths"": ""Good hand positioning and control"",
                ""areas_for_improvement"": ""Need to secure the legs to prevent escape"",
                ""suggested_drills"": [
                    {
                        ""name"": ""Leg Hook Drill"",
                        ""description"": ""Practice securing opponent's legs while maintaining choke grip."",
                        ""demo_video_link"": ""https://example.com/leg-hook-drill""
                    }
                ]
            }
        ";
    }
}
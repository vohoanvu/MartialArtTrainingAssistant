using System.Text.Json;
using System.Text.Json.Serialization;
using SharedEntities;
using SharedEntities.Models;

namespace VideoSharing.Server.Domain.AIServices
{
    /// <inheritdoc/>
    public interface IXAIService
    {
        /// <inheritdoc/>
        Task<string> SearchVideosAsync(string techniqueName, TrainingSession trainingSession);
    }
    /// <inheritdoc/>
    public class XAIService : IXAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;
        private readonly ILogger<IXAIService> _logger;
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        /// <inheritdoc/>
        public XAIService(HttpClient httpClient, ILogger<IXAIService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiKey = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.XAIGrokApiKey);
            _apiEndpoint = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.XAIGrokEndpoint);

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("xAI Live Search API key is not configured.");
                throw new InvalidOperationException("xAI Live Search API key is not configured.");
            }

            if (string.IsNullOrEmpty(_apiEndpoint))
            {
                _logger.LogError("xAI Live Search API endpoint is not configured.");
                throw new InvalidOperationException("xAI Live Search API endpoint is not configured.");
            }

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _logger.LogInformation("Initialized xAI Live Search client with endpoint: {Endpoint}", _apiEndpoint);
        }

        /// <inheritdoc/>
        public async Task<string> SearchVideosAsync(string techniqueName, TrainingSession trainingSession)
        {
            if (string.IsNullOrEmpty(techniqueName))
            {
                _logger.LogWarning("Technique name is empty or null.");
                throw new ArgumentException("Technique name cannot be empty or null.", nameof(techniqueName));
            }

            var requestPayload = BuildSearchRequest(techniqueName, trainingSession);

            try
            {
                _logger.LogInformation("Sending video search request to xAI Live Search API: {Endpoint}", _apiEndpoint);

                var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, requestPayload, _jsonSerializerOptions);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonSerializer.Deserialize<XAILiveSearchApiResponse>(responseContent, _jsonSerializerOptions)
                    ?? throw new InvalidOperationException("No valid JSON response received from xAI Live Search API.");

                _logger.LogInformation("Received valid response for search query: {Query}", requestPayload.Messages[0].Content);

                return searchResponse.Choices[0].Message?.Content ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "xAI Live Search API call failed for query: {Query}, status code: {StatusCode}", requestPayload.Messages[0].Content, ex.StatusCode);
                throw new InvalidOperationException($"xAI Live Search API call failed: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize xAI Live Search API response for query: {Query}", requestPayload.Messages[0].Content);
                throw new InvalidOperationException($"Failed to parse xAI Live Search API response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during xAI Live Search API call for query: {Query}", requestPayload.Messages[0].Content);
                throw;
            }
        }

        private static XAILiveSearchRequest BuildSearchRequest(string techniqueName, TrainingSession trainingSession)
        {
            var query = $"{trainingSession.MartialArt} {techniqueName} or closely related techniques";
            return new XAILiveSearchRequest
            {
                Model = "grok-3-latest",
                Messages =
                [
                    new() {
                        Role = "user",
                        Content = $@"
            Find instructional videos for {query} on YouTube, prioritizing public videos from reputable BJJ channels such as Chewjitsu, BJJ Fanatics, and Grapplearts. For each video, include the title, a brief description (2-3 sentences), the video ID, the publication date (to prioritize videos from the last 5 years), and a working embed link in the format 'https://www.youtube.com/embed/video_id'. Ensure the embed link is live, functional, and playable directly in an app by verifying its availability.

            Also, search for instructional content on other websites and categorize them into:
            - Free, publicly available videos or articles: Provide the title and a direct, accessible link. Ensure the link is valid, does not require login or subscription, and can be opened in a browser tab. Include all valid links found to account for potential broken links.
            - Paid, premium instructional resources: Provide the title, a brief description (2-3 sentences), a direct web link to the resource, and explain its relevance to the technique in one sentence. Do not include long advertisements.

            Structure the search results in a well-formatted JSON object with the following sections:
            - 'youtube_videos': An array of objects, each with 'title', 'description', 'video_id', 'publication_date', and 'embed_link'. Include all valid YouTube videos found, up to the maximum search results.
            - 'free_videos': An array of objects, each with 'title' and 'link'. Include all valid free resources found, up to the maximum search results.
            - 'paid_resources': An array of objects, each with 'title', 'description', 'web_link', and 'relevance'. Include all valid paid resources found, up to the maximum search results.

            Ensure the resources are suitable for a {trainingSession.TargetLevel} level class with {trainingSession.Capacity} students, lasting {trainingSession.Duration} hours. The content should assist BJJ instructors in teaching techniques effectively and help students understand them better. Return all valid results found within the maximum search results limit.

            **Important**: Return only the JSON object as specified above, with no additional text, notes, comments, or metadata outside the JSON structure. Do not include usage notes, explanations, or any other prose.
            "
                    }
                ],
                SearchParameters = new
                {
                    mode = "on",
                    sources = new[] { new Dictionary<string, object> {
                        { "type", "web" }
                    } },
                    return_citations = true,
                    max_search_results = 20
                }
            };
        }

        // private List<VideoSearchResult> ProcessSearchResults(XAILiveSearchApiResponse response)
        // {
        //     var results = new List<VideoSearchResult>();

        //     if (response.Choices == null || response.Choices.Count == 0 || response.Choices[0].Message?.Content == null)
        //     {
        //         _logger.LogWarning("No valid choices or content found in xAI Live Search API response.");
        //         return results;
        //     }

        //     try
        //     {
        //         var content = response.Choices[0].Message.Content;
        //         // Regex to match video entries in markdown format, e.g., "1. **Title** by Author"
        //         var videoEntryRegex = new System.Text.RegularExpressions.Regex(
        //             @"(?:\d+\.\s*\*\*|(?:.*?-\s*))\*\*([^(*]+)(?:\([^)]+\))?\*\*(?:\s*-\s*\*\*[^:]+:\*\*\s*([^)]+))?\n\s*-\s*\*\*[^:]+:\*\*([^\n]*)\n\s*[^\n]*\n\s*-\s*([^\n]+)",
        //             System.Text.RegularExpressions.RegexOptions.Multiline
        //         );

        //         var matches = videoEntryRegex.Matches(content);
        //         foreach (var match in matches)
        //         {
        //             if (match.Success && match.Groups.Count > 4)
        //             {
        //                 var title = match.Groups[1].Value.Trim();
        //                 var channel = match.Groups[2].Value.Trim();
        //                 var link = match.Groups[3].Value.Trim();
        //                 var description = match.Groups[4].Value.Trim();

        //                 // Skip entries with missing title or link
        //                 if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(link))
        //                 {
        //                     continue;
        //                 }

        //                 results.Add(new VideoSearchResult
        //                 {
        //                     Title = title,
        //                     Url = link.StartsWith("http") ? link : $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(title)}",
        //                     Description = string.IsNullOrEmpty(description) ? channel : $"{channel}: {description}",
        //                     Thumbnail = string.Empty // Thumbnails not provided in text; could fetch via YouTube API if needed
        //                 });
        //             }
        //         }

        //         if (!results.Any())
        //         {
        //             _logger.LogWarning("No video results extracted from xAI Live Search API response content.");
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Failed to parse video search results from xAI Live Search API response content.");
        //         return results;
        //     }

        //     return results;
        // }

    }
    /// <inheritdoc/>
    public class XAILiveSearchRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = new List<Message>();

        [JsonPropertyName("search_parameters")]
        public object SearchParameters { get; set; } = null!;
    }
   
    public class XAILiveSearchApiResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = new List<Choice>();

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; } = new Usage();

        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; } = string.Empty;

        [JsonPropertyName("citations")]
        public List<string> Citations { get; set; } = [];
    }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message Message { get; set; } = new Message();

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = string.Empty;
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("refusal")]
        public string? Refusal { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("prompt_tokens_details")]
        public PromptTokensDetails PromptTokensDetails { get; set; } = new PromptTokensDetails();

        [JsonPropertyName("completion_tokens_details")]
        public CompletionTokensDetails CompletionTokensDetails { get; set; } = new CompletionTokensDetails();

        [JsonPropertyName("num_sources_used")]
        public int NumSourcesUsed { get; set; }
    }

    public class PromptTokensDetails
    {
        [JsonPropertyName("text_tokens")]
        public int TextTokens { get; set; }

        [JsonPropertyName("audio_tokens")]
        public int AudioTokens { get; set; }

        [JsonPropertyName("image_tokens")]
        public int ImageTokens { get; set; }

        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; }
    }

    public class CompletionTokensDetails
    {
        [JsonPropertyName("reasoning_tokens")]
        public int ReasoningTokens { get; set; }

        [JsonPropertyName("audio_tokens")]
        public int AudioTokens { get; set; }

        [JsonPropertyName("accepted_prediction_tokens")]
        public int AcceptedPredictionTokens { get; set; }

        [JsonPropertyName("rejected_prediction_tokens")]
        public int RejectedPredictionTokens { get; set; }
    }

    public class VideoSearchResult
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Thumbnail { get; set; } = string.Empty;
    }
}
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using SharedEntities;

namespace VideoAnalysis.Server.Domain.AIServices
{
    /// <summary>
    /// Thin reusable client for the Vertex AI REST surface (<c>generateContent</c> and
    /// <c>cachedContents</c>), authenticating with ADC/OAuth2 Bearer tokens. Extracted from the
    /// token/HTTP plumbing in <see cref="GeminiService.GeminiVisionService"/> so the agentic pipeline
    /// can reuse it. Unlike the single-shot service, the host is resolved per call from the supplied
    /// <c>location</c> so the pipeline can target a REGIONAL endpoint (required for context caching).
    /// </summary>
    public interface IVertexRestClient
    {
        /// <summary>POST <c>:generateContent</c> for the given model/location, returns the raw response JSON.</summary>
        Task<JsonElement> GenerateContentAsync(string modelId, string location, object body, CancellationToken ct);

        /// <summary>POST <c>cachedContents</c>; <paramref name="cacheBody"/> must include the full model path. Returns the cache resource name.</summary>
        Task<string> CreateCachedContentAsync(object cacheBody, string location, CancellationToken ct);

        /// <summary>DELETE a context cache by its full resource name (<c>projects/.../cachedContents/id</c>).</summary>
        Task DeleteCachedContentAsync(string cacheName, string location, CancellationToken ct);

        /// <summary>Full model resource path used in cache bodies: <c>projects/{p}/locations/{loc}/publishers/google/models/{id}</c>.</summary>
        string ModelResourcePath(string modelId, string location);

        /// <summary>Extracts the last non-empty text part (thinking models emit thinking parts first).</summary>
        string ExtractText(JsonElement response);
    }

    /// <inheritdoc cref="IVertexRestClient"/>
    public class VertexRestClient : IVertexRestClient
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleCredential _credential;
        private readonly string _projectId;
        private readonly ILogger<VertexRestClient> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        public VertexRestClient(HttpClient httpClient, ILogger<VertexRestClient> logger)
        {
            _httpClient = httpClient;
            // 4 sequential pro-model passes per pipeline run — give each call generous headroom.
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
            _logger = logger;

            // Trim trailing CR/LF that the VM .env can carry on env values (CRLF / Secret Manager),
            // which would otherwise corrupt the Vertex resource path (projects/{id}\n/locations/...).
            _projectId = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudProjectId).Trim();

            // Same auth resolution as GeminiVisionService: SA key file if present, else ADC (keyless).
            var keyPath = Environment.GetEnvironmentVariable("GoogleCloud__ServiceAccountKeyPath");
            if (!string.IsNullOrEmpty(keyPath) && File.Exists(keyPath))
            {
                _credential = GoogleCredential.FromFile(keyPath)
                    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                _logger.LogInformation("VertexRestClient: using service account key from {KeyPath}", keyPath);
            }
            else
            {
                _credential = GoogleCredential.GetApplicationDefault()
                    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                _logger.LogInformation("VertexRestClient: using Application Default Credentials");
            }
        }

        // ── URL builders ─────────────────────────────────────────────────────

        private static string HostFor(string location) =>
            string.Equals(location, "global", StringComparison.OrdinalIgnoreCase)
                ? "https://aiplatform.googleapis.com"
                : $"https://{location}-aiplatform.googleapis.com";

        public string ModelResourcePath(string modelId, string location) =>
            $"projects/{_projectId}/locations/{location}/publishers/google/models/{modelId}";

        private string GenerateContentUrl(string modelId, string location) =>
            $"{HostFor(location)}/v1/{ModelResourcePath(modelId, location)}:generateContent";

        private string CachedContentsUrl(string location) =>
            $"{HostFor(location)}/v1/projects/{_projectId}/locations/{location}/cachedContents";

        // ── Public API ───────────────────────────────────────────────────────

        public async Task<JsonElement> GenerateContentAsync(string modelId, string location, object body, CancellationToken ct)
        {
            return await PostJsonAsync(GenerateContentUrl(modelId, location), body, ct);
        }

        public async Task<string> CreateCachedContentAsync(object cacheBody, string location, CancellationToken ct)
        {
            var response = await PostJsonAsync(CachedContentsUrl(location), cacheBody, ct);
            if (!response.TryGetProperty("name", out var nameProp) || nameProp.GetString() is not { Length: > 0 } cacheName)
            {
                throw new InvalidOperationException("cachedContents response did not contain a resource name.");
            }
            _logger.LogInformation("Created Vertex context cache {CacheName}", cacheName);
            return cacheName;
        }

        public async Task DeleteCachedContentAsync(string cacheName, string location, CancellationToken ct)
        {
            var token = await GetAccessTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{HostFor(location)}/v1/{cacheName}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Cache cleanup is best-effort — log but do not throw (TTL expiry will reclaim it).
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to delete context cache {CacheName}. Status {Status}: {Body}",
                    cacheName, (int)response.StatusCode, errorBody[..Math.Min(300, errorBody.Length)]);
                return;
            }
            _logger.LogInformation("Deleted Vertex context cache {CacheName}", cacheName);
        }

        public string ExtractText(JsonElement response)
        {
            var parts = response
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            for (int i = parts.GetArrayLength() - 1; i >= 0; i--)
            {
                if (parts[i].TryGetProperty("text", out var textProp))
                {
                    var text = textProp.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            throw new InvalidOperationException("Response contained no text in candidates[0].content.parts");
        }

        public static object[] AllSafetyOff() =>
        [
            new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "OFF" },
            new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "OFF" },
            new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "OFF" },
            new { category = "HARM_CATEGORY_HARASSMENT", threshold = "OFF" },
        ];

        // ── HTTP helper ──────────────────────────────────────────────────────

        private async Task<string> GetAccessTokenAsync()
        {
            var tokenAccess = _credential as ITokenAccess;
            return await tokenAccess!.GetAccessTokenForRequestAsync();
        }

        private async Task<JsonElement> PostJsonAsync(string url, object body, CancellationToken ct)
        {
            var token = await GetAccessTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Vertex AI API error. Status: {StatusCode}, Body: {Body}",
                    (int)response.StatusCode, errorBody[..Math.Min(500, errorBody.Length)]);
                throw new HttpRequestException(
                    $"Vertex AI request failed with {(int)response.StatusCode}: {errorBody[..Math.Min(500, errorBody.Length)]}",
                    null,
                    response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct);
        }
    }
}

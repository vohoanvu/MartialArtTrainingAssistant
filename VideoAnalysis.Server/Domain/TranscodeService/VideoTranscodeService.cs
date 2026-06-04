using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using SharedEntities;
using VideoAnalysis.Server.Configuration;

namespace VideoAnalysis.Server.Domain.TranscodeService
{
    /// <summary>Terminal/in-flight state of a Transcoder job, distilled from the REST response.</summary>
    public record TranscodeJobState(string State, string? Error)
    {
        public bool IsSucceeded => string.Equals(State, "SUCCEEDED", StringComparison.OrdinalIgnoreCase);
        public bool IsFailed => string.Equals(State, "FAILED", StringComparison.OrdinalIgnoreCase);
        public bool IsTerminal => IsSucceeded || IsFailed;
    }

    /// <summary>
    /// Thin REST client for the GCP Transcoder API (<c>transcoder.googleapis.com</c>), authenticating
    /// with ADC/OAuth2 Bearer tokens — the same keyless pattern as <see cref="AIServices.VertexRestClient"/>.
    /// Creates an H.264/AAC MP4 "playback" rendition of an uploaded tape so HEVC tapes (which browsers
    /// can't render) become viewable in-browser. Analysis still ingests the original file directly.
    /// </summary>
    public interface IVideoTranscodeService
    {
        /// <summary>
        /// Creates a transcode job. <paramref name="inputGcsUri"/> is the source (<c>gs://…</c>);
        /// <paramref name="outputUriPrefix"/> is a <c>gs://…/</c> folder — the muxed output lands at
        /// <c>{outputUriPrefix}playback.mp4</c>. Returns the job resource name
        /// (<c>projects/{p}/locations/{loc}/jobs/{id}</c>).
        /// </summary>
        Task<string> CreateJobAsync(string inputGcsUri, string outputUriPrefix, CancellationToken ct);

        /// <summary>Polls a job by its resource name and returns its state (+ error message when FAILED).</summary>
        Task<TranscodeJobState> GetJobAsync(string jobName, CancellationToken ct);
    }

    /// <inheritdoc cref="IVideoTranscodeService"/>
    public class VideoTranscodeService : IVideoTranscodeService
    {
        private const string ApiBase = "https://transcoder.googleapis.com/v1";

        private readonly HttpClient _httpClient;
        private readonly GoogleCredential _credential;
        private readonly TranscoderOptions _options;
        private readonly string _projectId;
        private readonly ILogger<VideoTranscodeService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public VideoTranscodeService(HttpClient httpClient, IOptions<TranscoderOptions> options, ILogger<VideoTranscodeService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            // Trim trailing CR/LF the VM .env can carry on values (CRLF / Secret Manager), which would
            // otherwise corrupt the job resource path (projects/{id}\n/locations/...).
            _projectId = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudProjectId).Trim();

            // Same auth resolution as VertexRestClient/GoogleCloudStorageService: SA key file if present,
            // else ADC (the VM's attached service account — keyless, MyCoach org policy compatible).
            var keyPath = Environment.GetEnvironmentVariable("GoogleCloud__ServiceAccountKeyPath");
            if (!string.IsNullOrEmpty(keyPath) && File.Exists(keyPath))
            {
                _credential = GoogleCredential.FromFile(keyPath)
                    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                _logger.LogInformation("VideoTranscodeService: using service account key from {KeyPath}", keyPath);
            }
            else
            {
                _credential = GoogleCredential.GetApplicationDefault()
                    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                _logger.LogInformation("VideoTranscodeService: using Application Default Credentials");
            }
        }

        private string Location => _options.Location.Trim();

        private string JobsUrl => $"{ApiBase}/projects/{_projectId}/locations/{Location}/jobs";

        public async Task<string> CreateJobAsync(string inputGcsUri, string outputUriPrefix, CancellationToken ct)
        {
            // One H.264 video stream (height-only → preserve aspect) + one AAC audio stream, muxed to a
            // single mp4 keyed "playback" → output file is "{outputUriPrefix}playback.mp4".
            var body = new
            {
                inputUri = inputGcsUri,
                outputUri = outputUriPrefix,
                config = new
                {
                    elementaryStreams = new object[]
                    {
                        new
                        {
                            key = "video-stream0",
                            videoStream = new
                            {
                                h264 = new
                                {
                                    heightPixels = _options.HeightPixels,
                                    frameRate = _options.FrameRate,
                                    bitrateBps = _options.BitrateBps,
                                },
                            },
                        },
                        new
                        {
                            key = "audio-stream0",
                            audioStream = new
                            {
                                codec = "aac",
                                bitrateBps = _options.AudioBitrateBps,
                            },
                        },
                    },
                    muxStreams = new object[]
                    {
                        new
                        {
                            key = "playback",
                            container = "mp4",
                            elementaryStreams = new[] { "video-stream0", "audio-stream0" },
                        },
                    },
                },
            };

            var response = await PostJsonAsync(JobsUrl, body, ct);
            if (!response.TryGetProperty("name", out var nameProp) || nameProp.GetString() is not { Length: > 0 } jobName)
            {
                throw new InvalidOperationException("Transcoder createJob response did not contain a job name.");
            }
            _logger.LogInformation("Created transcode job {JobName} ({Input} → {Output})", jobName, inputGcsUri, outputUriPrefix);
            return jobName;
        }

        public async Task<TranscodeJobState> GetJobAsync(string jobName, CancellationToken ct)
        {
            var token = await GetAccessTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/{jobName}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Transcoder getJob failed with {(int)response.StatusCode}: {Truncate(errorBody, 500)}",
                    null, response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var job = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct);

            var state = job.TryGetProperty("state", out var stateProp) ? stateProp.GetString() ?? "UNKNOWN" : "UNKNOWN";
            string? error = null;
            if (job.TryGetProperty("error", out var errProp) && errProp.TryGetProperty("message", out var msgProp))
            {
                error = msgProp.GetString();
            }
            return new TranscodeJobState(state, error);
        }

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
                _logger.LogError("Transcoder API error. Status: {StatusCode}, Body: {Body}",
                    (int)response.StatusCode, Truncate(errorBody, 500));
                throw new HttpRequestException(
                    $"Transcoder request failed with {(int)response.StatusCode}: {Truncate(errorBody, 500)}",
                    null, response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct);
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
    }
}

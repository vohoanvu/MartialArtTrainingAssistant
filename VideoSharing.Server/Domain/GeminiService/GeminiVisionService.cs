using Google.Cloud.AIPlatform.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.StaticFiles;
using VideoSharing.Server.Domain.GoogleCloudStorageService;
using VideoSharing.Server.Models.Dtos;
using SharedEntities.Models;
using SharedEntities.Data;

namespace VideoSharing.Server.Domain.GeminiService
{
    public interface IGeminiVisionService
    {
        Task<GeminiVisionResponse> AnalyzeVideoAsync(string videoInput);

        Task<AiFeedback> AnalyzeVideoSegment(int videoId, double timestamp);
    }

    public class GeminiVisionService : IGeminiVisionService
    {
        private readonly PredictionServiceClient _predictionClient;
        private readonly string _projectId;
        private readonly string _location;
        private readonly string _model;
        private readonly string _customPrompt;
        private readonly IGoogleCloudStorageService _storageService;
        private readonly ILogger<GeminiVisionService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public GeminiVisionService(IConfiguration configuration, IGoogleCloudStorageService storageService, 
        ILogger<GeminiVisionService> logger, IServiceProvider serviceProvider)
        {
            _projectId = configuration["GoogleCloud:ProjectId"] ?? throw new ArgumentNullException("GoogleCloud:ProjectId");
            _location = configuration["GeminiVision:Location"] ?? "us-central1";
            _model = configuration["GeminiVision:Model"] ?? "gemini-pro-1.5";
            _customPrompt = @"
            Analyze what is happening in this martial arts sparring video. Provide a detailed description of the techniques used (e.g., strikes, grapples, submissions), evaluate their execution, and suggest specific improvements. 
            Include tailored drills to practice, formatted as a JSON object with fields: technique_identified, textual_description, strengths, areas_for_improvement, suggested_drills.
            Structure the output as a JSON object using the following format as example:
            {
                ""description"": ""Both fighters start from standing positions and progress toward full-guard. Fighter 1 applies a rear naked choke but struggles with leg control..."",
                ""techniques_identified"": [
                        {
                            ""name"": ""Rear Naked Choke"",
                            ""description"": ""A chokehold applied from behind the opponent."",
                            ""timestamp"": ""00:01:23"",
                            ""fighter_identifier"" : ""Fighter in the black uniform""
                        },
                        {
                            ""name"": ""Armbar"",
                            ""description"": ""A joint lock that hyperextends the elbow."",
                            ""timestamp"": ""00:02:45"",
                            ""fighter_identifier"" : ""Blue belt fighter""
                        }
                ],
                ""strengths"": [
                    ""Fighter 1"" : ""Good hand positioning and control"",
                    ""Fighter 2"" : ""Great control and good movement""
                ],
                ""areas_for_improvement"": [
                    ""Blue belt"" : ""Need to secure the legs to prevent escape"",
                    ""White belt"" : ""should have better maintaining the rear naked choke grip""
                ],
                ""suggested_drills"": [
                    {
                        ""name"": ""Leg Hook Drill"",
                        ""description"": ""Practice securing opponent's legs while maintaining choke grip."",
                        ""focus"": ""Position Stabilization for White Belt, Escapes for Blue Belt"",
                        ""duration"" : ""2 minutes""
                    }
                ]
            }";
            //configuration["GeminiVision:VideoAnalysisPrompt"] ?? throw new ArgumentNullException("GeminiVision:VideoAnalysisPrompt");
            _storageService = storageService;
            _logger = logger;
            _serviceProvider = serviceProvider;

            // Initialize Vertex AI's PredictionServiceClient with service account credentials
            try
            {
                var keyPath = configuration["GoogleCloud:ServiceAccountKeyPath"] 
                    ?? throw new ArgumentNullException("GoogleCloud:ServiceAccountKeyPath");
                if (!File.Exists(keyPath))
                    throw new FileNotFoundException($"Service account key file not found at {keyPath}");

                var credential = GoogleCredential.FromFile(keyPath)
                    .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                if (credential.UnderlyingCredential is ServiceAccountCredential serviceAccountCredential)
                {
                    _logger.LogInformation("Using service account: {ServiceAccountEmail}", serviceAccountCredential.Id);
                }
                else
                {
                    _logger.LogInformation("Using service account credentials.");
                }

                _predictionClient = new PredictionServiceClientBuilder
                {
                    Credential = credential,
                    Endpoint = $"{_location}-aiplatform.googleapis.com"
                }.Build();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Vertex AI client.");
                throw new InvalidOperationException("Failed to initialize Vertex AI client.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<GeminiVisionResponse> AnalyzeVideoAsync(string videoInput)
        {
            string fileUri;
            string mimeType;

            // Handle video input types
            if (videoInput.StartsWith("http://") || videoInput.StartsWith("https://") || videoInput.StartsWith("gs://"))
            {
                // if (videoInput.Contains("storage.googleapis.com") && videoInput.Contains("X-Goog-Signature"))
                //     throw new ArgumentException("Signed URLs are not supported. Use a gs:// URI or public URL.", nameof(videoInput));
                fileUri = videoInput;
                mimeType = DetermineMimeType(videoInput);
            }
            else
            {
                // locally stored video
                mimeType = DetermineMimeType(videoInput);
                using var fileStream = new FileStream(videoInput, FileMode.Open, FileAccess.Read);
                fileUri = await _storageService.UploadFileAsync(fileStream, Path.GetFileName(videoInput), mimeType);
            }

            string prompt = GetCustomPrompt();

            // Construct the GenerateContentRequest
            var request = new GenerateContentRequest
            {
                Model = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{_model}",
                Contents =
                {
                    new Content
                    {
                        Role = "user",
                        Parts =
                        {
                            new Part
                            {
                                FileData = new FileData
                                {
                                    MimeType = mimeType,
                                    FileUri = fileUri
                                }
                            },
                            new Part
                            {
                                Text = prompt
                            }
                        }
                    }
                },
                SafetySettings =
                {
                    new SafetySetting
                    {
                        Category = HarmCategory.SexuallyExplicit,
                        Threshold = SafetySetting.Types.HarmBlockThreshold.Off
                    },
                    new SafetySetting
                    {
                        Category = HarmCategory.HateSpeech,
                        Threshold = SafetySetting.Types.HarmBlockThreshold.Off
                    },
                    new SafetySetting
                    {
                        Category = HarmCategory.DangerousContent,
                        Threshold = SafetySetting.Types.HarmBlockThreshold.Off
                    },
                    new SafetySetting
                    {
                        Category = HarmCategory.Harassment,
                        Threshold = SafetySetting.Types.HarmBlockThreshold.Off
                    }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.4f,
                    TopP = 1.0f,
                    MaxOutputTokens = 65535,
                    ResponseMimeType = "application/json",
                }
            };

            // Call the Vertex AI API
            try
            {
                _logger.LogInformation("Sending GenerateContent request for video: {FileUri} to model {Model} in project {ProjectId}", 
                    fileUri, _model, _projectId);
                GenerateContentResponse response = await _predictionClient.GenerateContentAsync(request);
                _logger.LogDebug("Raw API response: {Response}", response.ToString());
                string resultJson = response.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault(p => p.Text != null)?.Text
                    ?? throw new InvalidOperationException("No valid JSON response received from the API.");
                _logger.LogInformation("Received valid response for video: {FileUri}", fileUri);
                return new GeminiVisionResponse { AnalysisJson = resultJson };
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Vertex AI API call failed for video: {FileUri}, project: {ProjectId}, model: {Model}, HTTP status: {StatusCode}, error details: {Details}", 
                    fileUri, _projectId, _model, ex.HttpStatusCode, ex.Error?.ToString());
                throw new InvalidOperationException($"Vertex AI API call failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Vertex AI API call for video: {FileUri}", fileUri);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<AiFeedback> AnalyzeVideoSegment(int videoId, double timestamp)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();

            var video = await dbContext.UploadedVideos.FindAsync(videoId);
            string analysis = await AnalyzeSegmentsWithGemini(video!.FilePath, timestamp); //TODO: Implement this method to call the Vertex AI Gemini API for segment analysis.
            var aiFeedback = new AiFeedback
            {
                VideoId = videoId,
                Timestamp = timestamp, 
                FeedbackType = string.Empty,
                AnalysisJson = analysis,
            };
            dbContext.AiFeedbacks.Add(aiFeedback);
            await dbContext.SaveChangesAsync();
            return aiFeedback;
        }

        private static async Task<string> AnalyzeSegmentsWithGemini(string videoPath, double timestamp)
        {
            await Task.Run(() => Thread.Sleep(1000));
            throw new NotImplementedException("This method should be implemented to call the Vertex AI Gemini API for segment analysis.");
        }

        private string DetermineMimeType(string pathOrUri)
        {
            string extension;
            if (pathOrUri.StartsWith("http://") || pathOrUri.StartsWith("https://") || pathOrUri.StartsWith("gs://"))
            {
                var uri = new Uri(pathOrUri);
                extension = Path.GetExtension(uri.AbsolutePath);
            }
            else
            {
                extension = Path.GetExtension(pathOrUri);
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(extension, out var contentType))
            {
                contentType = "video/mp4"; // Default to mp4 if unknown
            }
            return contentType;
        }

        private string GetCustomPrompt() => _customPrompt;
    }
}
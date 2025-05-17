using Google.Cloud.AIPlatform.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.StaticFiles;
using VideoSharing.Server.Domain.GoogleCloudStorageService;
using VideoSharing.Server.Models.Dtos;
using SharedEntities.Models;
using SharedEntities;

namespace VideoSharing.Server.Domain.GeminiService
{
    public interface IGeminiVisionService
    {
        Task<GeminiVisionResponse> AnalyzeVideoAsync(string videoInput, string martialArt, string studentIdentifier, string videoDescription, string skillLevel);

        Task<GeminiChatResponse> SuggestClassCurriculum(List<string> weaknesses, List<Fighter>? students, TrainingSession classSession);
    }

    public class GeminiVisionService : IGeminiVisionService
    {
        private readonly PredictionServiceClient _predictionClient;
        private readonly string _projectId;
        private readonly string _location;
        private readonly string _model;
        private readonly string _visionPrompt = @"
        You are an expert [MartialArt] instructor. You have been given video of your student with the description as '[VideoDescription]'.
        Analyze the performance of the student, identified as [StudentIdentifier], in this [MartialArt] video. The student is at the [SkillLevel] level and is training for [TrainingGoal]. Provide a detailed analysis of the student's techniques, execution, strengths, weaknesses, and suggest specific drills for practice. Format the output as a JSON object with the following structure:
        {
            ""overall_description"": ""A detailed description of the student's overall performance in the sparring session."",
            ""techniques_identified"": [
                {
                ""technique_name"": ""Name of the technique (e.g., 'Armbar')"",
                ""description"": ""Description of how the technique was executed."",
                ""start_timestamp"": ""Timestamp in the video where the technique occurs (e.g., '00:01:23')"",
                ""end_timestamp"": ""Timestamp in the video where the technique ends (e.g., '00:01:30')"",
                ""technique_type"": ""Select from: ['Takedown', 'Submission', 'Sweep', 'Pass', 'Escape', 'Transition', 'Control', 'Defense']"",
                ""positional_scenario"": ""Select from: ['Standing', 'Guard', 'Half Guard', 'Side Control', 'Mount', 'Back Control', 'Knee on Belly', 'Turtle']""
                }
            ],
            ""strengths"": [
                {
                ""description"": ""Description of a strength observed in the student's performance."",
                ""related_technique"": ""Optional: Name of the technique related to this strength.""
                }
            ],
            ""areas_for_improvement"": [
                {
                ""description"": ""Description of an area where the student can improve."",
                ""weakness_category"": ""Select from: ['Takedown Defense', 'Guard Retention', 'Submission Escapes', 'Posture Control', 'Grip Strength', 'Timing', 'Stamina']""
                ""related_technique"": ""Optional: Name of the technique related to this area.""
                }
            ],
            ""suggested_drills"": [
                {
                ""name"": ""Name of the drill (e.g., 'Armbar Repetition Drill')"",
                ""description"": ""Detailed description of the drill."",
                ""focus"": ""Focus area of the drill (e.g., 'Technique Execution', 'Timing')"",
                ""duration"": ""Recommended duration for the drill (e.g., '5 minutes')"",
                ""related_technique"": ""Name of the technique the drill is intended to improve.""
                }
            ]
        }

        Ensure that the techniques identified are categorized by their technique type and linked to the appropriate positional scenarios as defined. Tailor the feedback and suggested drills to the student's [SkillLevel] and [TrainingGoal], ensuring they are actionable and relevant to their current abilities and objectives.
        ";

        private readonly IGoogleCloudStorageService _storageService;
        private readonly ILogger<GeminiVisionService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public GeminiVisionService(IGoogleCloudStorageService storageService,
        ILogger<GeminiVisionService> logger, IServiceProvider serviceProvider)
        {
            _projectId = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudProjectId);
            _location = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GeminiVisionLocation);
            _model = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GeminiVisionModel);

            _storageService = storageService;
            _logger = logger;
            _serviceProvider = serviceProvider;

            // Initialize Vertex AI's PredictionServiceClient with service account credentials
            try
            {
                var keyPath = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudServiceAccountKeyPath);
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
        public async Task<GeminiVisionResponse> AnalyzeVideoAsync(string videoInput,
        string martialArt,
        string studentIdentifier,
        string videoDescription,
        string skillLevel)
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

            // Replace placeholders in the prompt
            string prompt = _visionPrompt
                .Replace("[MartialArt]", martialArt)
                .Replace("[StudentIdentifier]", studentIdentifier)
                .Replace("[TrainingGoal]", "both self-defense and competition")
                .Replace("[SkillLevel]", skillLevel)
                .Replace("[VideoDescription]", videoDescription);

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
        public async Task<GeminiChatResponse> SuggestClassCurriculum(List<string> weaknesses, List<Fighter>? students, TrainingSession classSession)
        {
            var curriculumPrompt = BuildCurriculumPrompt(weaknesses, students, classSession);

            // Construct the GenerateContentRequest for chat (text-only)
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
                                Text = curriculumPrompt
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
                    MaxOutputTokens = 4096,
                    ResponseMimeType = "application/json",
                }
            };

            try
            {
                _logger.LogInformation("Sending GenerateContent request for class curriculum to model {Model} in project {ProjectId}", _model, _projectId);
                _logger.LogInformation("Current Prompt: {Prompt}", curriculumPrompt);
                GenerateContentResponse response = await _predictionClient.GenerateContentAsync(request);
                string resultJson = response.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault(p => p.Text != null)?.Text
                    ?? throw new InvalidOperationException("No valid JSON response received from the API.");
                _logger.LogInformation("Received valid curriculum response from Vertex AI.");
                return new GeminiChatResponse { CurriculumJson = resultJson };
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Vertex AI API call failed for curriculum generation: project: {ProjectId}, model: {Model}, HTTP status: {StatusCode}, error details: {Details}",
                    _projectId, _model, ex.HttpStatusCode, ex.Error?.ToString());
                throw new InvalidOperationException($"Vertex AI API call failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Vertex AI API call for curriculum generation.");
                throw;
            }
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

        private static string BuildCurriculumPrompt(List<string> weaknesses, List<Fighter>? students, TrainingSession classSession)
        {
            // Format student details, ensuring null safety
            var studentDetails = students?.Select(s =>
                $"- Student: Belt Rank - {s.BelkRank}, Height - {s.Height:F1} ft, Weight - {s.Weight} lbs, Training Experience - {s.Experience}"
            ).Aggregate((a, b) => $"{a}\n{b}") ?? "No student details provided.";

            var weaknessesList = (weaknesses != null && weaknesses.Count > 0)
                ? weaknesses
                : ["Guard Retention", "Submission Escapes", "Timing"];

            return $@"You are an expert {classSession.MartialArt.ToString()} instructor. Design a {classSession.Duration}-hours class session curriculum based on the following:
            - Most common weakness among the students: [{string.Join(", ", weaknessesList)}]
            - Students:
                {studentDetails}

            Create a curriculum that includes:
            - A 10-minute warm-up drill.
            - 1-2 {classSession.TargetLevel.ToString()} techniques.
            - 3-5 specific drills.
            - Controlled positional sparring with guidelines (emphasizing safety and technique over power).
            - A 10-minute cool-down drill.

            The curriculum should:
            - Be clear, easy to understand, and reflect professional training structures used in top {classSession.TargetLevel.ToString()} gyms.
            - Be engaging for {classSession.TargetLevel.ToString()}, focusing on technical execution to build confidence.
            - Not be too physically demanding, to avoid discouragement or potential injuries.
            - Minimize the risk of in-class drama, such as students using too much raw power.

            Return the curriculum as a JSON object with the following format:
            {{
                ""session_title"": ""A thematic title for the session"",
                ""duration"": ""60 minutes"",
                ""warm_up"": {{
                    ""name"": ""Warm-up drill name"",
                    ""description"": ""Description of the warm-up drill, focusing on preparing the body for the session's techniques."",
                    ""duration"": ""10 minutes""
                }},
                ""techniques"": [
                    {{
                    ""name"": ""Technique name"",
                    ""description"": ""Detailed description of the technique, including key steps."",
                    ""tips"": ""Instructor tips, such as common mistakes or key points to emphasize.""
                    }}
                ],
                ""drills"": [
                    {{
                    ""name"": ""Drill name"",
                    ""description"": ""Description of the drill, including how it relates to the techniques or weaknesses."",
                    ""focus"": ""Focus area (e.g., movement, positioning, timing)."",
                    ""duration"": ""Duration of the drill (e.g., 15 minutes).""
                    }}
                ],
                ""sparring"": {{
                    ""name"": ""Sparring activity name (e.g., Positional Sparring)"",
                    ""description"": ""Description of the sparring activity, including starting positions."",
                    ""guidelines"": ""Guidelines to ensure safety and focus on technique (e.g., no submissions, reset if position changes)."",
                    ""duration"": ""Duration of the sparring activity (e.g., 15 minutes).""
                }},
                ""cool_down"": {{
                    ""name"": ""Cool-down drill name"",
                    ""description"": ""Description of the cool-down drill, focusing on recovery and stretching."",
                    ""duration"": ""10 minutes""
                }}
            }}

            Ensure the curriculum is tailored to the students' attributes and addresses the most common weakness effectively. The drills should reinforce the techniques taught and focus on technique and movement rather than strength. Descriptions should be concise yet informative, providing enough details for an instructor to implement effectively.";
        }
    }
}
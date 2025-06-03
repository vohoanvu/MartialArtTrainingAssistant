using Google.Cloud.AIPlatform.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.StaticFiles;
using VideoSharing.Server.Domain.GoogleCloudStorageService;
using VideoSharing.Server.Models.Dtos;
using SharedEntities.Models;
using SharedEntities;
using System.Text;
using System.Text.Json;

namespace VideoSharing.Server.Domain.GeminiService
{
    public interface IGeminiVisionService
    {
        /// <summary>
        /// </summary>
        Task<GeminiVisionResponse> AnalyzeVideoAsync(string videoInput, string martialArt, string studentIdentifier, string videoDescription,
            string skillLevel, string trainingGoal = "both self-defense and competition");

        /// <summary>
        /// </summary>
        Task<GeminiChatResponse> SuggestClassCurriculum(List<string>? weaknesses, List<Fighter>? students, TrainingSession classSession);

        /// <summary>
        /// </summary>
        Task<MatchMakerResponse> SuggestFighterPairs(List<Fighter> fighters, TrainingSession classSession);
    }

    /// <summary>
    /// </summary>
    public class GeminiVisionService : IGeminiVisionService
    {
        private readonly PredictionServiceClient _predictionClient;
        private readonly string _projectId;
        private readonly string _location;
        private readonly string _model; //default to latest gemini pro model from appsettings for Vision analysis
        private readonly string _textModel = "gemini-2.5-flash-preview-05-20";

        private readonly IGoogleCloudStorageService _storageService;
        private readonly ILogger<GeminiVisionService> _logger;
        //private readonly IServiceProvider _serviceProvider;
        // (Optional but recommended) Add a static field for JsonSerializerOptions for consistent deserialization
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true // Handles cases where JSON might not strictly match C# casing,
        };

        public GeminiVisionService(IGoogleCloudStorageService storageService, ILogger<GeminiVisionService> logger)
        {
            _projectId = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudProjectId);
            _location = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GeminiVisionLocation);
            // Assuming the same model is used for vision and text, or you might want separate model IDs
            _model = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GeminiVisionModel);

            _storageService = storageService;
            _logger = logger;
            // _serviceProvider = serviceProvider;

            try
            {
                var keyPath = Global.AccessAppEnvironmentVariable(AppEnvironmentVariables.GoogleCloudServiceAccountKeyPath);
                if (!File.Exists(keyPath))
                {
                    // Log a warning or handle differently if key is optional for some environments (e.g. running on GCP infra)
                    _logger.LogWarning($"Service account key file not found at {keyPath}. Attempting to use default credentials if available.");
                    _predictionClient = new PredictionServiceClientBuilder
                    {
                        // Credential will be null, relying on ADC (Application Default Credentials)
                        Endpoint = $"{_location}-aiplatform.googleapis.com"
                    }.Build();

                    _logger.LogInformation("Initialized Vertex AI client with Application Default Credentials.");
                }
                else
                {
                    var credential = GoogleCredential.FromFile(keyPath)
                        .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

                    if (credential.UnderlyingCredential is ServiceAccountCredential serviceAccountCredential)
                    {
                        _logger.LogInformation("Using service account: {ServiceAccountEmail}", serviceAccountCredential.Id);
                    }
                    else
                    {
                        _logger.LogInformation("Using service account credentials from file.");
                    }

                    _predictionClient = new PredictionServiceClientBuilder
                    {
                        Credential = credential,
                        Endpoint = $"{_location}-aiplatform.googleapis.com"
                    }.Build();
                    _logger.LogInformation("Initialized Vertex AI client with service account key.");
                }
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
            string skillLevel,
            string trainingGoal = "both self-defense and competition") // Added trainingGoal as parameter with default
        {
            string fileUri;
            string mimeType;

            if (videoInput.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                videoInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                videoInput.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                fileUri = videoInput;
                mimeType = DetermineMimeType(videoInput);
            }
            else
            {
                mimeType = DetermineMimeType(videoInput);
                using var fileStream = new FileStream(videoInput, FileMode.Open, FileAccess.Read);
                fileUri = await _storageService.UploadFileAsync(fileStream, Path.GetFileName(videoInput), mimeType);
            }

            // Build the prompt using the new static method
            string prompt = BuildVisionAnalysisPrompt(martialArt, studentIdentifier, videoDescription, skillLevel, trainingGoal);

            var request = new GenerateContentRequest
            {
                Model = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{_model}", // Ensure this model supports video
                Contents =
                {
                    new Content
                    {
                        Role = "user",
                        Parts =
                        {
                            new Part { FileData = new FileData { MimeType = mimeType, FileUri = fileUri } },
                            new Part { Text = prompt }
                        }
                    }
                },
                SafetySettings =
                {
                    new SafetySetting { Category = HarmCategory.SexuallyExplicit, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.HateSpeech, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.DangerousContent, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.Harassment, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.4f,
                    TopP = 1.0f,
                    MaxOutputTokens = 65535, // Increased from 2048, 65535 is current Max.
                    ResponseMimeType = "application/json",
                }
            };

            try
            {
                _logger.LogInformation("Sending GenerateContent request for video: {FileUri} to model {Model} in project {ProjectId}",
                    fileUri, _model, _projectId);
                // _logger.LogDebug("Vision Prompt: {Prompt}", prompt); // Log prompt at Debug level if needed

                GenerateContentResponse response = await _predictionClient.GenerateContentAsync(request);
                string resultJson = response.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault(p => p.Text != null)?.Text
                    ?? throw new InvalidOperationException("No valid JSON response received from the API for video analysis.");
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
        public async Task<GeminiChatResponse> SuggestClassCurriculum(List<string>? weaknesses, List<Fighter>? students, TrainingSession classSession)
        {
            var curriculumPrompt = BuildCurriculumPrompt(weaknesses, students, classSession);

            var request = new GenerateContentRequest
            {
                Model = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{_textModel}",
                Contents =
                {
                    new Content
                    {
                        Role = "user",
                        Parts = { new Part { Text = curriculumPrompt } }
                    }
                },
                SafetySettings =
                {
                    new SafetySetting { Category = HarmCategory.SexuallyExplicit, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.HateSpeech, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.DangerousContent, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.Harassment, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 1.0f, // Higher temperature for more creative responses
                    TopP = 1.0f,
                    MaxOutputTokens = 16384, // Ensure this is appropriate for curriculum length
                    ResponseMimeType = "application/json",
                }
            };

            try
            {
                _logger.LogInformation("Sending GenerateContent request for class curriculum to model {Model} in project {ProjectId}", _textModel, _projectId);
                // _logger.LogDebug("Curriculum Prompt: {Prompt}", curriculumPrompt); // Log prompt at Debug level

                GenerateContentResponse response = await _predictionClient.GenerateContentAsync(request);
                string resultJson = response.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault(p => p.Text != null)?.Text
                    ?? throw new InvalidOperationException("No valid JSON response received from the API for curriculum suggestion.");
                _logger.LogInformation("Received valid curriculum response from Vertex AI.");
                return new GeminiChatResponse { CurriculumJson = resultJson };
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Vertex AI API call failed for curriculum generation: project: {ProjectId}, model: {Model}, HTTP status: {StatusCode}, error details: {Details}",
                    _projectId, _textModel, ex.HttpStatusCode, ex.Error?.ToString());
                throw new InvalidOperationException($"Vertex AI API call failed for curriculum generation: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Vertex AI API call for curriculum generation.");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<MatchMakerResponse> SuggestFighterPairs(List<Fighter> fighters, TrainingSession classSession)
        {
            var response = new MatchMakerResponse();

            if (fighters == null || fighters.Count == 0)
            {
                _logger.LogInformation("SuggestFighterPairs: No fighters provided.");
                response.SuggestedPairings = new MatchMakerResponseContent
                {
                    Pairs = [],
                    PairingRationale = "No students provided to pair."
                };
                response.RawGenerateContentResponseJson = JsonSerializer.Serialize(response, _jsonSerializerOptions);
                response.IsSuccessfullyParsed = true;
                return response;
            }

            if (fighters.Count == 1)
            {
                _logger.LogInformation("SuggestFighterPairs: Only one fighter provided.");
                var singleFighter = fighters.First();
                response.SuggestedPairings = new MatchMakerResponseContent
                {
                    Pairs = [],
                    UnpairedStudent = new UnpairedFighterInfo
                    {
                        StudentId = singleFighter.Id,
                        StudentName = singleFighter.FighterName,
                        Reason = "Only one student in the class."
                    },
                    PairingRationale = "Only one student available."
                };
                response.RawGenerateContentResponseJson = JsonSerializer.Serialize(response, _jsonSerializerOptions);
                response.IsSuccessfullyParsed = true;
                return response;
            }

            var pairingPrompt = BuildFighterPairingPrompt(fighters, classSession);

            var request = new GenerateContentRequest
            {
                Model = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{_textModel}",
                Contents = { new Content { Role = "user", Parts = { new Part { Text = pairingPrompt } } } },
                SafetySettings =
                {
                    new SafetySetting { Category = HarmCategory.SexuallyExplicit, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.HateSpeech, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.DangerousContent, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone },
                    new SafetySetting { Category = HarmCategory.Harassment, Threshold = SafetySetting.Types.HarmBlockThreshold.BlockNone }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.2f,
                    TopP = 1.0f,
                    MaxOutputTokens = 8192,
                    ResponseMimeType = "application/json", // Crucial: Ask AI for JSON output
                }
            };

            try
            {
                _logger.LogInformation("Sending GenerateContent request for fighter pairing to model {Model} in project {ProjectId}", _textModel, _projectId);
                _logger.LogInformation("User Prompt: {Prompt}", pairingPrompt);
                GenerateContentResponse geminiResponse = await _predictionClient.GenerateContentAsync(request);

                string resultJson = geminiResponse.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault(p => p.Text != null)?.Text
                    ?? string.Empty;
                _logger.LogInformation("Model Response: {response}", geminiResponse);

                response.RawGenerateContentResponseJson = geminiResponse.Candidates.FirstOrDefault()?.Content.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(resultJson))
                {
                    _logger.LogWarning("Received empty or whitespace JSON response from Vertex AI for fighter pairing.");
                    response.ErrorMessage = "No valid JSON response received from the API (empty or whitespace).";
                    response.IsSuccessfullyParsed = false;
                    // Create a default empty content to avoid null SuggestedPairings
                    response.SuggestedPairings = new MatchMakerResponseContent { PairingRationale = response.ErrorMessage };
                    return response;
                }

                // Helper to remove markdown code fences (```json ... ``` or ``` ... ```)
                string cleanedJson = CleanJsonFromMarkdown(resultJson);

                try
                {
                    // Deserialize the cleaned JSON string into our C# object model
                    response.SuggestedPairings = JsonSerializer.Deserialize<MatchMakerResponseContent>(cleanedJson, _jsonSerializerOptions);
                    response.IsSuccessfullyParsed = response.SuggestedPairings != null;
                    if (!response.IsSuccessfullyParsed)
                    {
                        response.ErrorMessage = "JSON deserialization resulted in a null object, though no exception was thrown.";
                    }
                    _logger.LogInformation("Successfully deserialized fighter pairing response from Vertex AI.");
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize fighter pairing JSON. Cleaned JSON: {CleanedJson}. Raw JSON: {RawJson}", cleanedJson, resultJson);
                    response.ErrorMessage = $"JSON Deserialization Error: {jsonEx.Message}. Check logs for raw JSON.";
                    response.IsSuccessfullyParsed = false;
                    // Create a default empty content to avoid null SuggestedPairings
                    response.SuggestedPairings = new MatchMakerResponseContent { PairingRationale = $"Failed to parse AI response: {jsonEx.Message}" };
                }
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Vertex AI API call failed for fighter pairing: project: {ProjectId}, model: {Model}, HTTP status: {StatusCode}, error details: {Details}",
                    _projectId, _textModel, ex.HttpStatusCode, ex.Error?.ToString());
                response.ErrorMessage = $"Vertex AI API call failed: {ex.Message}";
                response.IsSuccessfullyParsed = false;
                response.SuggestedPairings = new MatchMakerResponseContent { PairingRationale = response.ErrorMessage };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Vertex AI API call for fighter pairing.");
                response.ErrorMessage = $"Unexpected error: {ex.Message}";
                response.IsSuccessfullyParsed = false;
                response.SuggestedPairings = new MatchMakerResponseContent { PairingRationale = response.ErrorMessage };
            }

            return response;
        }

        private static string BuildVisionAnalysisPrompt(string martialArt, string studentIdentifier, string videoDescription, string skillLevel, string trainingGoal)
        {
            // Comprehensive list of BJJ weakness categories, useful for the AI's selection process.
            const string bjjWeaknessCategories = "'Takedown Defense/Offense', 'Guard Passing/Retention', 'Submission', 'Posture Control', 'Grip Strength', 'Timing', 'Stamina'";
            // Technique types and positional scenarios for the AI to select from.
            const string techniqueTypes = "'Takedown', 'Submission', 'Sweep', 'Pass', 'Escape', 'Transition', 'Control', 'Defense'";
            const string positionalScenarios = "'Standing', 'Guard', 'Half Guard', 'Side Control', 'Mount', 'Back Control', 'Knee on Belly', 'Turtle'";

            return $@"
            You are an expert {martialArt} instructor. You have been given video of your student with the description as '{videoDescription}'.
            Analyze the performance of the student, identified as {studentIdentifier}, in this {martialArt} video. The student is at the {skillLevel} level and is training for {trainingGoal}.
            Provide a detailed analysis of the student's techniques, execution, strengths, weaknesses, and suggest specific drills for practice on what they could do differently.
            Pay close attention to the student's score-losing techniques, based loosely on official IBJJF rule set, and tailor the situational drills (positional sparring) to only focus on practicing that weakness.

            Format the output as a JSON object with the following example format:
            {{
                ""overall_description"": ""A detailed description of the student's overall performance in the sparring session."",
                ""techniques_identified"": [
                    {{
                    ""technique_name"": ""Name of the technique (e.g., 'Armbar')"",
                    ""description"": ""Description of how the technique was executed."",
                    ""start_timestamp"": ""Timestamp in the video where the technique occurs (e.g., '00:01:23')"",
                    ""end_timestamp"": ""Timestamp in the video where the technique ends (e.g., '00:01:30')"",
                    ""technique_type"": ""Select from: [{techniqueTypes}]"",
                    ""positional_scenario"": ""Select from: [{positionalScenarios}]""
                    }}
                ],
                ""strengths"": [
                    {{
                    ""description"": ""Description of a strength observed in the student's performance."",
                    ""related_technique"": ""Optional: Name of the technique related to this strength.""
                    }}
                ],
                ""areas_for_improvement"": [
                    {{
                    ""description"": ""Description of an area where the student can improve. e.g., the student got stuck in botton side control, or maybe they got a triangle but didnt know how to finish it."",
                    ""weakness_category"": ""Select from: [{bjjWeaknessCategories}]"",
                    ""related_technique"": ""Optional: Name of the technique related to this area."",
                    ""keywords"": ""sample keywords related to each weakness that students can use to search for related online content/instructional videos themselves.""
                    }}
                ],
                ""suggested_drills"": [
                    {{
                    ""name"": ""Name of the drill (e.g., 'Guard Passing Drill')"",
                    ""description"": ""Detailed description of the drill. e.g., start from closed guard, with your partner on bottom, you are on top trying to pass their guard. If either the bottom person sweeps or top person passes the guard, reset the match with opposite starting positions."",
                    ""focus"": ""e.g., You should focus on passing guard or sweep your opponent, by paying attention to smaller moves such as grip fighting or underhook setup"",
                    ""duration"": ""e.g., '5 minutes'"",
                    ""related_technique"": ""Name of the technique the drill is intended to improve.""
                    }}
                ]
            }}

            Ensure that the techniques identified are categorized by their technique type and linked to the appropriate positional scenarios as defined. Tailor the feedback and suggested drills to the student's {skillLevel} and {trainingGoal}, ensuring they are actionable and relevant to their current abilities and objectives.
            ";
        }

        private string DetermineMimeType(string pathOrUri)
        {
            string extension;
            if (pathOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUri.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(pathOrUri);
                extension = Path.GetExtension(uri.AbsolutePath);
            }
            else
            {
                extension = Path.GetExtension(pathOrUri);
            }

            var provider = new FileExtensionContentTypeProvider();
            if (string.IsNullOrEmpty(extension) || !provider.TryGetContentType(extension, out var contentType))
            {
                _logger.LogWarning("Could not determine MIME type for '{PathOrUri}'. Defaulting to 'video/mp4'.", pathOrUri);
                contentType = "video/mp4"; // Default to mp4 if unknown or no extension
            }
            return contentType;
        }

        private static string BuildCurriculumPrompt(List<string>? weaknesses, List<Fighter>? students, TrainingSession classSession)
        {
            var studentDetails = students != null && students.Any()
                ? students.Select(s =>
                    $"- Student: Belt Rank - {s.BelkRank}, Height - {s.Height:F1} ft, Weight - {s.Weight} lbs, Training Experience - {s.Experience}"
                  ).Aggregate((a, b) => $"{a}\n{b}")
                : "No student details provided.";

            string weaknessInstruction;
            string addressingClause;
            const string comprehensiveBjjWeaknessList = "'Takedown Defense/Offense', 'Guard Passing/Retention', 'Submission', 'Posture Control', 'Grip Strength', 'Timing', 'Stamina'";

            if (weaknesses != null && weaknesses.Count > 0)
            {
                weaknessInstruction = $"- Base the curriculum on the following student-specific weaknesses: [{string.Join(", ", weaknesses)}].";
                addressingClause = "the specified student weaknesses";
            }
            else
            {
                weaknessInstruction = $"- For the {classSession.TargetLevel.ToString()} level, determine the 2-3 most common and impactful weakness categories from the following general BJJ weakness categories: [{comprehensiveBjjWeaknessList}]. Focus the curriculum on addressing these determined weaknesses.";
                addressingClause = "the determined weaknesses";
            }

            return $@"You are an expert {classSession.MartialArt.ToString()} instructor. Design a {classSession.Duration}-hour class session curriculum based on the following:
            {weaknessInstruction}
            - Students:
                {studentDetails}

            Create a curriculum that includes:
            - A 10-minute warm-up drill.
            - 1-2 {classSession.TargetLevel.ToString()} techniques.
            - 3-5 specific drills.
            - Controlled positional sparring with guidelines (emphasizing safety and technique over power).
            - A 10-minute cool-down drill.

            The curriculum should:
            - Be clear, easy to understand, and reflect professional training structures used in top {classSession.MartialArt.ToString()} gyms.
            - Be engaging for {classSession.TargetLevel.ToString()} target level, focusing on situational drilling (positional sparring) of a specific technique weakness instead of full-on rolling until submission.

            Return the curriculum as a JSON object with the following format:
            {{
                ""session_title"": ""e.g., Guard Passing sequence or {classSession.TargetLevel.ToString()} Level Weakness Focus"",
                ""duration"": ""{(int)(classSession.Duration * 60)} minutes"",
                ""target_level_specific_weaknesses_identified"": [""If AI determined weaknesses, list them here, e.g., Guard Retention, Takedown Defense""],
                ""warm_up"": {{
                    ""name"": ""e.g. Rollback/sit-through, Triangle sit-ups"",
                    ""description"": ""e.g. Each person do this solo drill by starting as technical stand-up, rolling back on your shoulders all the way through, and then use your hip thrust to sit back up on one knee. For Triangle sit-ups, start as sit-up position and falling back on your shoulders and lifting your hips up to simulate a triangle lock."",
                    ""duration"": ""10 minutes""
                }},
                ""techniques"": [
                    {{
                    ""name"": ""Technique name related to identified weaknesses"",
                    ""description"": ""Detailed description of the technique, including key steps."",
                    ""tips"": ""Instructor tips, such as common mistakes or key points to emphasize.""
                    }}
                ],
                ""drills"": [
                    {{
                    ""name"": ""Drill name"",
                    ""description"": ""Description of the drill, including how it relates to the techniques or identified weaknesses."",
                    ""focus"": ""Focus Area. e.g., focus on protecting your guard and not let the opponent pass, for example, by keeping elbows tight to the body and fight the underhook. This should tie back to identified weaknesses."",
                    ""duration"": ""Duration of the drill (e.g., 10-15 minutes).""
                    }}
                ],
                ""sparring"": {{
                    ""name"": ""Sparring activity name (e.g., Positional Sparring focusing on weaknesses)"",
                    ""description"": ""Description of the sparring activity, including starting positions. e.g., start from closed guard, with your partner on bottom, you are on top trying to pass their guard. If either the bottom person sweeps or top person passes the guard, reset the match with opposite starting positions. Should be relevant to the identified weaknesses."",
                    ""guidelines"": ""Guidelines to ensure safety and focus on technique (e.g., no submissions, reset if position changes, students should focus on the identified weaknesses, by paying attention to smaller moves such as grip fighting or underhook setup)."",
                    ""duration"": ""Duration of the sparring activity (e.g., 15-20 minutes).""
                }},
                ""cool_down"": {{
                    ""name"": ""Cool-down drill name"",
                    ""description"": ""Description of the cool-down drill, focusing on recovery and stretching."",
                    ""duration"": ""10 minutes""
                }}
            }}

            Ensure the curriculum is tailored to the students' attributes (if provided) and effectively addresses {addressingClause}. The drills should reinforce the techniques taught and focus on technique and movement rather than strength. Descriptions should be concise yet informative, providing enough details for an instructor to implement effectively.";
        }

        private static string BuildFighterPairingPrompt(List<Fighter> fighters, TrainingSession classSession)
        {
            var studentDetailsSb = new StringBuilder();
            studentDetailsSb.AppendLine("Students available for pairing (ID, Name, Belt, Weight, Height, Birthdate, Experience):");
            if (fighters == null || fighters.Count == 0)
            {
                studentDetailsSb.AppendLine("No students provided.");
            }
            else
            {
                foreach (var student in fighters)
                {
                    studentDetailsSb.AppendLine($"- ID: {student.Id}, Name: {student.FighterName}, Belt: {student.BelkRank.ToString()}, Weight: {student.Weight:F1} kg, Height: {student.Height:F0} cm, Birtdate: {student.Birthdate.ToString("d")}, Experience: {student.Experience.ToString()}");
                }
            }

            string instructorInfo;
            if (classSession.Instructor != null)
            {
                var ins = classSession.Instructor;
                instructorInfo = $"The Class Instructor is: ID: {ins.Id}, Name: {ins.FighterName}, Belt: {ins.BelkRank},Weight: {ins.Weight:F1} kg, Height: {ins.Height:F0} cm, Birtdate: {ins.Birthdate:d}.";
            }
            else
            {
                instructorInfo = "No Class Instructor details provided for this session.";
            }

            // Determine if the number of students is odd
            bool isOddNumberOfStudents = fighters != null && fighters.Count % 2 != 0;
            string oddNumberRule = "";
            if (isOddNumberOfStudents)
            {
                if (classSession.Instructor != null)
                {
                    oddNumberRule = "Since there is an odd number of students, identify the most advanced (by belt/experience) or biggest (by weight/height,age) student who would otherwise be unpaired, and pair this student with the Class Instructor. Include this pairing in the 'pairs' list, using the instructor's ID and name.";
                }
                else
                {
                    oddNumberRule = "Since there is an odd number of students and no instructor details are provided, one student will be left unpaired. Clearly indicate this student in the 'unpaired_student' field in the JSON output.";
                }
            }

            return $@"You are an expert {classSession.MartialArt.ToString()} instructor. Your task is to pair up students for training based on their compatibility, considering skill level, size, and age.
            {studentDetailsSb.ToString()}

            {instructorInfo}

            Instructions:
            1. Pair students to create the most compatible training partnerships. Aim for pairs with similar skill levels (Belt Rank, Experience) and compatible physical attributes (Weight, Height, Age) for safe and productive training.
            2. {oddNumberRule}
            3. If no students are provided or only one student is present, reflect this appropriately in your response.
            4. Ensure every student from the input list is accounted for in the output JSON, either in a pair or as an unpaired student. Use the provided numeric IDs for each fighter in the JSON output.
            5. Watch out for inconsistent unit metric and weird fighter's details, such as unusually high or low values for weight, height, or age, and ensure they are reasonable.

            Return your pairings as a JSON object with the following format:
            {{
            ""pairs"": [
                {{
                ""fighter1_id"": 123,
                ""fighter1_name"": ""Student Name One"",
                ""fighter2_id"": 456,
                ""fighter2_name"": ""Student Name Two""
                }}
            ],
            ""unpaired_student"": {{ // Include this object ONLY if a student is left unpaired (e.g., odd number and no instructor)
                ""student_id"": 789,
                ""student_name"": ""Unpaired Student Name"",
                ""reason"": ""Reason for being unpaired (e.g., Odd number of students, instructor not available for pairing).""
            }},
            ""pairing_rationale"": ""(Optional) Brief overall rationale for your pairing strategy or any specific interesting/challenging pairing decisions.""
            }}

            Prioritize creating balanced pairs. If all students can be paired (even number), the 'unpaired_student' field should be omitted or null.
            ";
        }

        /// <summary>
        /// Cleans a JSON string that might be wrapped in Markdown code fences.
        /// </summary>
        /// <param name="rawJson">The raw string from the LLM.</param>
        /// <returns>A cleaned JSON string, or the original string if no fences are detected.</returns>
        private static string CleanJsonFromMarkdown(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return string.Empty;

            string trimmedJson = rawJson.Trim();

            // Remove ```json ... ```
            if (trimmedJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase) && trimmedJson.EndsWith("```"))
            {
                // Length of "```json" is 7, "```" is 3
                if (trimmedJson.Length > 7 + 3)
                {
                    return trimmedJson.Substring(7, trimmedJson.Length - 7 - 3).Trim();
                }
                return string.Empty; // Avoid issues with very short strings
            }
            // Remove ``` ... ``` (generic markdown code block)
            else if (trimmedJson.StartsWith("```") && trimmedJson.EndsWith("```"))
            {
                // Length of "```" is 3
                if (trimmedJson.Length > 3 + 3)
                {
                    return trimmedJson.Substring(3, trimmedJson.Length - 3 - 3).Trim();
                }
                return string.Empty; // Avoid issues with very short strings
            }
            return trimmedJson; // Return the original (trimmed) if no fences found
        }
    }
}
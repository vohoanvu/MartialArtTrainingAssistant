using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.StaticFiles;
using SharedEntities.Models;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Domain.GoogleCloudStorageService;
using VideoAnalysis.Server.Models.Dtos;

namespace VideoAnalysis.Server.Domain.GeminiService
{
    public interface IGeminiVisionService
    {
        /// <summary>
        /// Analyze a martial arts video using Gemini Vision.
        /// </summary>
        Task<GeminiVisionResponse> AnalyzeVideoAsync(string videoInput, string martialArt, string studentIdentifier, string videoDescription,
            string skillLevel, string trainingGoal = "both self-defense and competition");

        /// <summary>
        /// Suggest a class curriculum using Gemini text generation.
        /// </summary>
        Task<GeminiChatResponse> SuggestClassCurriculum(List<string>? weaknesses, List<Fighter>? students, TrainingSession classSession);

        /// <summary>
        /// Suggest fighter pairings using Gemini text generation.
        /// </summary>
        Task<MatchMakerResponse> SuggestFighterPairs(List<Fighter> fighters, TrainingSession classSession);
    }

    /// <summary>
    /// Single-shot Gemini features (v1 video analysis, curriculum, fighter pairing) on Vertex AI. The model,
    /// location and generation settings of each feature come from <see cref="IAiModelConfigService"/> (AI Console
    /// override, else appsettings/env), and requests are built per the model's capabilities.
    /// </summary>
    public class GeminiVisionService : IGeminiVisionService
    {
        private readonly IVertexRestClient _vertex;
        private readonly IAiModelConfigService _modelConfig;
        private readonly IGoogleCloudStorageService _storageService;
        private readonly ILogger<GeminiVisionService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        public GeminiVisionService(
            IVertexRestClient vertex,
            IAiModelConfigService modelConfig,
            IGoogleCloudStorageService storageService,
            ILogger<GeminiVisionService> logger)
        {
            _vertex = vertex;
            _modelConfig = modelConfig;
            _storageService = storageService;
            _logger = logger;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<GeminiVisionResponse> AnalyzeVideoAsync(
            string videoInput,
            string martialArt,
            string studentIdentifier,
            string videoDescription,
            string skillLevel,
            string trainingGoal = "both self-defense and competition")
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

            string prompt = BuildVisionAnalysisPrompt(martialArt, studentIdentifier, videoDescription, skillLevel, trainingGoal);
            var settings = await _modelConfig.GetEffectiveAsync(AiFeatureKeys.VideoAnalysisSingleShot);

            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { fileData = new { mimeType, fileUri } },
                            new { text = prompt },
                        }
                    }
                },
                generationConfig = BuildGenerationConfig(settings, topP: 0.95f),
                safetySettings = VertexRestClient.AllSafetyOff(),
            };

            _logger.LogInformation("Sending video analysis request for: {FileUri} to model {Model}", fileUri, settings.ModelId);

            var response = await _vertex.GenerateContentAsync(settings.ModelId, settings.Location, request, CancellationToken.None);
            string resultJson = _vertex.ExtractText(response);

            return new GeminiVisionResponse { AnalysisJson = resultJson };
        }

        /// <inheritdoc/>
        public async Task<GeminiChatResponse> SuggestClassCurriculum(
            List<string>? weaknesses,
            List<Fighter>? students,
            TrainingSession classSession)
        {
            var curriculumPrompt = BuildCurriculumPrompt(weaknesses, students, classSession);
            var settings = await _modelConfig.GetEffectiveAsync(AiFeatureKeys.ClassCurriculum);

            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = curriculumPrompt } }
                    }
                },
                generationConfig = BuildGenerationConfig(settings, topP: 1.0f),
                safetySettings = VertexRestClient.AllSafetyOff(),
            };

            _logger.LogInformation("Sending curriculum request to model {Model}", settings.ModelId);

            var response = await _vertex.GenerateContentAsync(settings.ModelId, settings.Location, request, CancellationToken.None);
            string resultJson = _vertex.ExtractText(response);

            return new GeminiChatResponse { CurriculumJson = resultJson };
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
                response.RawGenerateContentResponseJson = JsonSerializer.Serialize(response, JsonOptions);
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
                response.RawGenerateContentResponseJson = JsonSerializer.Serialize(response, JsonOptions);
                response.IsSuccessfullyParsed = true;
                return response;
            }

            var pairingPrompt = BuildFighterPairingPrompt(fighters, classSession);

            try
            {
                var settings = await _modelConfig.GetEffectiveAsync(AiFeatureKeys.FighterPairing);
                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = pairingPrompt } }
                        }
                    },
                    generationConfig = BuildGenerationConfig(settings, topP: 1.0f),
                    safetySettings = VertexRestClient.AllSafetyOff(),
                };

                _logger.LogInformation("Sending fighter pairing request to model {Model}", settings.ModelId);
                _logger.LogInformation("User Prompt: {Prompt}", pairingPrompt);

                var geminiResponse = await _vertex.GenerateContentAsync(settings.ModelId, settings.Location, request, CancellationToken.None);
                string resultJson = _vertex.ExtractText(geminiResponse);

                response.RawGenerateContentResponseJson = resultJson;
                string cleanedJson = CleanJsonFromMarkdown(resultJson);

                try
                {
                    response.SuggestedPairings = JsonSerializer.Deserialize<MatchMakerResponseContent>(cleanedJson, JsonOptions);
                    response.IsSuccessfullyParsed = response.SuggestedPairings != null;
                    if (!response.IsSuccessfullyParsed)
                    {
                        response.ErrorMessage = "JSON deserialization resulted in a null object, though no exception was thrown.";
                    }
                    _logger.LogInformation("Successfully deserialized fighter pairing response.");
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize fighter pairing JSON. Cleaned JSON: {CleanedJson}. Raw JSON: {RawJson}", cleanedJson, resultJson);
                    response.ErrorMessage = $"JSON Deserialization Error: {jsonEx.Message}. Check logs for raw JSON.";
                    response.IsSuccessfullyParsed = false;
                    response.SuggestedPairings = new MatchMakerResponseContent { PairingRationale = $"Failed to parse AI response: {jsonEx.Message}" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during fighter pairing request.");
                response.ErrorMessage = $"Error: {ex.Message}";
                response.IsSuccessfullyParsed = false;
                response.SuggestedPairings = new MatchMakerResponseContent { PairingRationale = response.ErrorMessage };
            }

            return response;
        }

        // All three features expect JSON back; thinking/sampling keys are emitted per the model's capabilities.
        private static Dictionary<string, object> BuildGenerationConfig(AiFeatureSettings settings, float topP) =>
            GeminiModelCatalog.BuildGenerationConfig(
                GeminiModelCatalog.Resolve(settings.ModelId),
                settings.ThinkingLevel, settings.ThinkingBudget, settings.Temperature, settings.MaxOutputTokens,
                topP: topP, responseMimeType: "application/json");

        // ── Prompt builders ───────────────────────────────────────────────────

        private static string BuildVisionAnalysisPrompt(string martialArt, string studentIdentifier, string videoDescription, string skillLevel, string trainingGoal)
        {
            const string bjjWeaknessCategories = "'Takedown Defense/Offense', 'Guard Passing/Retention', 'Sweep', 'Submission', 'Posture Control', 'Grip Strength', 'Timing', 'Stamina'";
            const string techniqueTypes = "'Takedown', 'Submission', 'Sweep', 'Pass', 'Escape', 'Transition', 'Control', 'Defense'";
            const string positionalScenarios = "'Standing', 'Guard', 'Half Guard', 'Side Control', 'Mount', 'Back Control', 'Knee on Belly', 'Turtle'";

            return $@"
            You are an expert {martialArt} instructor. You have been given video of your student with the description as '{videoDescription}'.
            Analyze the performance of the student, identified as {studentIdentifier}, in this {martialArt} video using the instructions below. The student is at the {skillLevel} level and is training for {trainingGoal}.
            Describe the student's techniques, execution, strengths, and weaknesses in detail. For each techniques used, provide the time stamps of the video where the technique starts and ends, and categorize the technique type and positional scenario.
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

        private static string BuildCurriculumPrompt(List<string>? weaknesses, List<Fighter>? students, TrainingSession classSession)
        {
            var studentDetails = students != null && students.Count > 0
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
                weaknessInstruction = $"- For the {classSession.TargetLevel} level, determine the 2-3 most common and impactful weakness categories from the following general BJJ weakness categories: [{comprehensiveBjjWeaknessList}]. Focus the curriculum on addressing these determined weaknesses.";
                addressingClause = "the determined weaknesses";
            }

            return $@"You are an expert {classSession.MartialArt} instructor. Design a {classSession.Duration}-hour class session curriculum based on the following:
            {weaknessInstruction}
            - Students:
                {studentDetails}

            Create a curriculum that includes:
            - A 10-minute warm-up drill.
            - 1-2 {classSession.TargetLevel} techniques.
            - 3-5 specific drills.
            - Controlled positional sparring with guidelines (emphasizing safety and technique over power).
            - A 10-minute cool-down drill.

            The curriculum should:
            - Be clear, easy to understand, and reflect professional training structures used in top {classSession.MartialArt} gyms.
            - Be engaging for {classSession.TargetLevel} target level, focusing on situational drilling (positional sparring) of a specific technique weakness instead of full-on rolling until submission.

            Return the curriculum as a JSON object with the following format:
            {{
                ""session_title"": ""e.g., Guard Passing sequence or {classSession.TargetLevel} Level Weakness Focus"",
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
                    studentDetailsSb.AppendLine($"- ID: {student.Id}, Name: {student.FighterName}, Belt: {student.BelkRank}, Weight: {student.Weight:F1} kg, Height: {student.Height:F0} cm, Birtdate: {student.Birthdate:d}, Experience: {student.Experience}");
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

            return $@"You are an expert {classSession.MartialArt} instructor. Your task is to pair up students for training based on their compatibility, considering skill level, size, and age.
            {studentDetailsSb}

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

        // ── Utilities ─────────────────────────────────────────────────────────

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
                contentType = "video/mp4";
            }
            return contentType;
        }

        private static string CleanJsonFromMarkdown(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return string.Empty;

            string trimmedJson = rawJson.Trim();

            if (trimmedJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase) && trimmedJson.EndsWith("```"))
            {
                if (trimmedJson.Length > 10)
                    return trimmedJson[7..^3].Trim();
                return string.Empty;
            }
            if (trimmedJson.StartsWith("```") && trimmedJson.EndsWith("```"))
            {
                if (trimmedJson.Length > 6)
                    return trimmedJson[3..^3].Trim();
                return string.Empty;
            }
            return trimmedJson;
        }
    }
}


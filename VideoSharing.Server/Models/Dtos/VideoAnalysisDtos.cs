using System.Text.Json.Serialization;
using SharedEntities.Models;

namespace VideoSharing.Server.Models.Dtos
{
    //For Youtube sharing url
    public class SharingVideoRequest
    {
        [JsonPropertyName("videoUrl")]
        public required string VideoUrl { get; set; }
    }

    public class GeminiVisionResponse
    {
        public string AnalysisJson { get; set; } = string.Empty;
    }

    public class GeminiChatResponse
    {
        public string CurriculumJson { get; set; } = string.Empty;
    }

    public class UploadedVideoDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string? FighterName { get; set; }
        public string? StudentIdentifier { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadTimestamp { get; set; }
        public string? Description { get; set; }

        public string? AiAnalysisResult { get; set; }

        public string SignedUrl { get; set; }
        public string MartialArt { get; set; }
        public int FighterId { get; set; }
    }

    public class UploadVideoRequest
    {
        public string? Description { get; set; }
        public string StudentIdentifier { get; set; } // Used for LLM Prompt parsing, e.g., "Fighter in blue gi"
        public MartialArt MartialArt { get; set; }
    }

    public class TechniqueDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public TechniqueTypeDto TechniqueType { get; set; }
        public PositionalScenarioDto PositionalScenario { get; set; }
        public string? StartTimestamp { get; set; }
        public string? EndTimestamp { get; set; }
    }

    public class DrillDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string? Focus { get; set; }
        public string Duration { get; set; }
        public string Description { get; set; }
        public int RelatedTechniqueId { get; set; }
        public string RelatedTechniqueName { get; set; }
    }

    public class TechniqueTypeDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int PositionalScenarioId { get; set; }
    }

    public class PositionalScenarioDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
    }

    public class AnalysisResultDto
    {
        public int? Id { get; set; }
        public List<TechniqueDto>? Techniques { get; set; }
        public List<DrillDto>? Drills { get; set; }
        public List<StrengthDto>? Strengths { get; set; }
        public List<AreasForImprovementDto>? AreasForImprovement { get; set; }
        public string? OverallDescription { get; set; }
    }

    public class PartialAnalysisResultDto
    {
        public int? Id { get; set; }
        public List<TechniqueDto>? Techniques { get; set; }
        public List<DrillDto>? Drills { get; set; }
        public List<StrengthDto>? Strengths { get; set; }
        public List<AreasForImprovementDto>? AreasForImprovement { get; set; }
        public string? OverallDescription { get; set; }
    }

    public class StrengthDto : Strength
    {
        public int? RelatedTechniqueId { get; set; }
    }

    public class AreasForImprovementDto : AreaForImprovement
    {
        public int? RelatedTechniqueId { get; set; }
    }

    public class FighterPair
    {
        /// <summary>
        /// </summary>
        [JsonPropertyName("fighter1_id")]
        public int Fighter1Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonPropertyName("fighter1_name")]
        public string Fighter1Name { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [JsonPropertyName("fighter2_id")]
        public int Fighter2Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonPropertyName("fighter2_name")]
        public string Fighter2Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// </summary>
    public class UnpairedFighterInfo
    {
        [JsonPropertyName("student_id")]
        public int StudentId { get; set; }

        [JsonPropertyName("student_name")]
        public string StudentName { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the deserialized content of the fighter pairing suggestion from the AI.
    /// </summary>
    public class MatchMakerResponseContent
    {
        [JsonPropertyName("pairs")]
        public List<FighterPair> Pairs { get; set; } = new List<FighterPair>();

        [JsonPropertyName("unpaired_student")]
        public UnpairedFighterInfo? UnpairedStudent { get; set; } // Nullable if not present

        [JsonPropertyName("pairing_rationale")]
        public string? PairingRationale { get; set; } // Nullable if not present
    }

    /// <summary>
    /// Wrapper for the AI's response when suggesting fighter pairs.
    /// Contains the deserialized object and raw JSON for debugging.
    /// </summary>
    public class MatchMakerResponse
    {
        /// <summary>
        /// The deserialized suggested pairings.
        /// Null if parsing failed or no valid response was received.
        /// </summary>
        public MatchMakerResponseContent? SuggestedPairings { get; set; }

        /// <summary>
        /// The raw JSON string received from the AI. Useful for debugging.
        /// </summary>
        public string RawFighterPairsJson { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the raw JSON was successfully parsed into SuggestedPairings.
        /// </summary>
        public bool IsSuccessfullyParsed { get; set; } = false;

        /// <summary>
        /// Contains an error message if parsing failed or an API error occurred.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
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

    public class UploadedVideoDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
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
        public string RelatedTechniqueName { get; set; }
    }

    public class TechniqueTypeDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string PositionalScenario { get; set; }
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
        public List<Strength>? Strengths { get; set; }
        public List<AreaForImprovement>? AreasForImprovement { get; set; }
        public string? OverallDescription { get; set; }
    }

    public class PartialAnalysisResultDto
    {
        public int? Id { get; set; }
        public List<TechniqueDto>? Techniques { get; set; }
        public List<DrillDto>? Drills { get; set; }
        public List<Strength>? Strengths { get; set; }
        public List<AreaForImprovement>? AreasForImprovement { get; set; }
        public string? OverallDescription { get; set; }
    }
}
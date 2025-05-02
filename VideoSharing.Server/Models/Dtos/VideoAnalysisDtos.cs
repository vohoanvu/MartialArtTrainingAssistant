using System.Text.Json.Serialization;
using SharedEntities.Models;

namespace VideoSharing.Server.Models.Dtos
{
    public class VideoAnalysisRequest
    {
        public required int VideoId { get; set; }
    }

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
    }

    public class UploadVideoRequest 
    {
        public string? Description { get; set; }
        public string StudentIdentifier { get; set; } // Used for LLM Prompt parsing, e.g., "Fighter in blue gi"
        public MartialArt MartialArt { get; set; }
    }
}
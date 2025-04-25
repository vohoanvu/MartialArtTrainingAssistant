using System.Text.Json.Serialization;

namespace VideoSharing.Server.Models.Dtos
{
    public class VideoAnalysisRequest
    {
        public required int VideoId { get; set; }
    }

    //For Youtube sharing url
    public class UploadVideoRequest
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
    }
}
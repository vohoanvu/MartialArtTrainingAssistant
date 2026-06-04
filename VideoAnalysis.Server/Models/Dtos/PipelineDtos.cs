using System.Text.Json.Serialization;

namespace VideoAnalysis.Server.Models.Dtos
{
    // ════════════════════════════════════════════════════════════════════════
    //  Vertex response-parse DTOs (snake_case from the model's responseSchema)
    // ════════════════════════════════════════════════════════════════════════

    public class VertexEventsResponse
    {
        [JsonPropertyName("events")]
        public List<VertexEvent> Events { get; set; } = [];
    }

    public class VertexEvent
    {
        [JsonPropertyName("start_timestamp_ms")]
        public int StartTimestampMs { get; set; }

        [JsonPropertyName("end_timestamp_ms")]
        public int EndTimestampMs { get; set; }

        [JsonPropertyName("actor")]
        public string Actor { get; set; } = "Student";

        [JsonPropertyName("technique_category")]
        public string TechniqueCategory { get; set; } = "Transition";

        [JsonPropertyName("technique_name")]
        public string? TechniqueName { get; set; }

        [JsonPropertyName("position_before")]
        public string? PositionBefore { get; set; }

        [JsonPropertyName("position_after")]
        public string? PositionAfter { get; set; }

        [JsonPropertyName("outcome")]
        public string Outcome { get; set; } = "InProgress";

        [JsonPropertyName("guard_type")]
        public string? GuardType { get; set; }

        [JsonPropertyName("submission_type")]
        public string? SubmissionType { get; set; }

        [JsonPropertyName("actions_description")]
        public string? ActionsDescription { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    public class VertexCoachingReport
    {
        [JsonPropertyName("match_summary")]
        public string MatchSummary { get; set; } = string.Empty;

        [JsonPropertyName("key_strengths")]
        public List<VertexStrength> KeyStrengths { get; set; } = [];

        [JsonPropertyName("critical_weaknesses")]
        public List<VertexWeakness> CriticalWeaknesses { get; set; } = [];

        [JsonPropertyName("prescribed_drills")]
        public List<VertexDrill> PrescribedDrills { get; set; } = [];

        [JsonPropertyName("technical_grade")]
        public double TechnicalGrade { get; set; }

        [JsonPropertyName("grade_label")]
        public string? GradeLabel { get; set; }

        [JsonPropertyName("elite_tip")]
        public string? EliteTip { get; set; }
    }

    public class VertexStrength
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("timestamp_start_ms")]
        public int? TimestampStartMs { get; set; }

        [JsonPropertyName("timestamp_end_ms")]
        public int? TimestampEndMs { get; set; }
    }

    public class VertexWeakness
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("timestamp_start_ms")]
        public int? TimestampStartMs { get; set; }

        [JsonPropertyName("timestamp_end_ms")]
        public int? TimestampEndMs { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "Major";

        [JsonPropertyName("scoring_impact")]
        public string? ScoringImpact { get; set; }
    }

    public class VertexDrill
    {
        [JsonPropertyName("drill_name")]
        public string DrillName { get; set; } = string.Empty;

        [JsonPropertyName("instructions")]
        public string? Instructions { get; set; }

        [JsonPropertyName("goal")]
        public string? Goal { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Frontend-facing API DTOs (serialized camelCase; enums exposed as strings
    //  to match the TS union types). Read/write services map enum ↔ string.
    // ════════════════════════════════════════════════════════════════════════

    public class AnalysisV2Dto
    {
        public int? Id { get; set; }
        public int VideoId { get; set; }
        public string? VisualDna { get; set; }
        public string PipelineStatus { get; set; } = "NotStarted";
        public int? TechnicalGrade { get; set; }
        public string? GradeLabel { get; set; }
        public string? EliteTip { get; set; }
        public string? MatchSummary { get; set; }
        public List<MatchEventDto> MatchEvents { get; set; } = [];
        public CoachingReportDto? CoachingReport { get; set; }
    }

    public class MatchEventDto
    {
        public int? Id { get; set; }
        public int StartTimestampMs { get; set; }
        public int EndTimestampMs { get; set; }
        public string Actor { get; set; } = "Student";
        public string TechniqueCategory { get; set; } = "Transition";
        public string? TechniqueName { get; set; }
        public string? PositionBefore { get; set; }
        public string? PositionAfter { get; set; }
        public string Outcome { get; set; } = "InProgress";
        public string? GuardType { get; set; }
        public string? SubmissionType { get; set; }
        public string? ActionsDescription { get; set; }
        public double Confidence { get; set; }
    }

    public class CoachingReportDto
    {
        public int? Id { get; set; }
        public string MatchSummary { get; set; } = string.Empty;
        public int TechnicalGrade { get; set; }
        public string? GradeLabel { get; set; }
        public string? EliteTip { get; set; }
        public List<CoachingStrengthDto> KeyStrengths { get; set; } = [];
        public List<CoachingWeaknessDto> CriticalWeaknesses { get; set; } = [];
        public List<PrescribedDrillDto> PrescribedDrills { get; set; } = [];
    }

    public class CoachingStrengthDto
    {
        public int? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public int? TimestampStartMs { get; set; }
        public int? TimestampEndMs { get; set; }
    }

    public class CoachingWeaknessDto
    {
        public int? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public int? TimestampStartMs { get; set; }
        public int? TimestampEndMs { get; set; }
        public string Severity { get; set; } = "Major";
        public string? ScoringImpact { get; set; }
    }

    public class PrescribedDrillDto
    {
        public int? Id { get; set; }
        public string DrillName { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public string? Goal { get; set; }
    }

    public class AnalysisStatusDto
    {
        public string Status { get; set; } = "NotStarted";
        public string? Error { get; set; }
    }
}

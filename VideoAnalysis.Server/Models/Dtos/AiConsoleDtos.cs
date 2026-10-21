namespace VideoAnalysis.Server.Models.Dtos
{
    /// <summary>Stable keys of the GenAI-driven features configurable from the AI Console.</summary>
    public static class AiFeatureKeys
    {
        public const string VideoAnalysisPipeline = "video-analysis-pipeline";
        public const string VideoAnalysisSingleShot = "video-analysis-single-shot";
        public const string ClassCurriculum = "class-curriculum";
        public const string FighterPairing = "fighter-pairing";
        public const string TechniqueSearch = "technique-search";
    }

    public static class AiFeatureKinds
    {
        public const string Pipeline = "pipeline";
        public const string Generation = "generation";
        public const string Search = "search";
    }

    public sealed record AiFeatureDefinition(string Key, string Kind, bool RequiresVideo, bool RequiresGoogleSearch);

    /// <summary>
    /// Model settings for one feature. Used as the console request/response body AND as the persisted
    /// override (<c>AiFeatureConfig.SettingsJson</c>). Pipeline features carry per-phase knobs in
    /// <see cref="Phases"/> (top-level generation fields are null); search features carry <see cref="Search"/>.
    /// </summary>
    public class AiFeatureSettings
    {
        public string ModelId { get; set; } = string.Empty;
        public string Location { get; set; } = "global";
        public string? ThinkingLevel { get; set; }
        public int? ThinkingBudget { get; set; }
        public float? Temperature { get; set; }
        public int? MaxOutputTokens { get; set; }
        public AiPipelinePhaseSettings? Phases { get; set; }
        public AiSearchSettings? Search { get; set; }
    }

    public class AiPipelinePhaseSettings
    {
        public AiPhaseSettings Profiler { get; set; } = new();
        public AiPhaseSettings EventLogger { get; set; } = new();
        public AiPhaseSettings Verifier { get; set; } = new();
        public AiPhaseSettings HeadCoach { get; set; } = new();

        public IEnumerable<(string Name, AiPhaseSettings Phase)> All() =>
        [
            (nameof(Profiler), Profiler), (nameof(EventLogger), EventLogger),
            (nameof(Verifier), Verifier), (nameof(HeadCoach), HeadCoach),
        ];
    }

    public class AiPhaseSettings
    {
        public string? ThinkingLevel { get; set; }
        public int? ThinkingBudget { get; set; }
        public float? Temperature { get; set; }
        public int? MaxOutputTokens { get; set; }

        /// <summary>Frames per second sampled from the video. 0 = no frame-by-frame work.</summary>
        public int? Fps { get; set; }
    }

    public class AiSearchSettings
    {
        public int MaxResults { get; set; } = 8;

        /// <summary>Drop any YouTube link the model returns that the YouTube Data API can't confirm is public + embeddable.</summary>
        public bool VerifyWithYouTube { get; set; } = true;
    }

    public class AiFeatureConfigDto
    {
        public required string Key { get; set; }
        public required string Kind { get; set; }
        public bool RequiresVideo { get; set; }
        public bool RequiresGoogleSearch { get; set; }
        public bool IsOverridden { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public required AiFeatureSettings Defaults { get; set; }
        public required AiFeatureSettings Effective { get; set; }
    }

    public class AiModelInfoDto
    {
        public required string Id { get; set; }
        public required string DisplayName { get; set; }
        public required string LaunchStage { get; set; }
        public required string ThinkingMode { get; set; }
        public bool SupportsExplicitCache { get; set; }
        public bool SupportsVideo { get; set; }
        public bool SupportsGoogleSearch { get; set; }
        public bool SamplingParamsIgnored { get; set; }
        public required IReadOnlyList<string> SupportedLocations { get; set; }
        public string? Notes { get; set; }
    }

    public class AiConsoleOverviewDto
    {
        public required IReadOnlyList<AiModelInfoDto> Models { get; set; }
        public required IReadOnlyList<AiFeatureConfigDto> Features { get; set; }
    }

    public class AiModelTestResultDto
    {
        public bool Ok { get; set; }
        public required string ModelId { get; set; }
        public long LatencyMs { get; set; }
        public string? ResponseModelVersion { get; set; }
        public string? Sample { get; set; }
        public string? Error { get; set; }
    }

    public class AiConsoleAccessDto
    {
        public bool IsAdmin { get; set; }
    }
}

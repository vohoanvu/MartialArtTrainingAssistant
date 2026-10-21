namespace VideoAnalysis.Server.Configuration
{
    /// <summary>
    /// Defaults for the grounded technique-resource search (replaces the xAI Grok live search).
    /// Bound from <c>AiFeatures:TechniqueSearch</c>; an AI Console override wins.
    /// </summary>
    public class TechniqueSearchOptions
    {
        public const string SectionName = "AiFeatures:TechniqueSearch";

        public string ModelId { get; set; } = "gemini-4-argon";
        public string Location { get; set; } = "global";
        public string? ThinkingLevel { get; set; } = "LOW";
        public int MaxOutputTokens { get; set; } = 8192;
        public int MaxResults { get; set; } = 8;
        public bool VerifyWithYouTube { get; set; } = true;
    }

    /// <summary>
    /// Free-demo video-analysis quota defaults. Bound from <c>AnalysisQuota</c> (env <c>AnalysisQuota__*</c>);
    /// an AI Console override wins. AI Console admins are exempt.
    /// </summary>
    public class AnalysisQuotaOptions
    {
        public const string SectionName = "AnalysisQuota";

        public bool Enabled { get; set; } = true;

        /// <summary>Analyses a user may start per rolling window. 0 = analyses disabled for non-admins.</summary>
        public int MaxPerWindow { get; set; } = 3;

        public int WindowHours { get; set; } = 24;
    }

    /// <summary>Who may use the AI Console. Bound from <c>AiConsole</c> (env <c>AiConsole__AdminEmails</c>).</summary>
    public class AiConsoleOptions
    {
        public const string SectionName = "AiConsole";

        /// <summary>Comma/semicolon-separated admin emails. Empty = nobody (console disabled).</summary>
        public string AdminEmails { get; set; } = string.Empty;

        public IReadOnlySet<string> AdminEmailSet() =>
            AdminEmails.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

namespace VideoAnalysis.Server.Configuration
{
    /// <summary>
    /// Strongly-typed options for the agentic (multi-step) Vertex AI video-analysis pipeline.
    /// Bound from the <c>VertexAi:Pipeline</c> configuration section. Environment overrides use
    /// the standard ASP.NET nesting delimiter, e.g. <c>VertexAi__Pipeline__Location</c>.
    /// </summary>
    public class VertexAiPipelineOptions
    {
        public const string SectionName = "VertexAi:Pipeline";

        /// <summary>Feature flag. When false, callers fall back to the single-shot v1 path.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Vertex location used for the pipeline. The proven MyCoach mobile pipeline runs context
        /// caching on the GLOBAL endpoint (https://aiplatform.googleapis.com + locations/global/cachedContents),
        /// validated with real uploads — so "global" is the default and matches the single-shot path.
        /// </summary>
        public string Location { get; set; } = "global";

        /// <summary>Context-cache TTL (minutes). Must exceed expected total pipeline runtime.</summary>
        public int ContextCacheTtlMinutes { get; set; } = 30;

        /// <summary>Events with confidence below this are flagged for review in the UI.</summary>
        public double ConfidenceThresholdForReview { get; set; } = 0.7;

        /// <summary>Caps concurrent pipeline jobs (dedicated Hangfire queue worker count).</summary>
        public int MaxConcurrentJobs { get; set; } = 2;

        public PhaseModelOptions Profiler { get; set; } = new() { Fps = 1, Temperature = 0.1f, MaxOutputTokens = 1024, ThinkingBudget = 8192 };
        public PhaseModelOptions EventLogger { get; set; } = new() { Fps = 4, Temperature = 0.2f, MaxOutputTokens = 65535, ThinkingBudget = 16384 };
        public PhaseModelOptions Verifier { get; set; } = new() { Fps = 0, Temperature = 0.1f, MaxOutputTokens = 65535, ThinkingBudget = 4096 };
        public PhaseModelOptions HeadCoach { get; set; } = new() { Fps = 0, Temperature = 0.4f, MaxOutputTokens = 8192, ThinkingBudget = 16384 };
    }

    /// <summary>Per-phase Vertex model + generation settings.</summary>
    public class PhaseModelOptions
    {
        public string ModelId { get; set; } = "gemini-3.1-pro-preview";

        /// <summary>Frames-per-second for video sampling. 0 = phase does not do frame-by-frame work.</summary>
        public int Fps { get; set; }

        public float Temperature { get; set; }

        public int MaxOutputTokens { get; set; }

        /// <summary>Thinking-token budget. 0 disables thinking for the phase.</summary>
        public int ThinkingBudget { get; set; }
    }
}

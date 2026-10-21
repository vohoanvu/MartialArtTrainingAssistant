namespace VideoAnalysis.Server.Configuration
{
    /// <summary>
    /// Strongly-typed options for the agentic (multi-step) Vertex AI video-analysis pipeline.
    /// Bound from the <c>VertexAi:Pipeline</c> configuration section. Environment overrides use
    /// the standard ASP.NET nesting delimiter, e.g. <c>VertexAi__Pipeline__Location</c>.
    /// These are the DEFAULTS — an admin override saved in the AI Console (AiFeatureConfigs table) wins.
    /// </summary>
    public class VertexAiPipelineOptions
    {
        public const string SectionName = "VertexAi:Pipeline";

        /// <summary>Feature flag. When false, callers fall back to the single-shot v1 path.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Vertex location used for the pipeline. The proven MyCoach mobile pipeline runs on the GLOBAL
        /// endpoint (https://aiplatform.googleapis.com), and Gemini 4 Argon is served from global/us/eu only.
        /// </summary>
        public string Location { get; set; } = "global";

        /// <summary>Context-cache TTL (minutes). Must exceed expected total pipeline runtime. Only used by models with explicit caching.</summary>
        public int ContextCacheTtlMinutes { get; set; } = 30;

        /// <summary>Events with confidence below this are flagged for review in the UI.</summary>
        public double ConfidenceThresholdForReview { get; set; } = 0.7;

        /// <summary>Caps concurrent pipeline jobs (dedicated Hangfire queue worker count). Startup-only.</summary>
        public int MaxConcurrentJobs { get; set; } = 2;

        /// <summary>
        /// The ONE model every phase runs on. Sharing a model is what lets phases reuse the video: an explicit
        /// context cache on models that support it (cache ↔ inference model must match), or Vertex's implicit
        /// prefix cache on models that don't (Gemini 4 Argon). Rollback: <c>VertexAi__Pipeline__ModelId=gemini-3.1-pro-preview</c>
        /// or pick it in the AI Console.
        /// </summary>
        public string ModelId { get; set; } = "gemini-4-argon";

        // Per-phase generation settings. ThinkingLevel applies to level-mode models (Gemini 4); ThinkingBudget to
        // budget-mode models (Gemini 3.x) — the request builder translates whichever one the model doesn't take.
        // Output caps leave headroom for thinking tokens; the Verifier stays cheap, the Head Coach thinks hardest.
        public PhaseModelOptions Profiler { get; set; } = new() { Fps = 1, Temperature = 0.1f, MaxOutputTokens = 4096, ThinkingBudget = 8192, ThinkingLevel = "MEDIUM" };
        public PhaseModelOptions EventLogger { get; set; } = new() { Fps = 4, Temperature = 0.2f, MaxOutputTokens = 16384, ThinkingBudget = 8192, ThinkingLevel = "MEDIUM" };
        public PhaseModelOptions Verifier { get; set; } = new() { Fps = 0, Temperature = 0.1f, MaxOutputTokens = 16384, ThinkingBudget = 4096, ThinkingLevel = "LOW" };
        public PhaseModelOptions HeadCoach { get; set; } = new() { Fps = 0, Temperature = 0.4f, MaxOutputTokens = 16384, ThinkingBudget = 16384, ThinkingLevel = "HIGH" };
    }

    /// <summary>Per-phase Vertex generation settings.</summary>
    public class PhaseModelOptions
    {
        /// <summary>Frames-per-second for video sampling. 0 = phase does not do frame-by-frame work.</summary>
        public int Fps { get; set; }

        /// <summary>Ignored by models whose backend ignores sampling params (Gemini 4 Argon).</summary>
        public float Temperature { get; set; }

        public int MaxOutputTokens { get; set; }

        /// <summary>Thinking-token budget for budget-mode models. 0 disables thinking for the phase.</summary>
        public int ThinkingBudget { get; set; }

        /// <summary>LOW / MEDIUM / HIGH for level-mode models.</summary>
        public string? ThinkingLevel { get; set; }
    }
}

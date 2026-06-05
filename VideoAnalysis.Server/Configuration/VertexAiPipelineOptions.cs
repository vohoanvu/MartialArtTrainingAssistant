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

        /// <summary>
        /// Shared model for ALL phases. Each phase inherits this unless it sets its own
        /// <see cref="PhaseModelOptions.ModelId"/>. Keeping every phase on ONE model is what lets the
        /// Vertex multimodal context cache be reused across phases (cache ↔ inference model must match),
        /// so this is the single knob to point the whole pipeline at a different model — e.g. flip to
        /// Gemini 3.5 Flash with <c>VertexAi__Pipeline__ModelId=gemini-3.5-flash</c> (no image rebuild).
        /// </summary>
        public string ModelId { get; set; } = "gemini-3.1-pro-preview";

        // Per-phase generation settings. ModelId is intentionally left blank so each phase inherits the
        // shared ModelId above (via Normalize); set a phase ModelId only to deliberately diverge — which
        // disables cross-phase context caching. Token/thinking budgets are tuned for speed while
        // protecting the accuracy-critical phases (Profiler visual-DNA, Verifier) and the Head Coach.
        public PhaseModelOptions Profiler { get; set; } = new() { Fps = 1, Temperature = 0.1f, MaxOutputTokens = 1024, ThinkingBudget = 8192 };
        public PhaseModelOptions EventLogger { get; set; } = new() { Fps = 4, Temperature = 0.2f, MaxOutputTokens = 16384, ThinkingBudget = 8192 };
        public PhaseModelOptions Verifier { get; set; } = new() { Fps = 0, Temperature = 0.1f, MaxOutputTokens = 16384, ThinkingBudget = 4096 };
        public PhaseModelOptions HeadCoach { get; set; } = new() { Fps = 0, Temperature = 0.4f, MaxOutputTokens = 8192, ThinkingBudget = 16384 };

        /// <summary>
        /// Fills any phase whose <see cref="PhaseModelOptions.ModelId"/> was left blank with the shared
        /// <see cref="ModelId"/>. Call AFTER binding configuration (see Program.cs PostConfigure) so an
        /// env/appsettings override of the shared model propagates to every phase.
        /// </summary>
        public void Normalize()
        {
            foreach (var phase in new[] { Profiler, EventLogger, Verifier, HeadCoach })
            {
                if (string.IsNullOrWhiteSpace(phase.ModelId))
                    phase.ModelId = ModelId;
            }
        }
    }

    /// <summary>Per-phase Vertex model + generation settings.</summary>
    public class PhaseModelOptions
    {
        /// <summary>Optional per-phase model override. Blank = inherit the pipeline-level shared ModelId.</summary>
        public string ModelId { get; set; } = "";

        /// <summary>Frames-per-second for video sampling. 0 = phase does not do frame-by-frame work.</summary>
        public int Fps { get; set; }

        public float Temperature { get; set; }

        public int MaxOutputTokens { get; set; }

        /// <summary>Thinking-token budget. 0 disables thinking for the phase.</summary>
        public int ThinkingBudget { get; set; }
    }
}

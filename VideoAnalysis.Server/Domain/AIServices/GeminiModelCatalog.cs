using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace VideoAnalysis.Server.Domain.AIServices
{
    /// <summary>How a model accepts its thinking configuration.</summary>
    public enum ThinkingMode
    {
        /// <summary>Numeric <c>thinkingConfig.thinkingBudget</c> (Gemini 2.5 / 3.x).</summary>
        Budget,
        /// <summary>Enumerated <c>thinkingConfig.thinkingLevel</c> LOW/MEDIUM/HIGH. A budget is rejected with HTTP 400 (Gemini 4).</summary>
        Level,
    }

    /// <summary>Request-shape capabilities of a Vertex AI Gemini model.</summary>
    public sealed record GeminiModelInfo(
        string Id,
        string DisplayName,
        string LaunchStage,
        ThinkingMode ThinkingMode,
        bool SupportsExplicitCache,
        bool SupportsVideo,
        bool SupportsGoogleSearch,
        bool SamplingParamsIgnored,
        IReadOnlyList<string> SupportedLocations,
        string? Notes,
        bool IsKnown = true);

    /// <summary>
    /// Known Vertex AI models and their request-shape rules, so callers build a valid
    /// <c>generationConfig</c> for whichever model the admin console selects. Unknown (custom) model ids
    /// get capabilities inferred from their family prefix.
    /// </summary>
    public static class GeminiModelCatalog
    {
        public const string Gemini4Argon = "gemini-4-argon";

        public static readonly string[] ThinkingLevels = ["LOW", "MEDIUM", "HIGH"];

        private static readonly Regex ModelIdPattern = new(@"^[a-z0-9][a-z0-9.\-]{1,62}$", RegexOptions.Compiled);
        private static readonly Regex LocationPattern = new(@"^(global|[a-z]+-[a-z]+[0-9]{1,2})$", RegexOptions.Compiled);

        public static IReadOnlyList<GeminiModelInfo> Models { get; } =
        [
            // GA 2026-10-13. Thinking levels only (thinking_budget / MINIMAL → HTTP 400), no explicit
            // context caching (implicit only, ≥6,144-token shared prefix), temperature/top_p/top_k are
            // ignored by the backend. Vertex locations: global, us, eu — only global is wired here.
            new(Gemini4Argon, "Gemini 4 Argon", "GA", ThinkingMode.Level,
                SupportsExplicitCache: false, SupportsVideo: true, SupportsGoogleSearch: true,
                SamplingParamsIgnored: true, SupportedLocations: ["global"],
                Notes: "Thinking levels only; implicit caching only (the shared video prefix is discounted automatically); temperature is ignored."),
            new("gemini-3.1-pro-preview", "Gemini 3.1 Pro (Preview)", "Preview", ThinkingMode.Budget,
                SupportsExplicitCache: true, SupportsVideo: true, SupportsGoogleSearch: true,
                SamplingParamsIgnored: false, SupportedLocations: ["global"],
                Notes: "Previous agentic-pipeline model; supports explicit context caching across phases."),
            new("gemini-3.1-flash-lite-preview", "Gemini 3.1 Flash-Lite (Preview)", "Preview", ThinkingMode.Budget,
                SupportsExplicitCache: true, SupportsVideo: true, SupportsGoogleSearch: true,
                SamplingParamsIgnored: false, SupportedLocations: ["global"],
                Notes: "Low-cost text model used by curriculum and pairing."),
        ];

        public static bool IsValidModelId(string? modelId) => modelId != null && ModelIdPattern.IsMatch(modelId);

        public static bool IsValidLocation(string? location) => location != null && LocationPattern.IsMatch(location);

        /// <summary>Catalog entry for <paramref name="modelId"/>, or capabilities inferred from its family.</summary>
        public static GeminiModelInfo Resolve(string modelId)
        {
            var known = Models.FirstOrDefault(m => string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));
            if (known != null) return known;

            // Gemini 4+ follows the Argon request rules; anything else keeps the 3.x (budget) shape.
            bool isGen4Plus = Regex.IsMatch(modelId, @"^gemini-([4-9]|\d{2,})");
            return new GeminiModelInfo(modelId, modelId, "Custom",
                isGen4Plus ? ThinkingMode.Level : ThinkingMode.Budget,
                SupportsExplicitCache: !isGen4Plus, SupportsVideo: true, SupportsGoogleSearch: true,
                SamplingParamsIgnored: isGen4Plus, SupportedLocations: [], Notes: null, IsKnown: false);
        }

        public static string BudgetToLevel(int budget) => budget switch
        {
            <= 4096 => "LOW",
            <= 12288 => "MEDIUM",
            _ => "HIGH",
        };

        public static int LevelToBudget(string level) => level.ToUpperInvariant() switch
        {
            "LOW" => 2048,
            "HIGH" => 16384,
            _ => 8192,
        };

        /// <summary>
        /// Builds a <c>generationConfig</c> that is valid for <paramref name="model"/>: emits
        /// <c>thinkingLevel</c> or <c>thinkingBudget</c> per the model's <see cref="ThinkingMode"/> (translating
        /// whichever one was configured), and omits sampling params the model ignores. Keys are added only
        /// when set, so nothing is sent as JSON null.
        /// </summary>
        public static Dictionary<string, object> BuildGenerationConfig(
            GeminiModelInfo model,
            string? thinkingLevel,
            int? thinkingBudget,
            float? temperature,
            int? maxOutputTokens,
            float? topP = null,
            string? responseMimeType = null,
            JsonNode? responseSchema = null)
        {
            var config = new Dictionary<string, object>();

            if (!model.SamplingParamsIgnored)
            {
                if (temperature is { } t) config["temperature"] = t;
                if (topP is { } p) config["topP"] = p;
            }
            if (maxOutputTokens is { } max) config["maxOutputTokens"] = max;
            if (responseMimeType != null) config["responseMimeType"] = responseMimeType;
            if (responseSchema != null) config["responseSchema"] = responseSchema;

            if (model.ThinkingMode == ThinkingMode.Level)
            {
                var level = !string.IsNullOrWhiteSpace(thinkingLevel)
                    ? thinkingLevel.ToUpperInvariant()
                    : thinkingBudget is { } b ? BudgetToLevel(b) : null;
                if (level != null) config["thinkingConfig"] = new Dictionary<string, object> { ["thinkingLevel"] = level };
            }
            else
            {
                int? budget = thinkingBudget
                    ?? (!string.IsNullOrWhiteSpace(thinkingLevel) ? LevelToBudget(thinkingLevel) : null);
                if (budget != null) config["thinkingConfig"] = new Dictionary<string, object> { ["thinkingBudget"] = budget.Value };
            }

            return config;
        }
    }
}

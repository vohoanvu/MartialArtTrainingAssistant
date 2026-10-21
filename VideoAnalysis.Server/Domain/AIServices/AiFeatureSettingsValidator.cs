using VideoAnalysis.Server.Models.Dtos;

namespace VideoAnalysis.Server.Domain.AIServices
{
    /// <summary>
    /// Validates + normalizes AI Console settings before they are tested or persisted. ModelId and Location
    /// are interpolated into the Vertex URL/host, so both are strictly pattern-checked.
    /// </summary>
    public static class AiFeatureSettingsValidator
    {
        public const int MaxThinkingBudget = 32768;
        public const int MaxOutputTokensLimit = 65536;
        public const int MaxFps = 10;
        public const int MaxSearchResults = 20;

        /// <summary>Returns field errors keyed by camelCase path (empty = valid). Normalizes <paramref name="s"/> in place.</summary>
        public static Dictionary<string, string[]> Validate(AiFeatureDefinition feature, AiFeatureSettings s)
        {
            var errors = new Dictionary<string, List<string>>();
            void Add(string field, string message)
            {
                if (!errors.TryGetValue(field, out var list)) errors[field] = list = [];
                list.Add(message);
            }

            s.ModelId = (s.ModelId ?? string.Empty).Trim().ToLowerInvariant();
            s.Location = (s.Location ?? string.Empty).Trim().ToLowerInvariant();

            if (!GeminiModelCatalog.IsValidModelId(s.ModelId))
            {
                Add("modelId", "Model id must be 2-63 chars of lowercase letters, digits, '.' or '-'.");
            }
            else
            {
                var model = GeminiModelCatalog.Resolve(s.ModelId);
                if (feature.RequiresVideo && !model.SupportsVideo)
                    Add("modelId", $"{model.DisplayName} does not accept video input.");
                if (feature.RequiresGoogleSearch && !model.SupportsGoogleSearch)
                    Add("modelId", $"{model.DisplayName} does not support Grounding with Google Search.");
                if (GeminiModelCatalog.IsValidLocation(s.Location) && model.SupportedLocations.Count > 0
                    && !model.SupportedLocations.Contains(s.Location))
                    Add("location", $"{model.DisplayName} is available in: {string.Join(", ", model.SupportedLocations)}.");
            }

            if (!GeminiModelCatalog.IsValidLocation(s.Location))
                Add("location", "Location must be 'global' or a region such as 'us-central1'.");

            if (feature.Kind == AiFeatureKinds.Pipeline)
            {
                s.ThinkingLevel = null; s.ThinkingBudget = null; s.Temperature = null; s.MaxOutputTokens = null;
                if (s.Phases == null)
                {
                    Add("phases", "Pipeline settings require per-phase values.");
                }
                else
                {
                    foreach (var (name, phase) in s.Phases.All())
                    {
                        var prefix = $"phases.{char.ToLowerInvariant(name[0])}{name[1..]}";
                        phase.ThinkingLevel = ValidateGeneration(prefix, phase.ThinkingLevel, phase.ThinkingBudget, phase.Temperature, phase.MaxOutputTokens, Add);
                        if (phase.Fps is < 0 or > MaxFps) Add($"{prefix}.fps", $"FPS must be between 0 and {MaxFps}.");
                    }
                }
            }
            else
            {
                s.Phases = null;
                s.ThinkingLevel = ValidateGeneration(null, s.ThinkingLevel, s.ThinkingBudget, s.Temperature, s.MaxOutputTokens, Add);
            }

            if (feature.Kind == AiFeatureKinds.Search)
            {
                s.Search ??= new AiSearchSettings();
                if (s.Search.MaxResults is < 1 or > MaxSearchResults)
                    Add("search.maxResults", $"Max results must be between 1 and {MaxSearchResults}.");
            }
            else
            {
                s.Search = null;
            }

            return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
        }

        private static string? ValidateGeneration(string? prefix, string? thinkingLevel, int? thinkingBudget, float? temperature, int? maxOutputTokens, Action<string, string> add)
        {
            string Field(string name) => prefix == null ? name : $"{prefix}.{name}";

            var level = string.IsNullOrWhiteSpace(thinkingLevel) ? null : thinkingLevel.Trim().ToUpperInvariant();
            if (level != null && !GeminiModelCatalog.ThinkingLevels.Contains(level))
                add(Field("thinkingLevel"), "Thinking level must be LOW, MEDIUM or HIGH.");
            if (thinkingBudget is < 0 or > MaxThinkingBudget)
                add(Field("thinkingBudget"), $"Thinking budget must be between 0 and {MaxThinkingBudget}.");
            if (temperature is < 0f or > 2f)
                add(Field("temperature"), "Temperature must be between 0 and 2.");
            if (maxOutputTokens is < 1 or > MaxOutputTokensLimit)
                add(Field("maxOutputTokens"), $"Max output tokens must be between 1 and {MaxOutputTokensLimit}.");
            return level;
        }
    }
}

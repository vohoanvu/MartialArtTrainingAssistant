using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Models.Dtos;

namespace VideoAnalysis.Server.Domain.AIServices
{
    /// <summary>
    /// Resolves the model settings each GenAI feature runs with: the AI Console override stored in the
    /// <c>AiFeatureConfigs</c> table when present, else the appsettings/env defaults. Overrides are cached
    /// briefly and invalidated on save, so a console change applies to the next request/pipeline run.
    /// </summary>
    public interface IAiModelConfigService
    {
        IReadOnlyList<AiFeatureDefinition> Features { get; }
        AiFeatureDefinition? FindFeature(string key);
        AiFeatureSettings GetDefaults(string key);
        Task<AiFeatureSettings> GetEffectiveAsync(string key, CancellationToken ct = default);
        Task<AiFeatureConfigDto> GetConfigAsync(string key, CancellationToken ct = default);
        Task<AiFeatureConfigDto> SaveOverrideAsync(string key, AiFeatureSettings settings, string? updatedBy, CancellationToken ct = default);
        Task<AiFeatureConfigDto> ResetAsync(string key, CancellationToken ct = default);
    }

    /// <inheritdoc cref="IAiModelConfigService"/>
    public class AiModelConfigService : IAiModelConfigService
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static readonly IReadOnlyList<AiFeatureDefinition> FeatureDefinitions =
        [
            new(AiFeatureKeys.VideoAnalysisPipeline, AiFeatureKinds.Pipeline, RequiresVideo: true, RequiresGoogleSearch: false),
            new(AiFeatureKeys.VideoAnalysisSingleShot, AiFeatureKinds.Generation, RequiresVideo: true, RequiresGoogleSearch: false),
            new(AiFeatureKeys.ClassCurriculum, AiFeatureKinds.Generation, RequiresVideo: false, RequiresGoogleSearch: false),
            new(AiFeatureKeys.FighterPairing, AiFeatureKinds.Generation, RequiresVideo: false, RequiresGoogleSearch: false),
            // TechniqueSearch is no longer listed: it is a plain YouTube Data API search now (no model to configure).
        ];

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly VertexAiPipelineOptions _pipelineOptions;
        private readonly TechniqueSearchOptions _searchOptions;
        private readonly ILogger<AiModelConfigService> _logger;

        public AiModelConfigService(
            IServiceScopeFactory scopeFactory,
            IMemoryCache cache,
            IConfiguration configuration,
            IOptions<VertexAiPipelineOptions> pipelineOptions,
            IOptions<TechniqueSearchOptions> searchOptions,
            ILogger<AiModelConfigService> logger)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            _configuration = configuration;
            _pipelineOptions = pipelineOptions.Value;
            _searchOptions = searchOptions.Value;
            _logger = logger;
        }

        public IReadOnlyList<AiFeatureDefinition> Features => FeatureDefinitions;

        public AiFeatureDefinition? FindFeature(string key) => FeatureDefinitions.FirstOrDefault(f => f.Key == key);

        // ── Defaults (appsettings / env) ─────────────────────────────────────

        public AiFeatureSettings GetDefaults(string key) => key switch
        {
            AiFeatureKeys.VideoAnalysisPipeline => PipelineDefaults(_pipelineOptions),
            AiFeatureKeys.VideoAnalysisSingleShot => new AiFeatureSettings
            {
                ModelId = ConfigValue("GeminiVision:Model", "GEMINI_VISION_MODEL", "gemini-3.1-pro-preview"),
                Location = ConfigValue("GeminiVision:Location", "GEMINI_VISION_LOCATION", "global"),
                ThinkingBudget = 16384,
                Temperature = 0.3f,
                MaxOutputTokens = 65535,
            },
            AiFeatureKeys.ClassCurriculum => TextDefaults(temperature: 1.0f, maxOutputTokens: 16384),
            AiFeatureKeys.FighterPairing => TextDefaults(temperature: 0.2f, maxOutputTokens: 8192),
            AiFeatureKeys.TechniqueSearch => new AiFeatureSettings
            {
                ModelId = _searchOptions.ModelId.Trim(),
                Location = _searchOptions.Location.Trim(),
                ThinkingLevel = _searchOptions.ThinkingLevel,
                MaxOutputTokens = _searchOptions.MaxOutputTokens,
                Search = new AiSearchSettings { MaxResults = _searchOptions.MaxResults, VerifyWithYouTube = _searchOptions.VerifyWithYouTube },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown AI feature key."),
        };

        /// <summary>Maps the pipeline appsettings/env options onto console settings (shared by tests).</summary>
        public static AiFeatureSettings PipelineDefaults(VertexAiPipelineOptions o) => new()
        {
            ModelId = o.ModelId.Trim(),
            Location = o.Location.Trim(),
            Phases = new AiPipelinePhaseSettings
            {
                Profiler = ToPhase(o.Profiler),
                EventLogger = ToPhase(o.EventLogger),
                Verifier = ToPhase(o.Verifier),
                HeadCoach = ToPhase(o.HeadCoach),
            },
        };

        private static AiPhaseSettings ToPhase(PhaseModelOptions p) => new()
        {
            ThinkingLevel = p.ThinkingLevel,
            ThinkingBudget = p.ThinkingBudget,
            Temperature = p.Temperature,
            MaxOutputTokens = p.MaxOutputTokens,
            Fps = p.Fps,
        };

        private AiFeatureSettings TextDefaults(float temperature, int maxOutputTokens) => new()
        {
            ModelId = ConfigValue("GeminiVision:TextModel", null, "gemini-3.1-flash-lite-preview"),
            Location = ConfigValue("GeminiVision:Location", "GEMINI_VISION_LOCATION", "global"),
            Temperature = temperature,
            MaxOutputTokens = maxOutputTokens,
        };

        // Same precedence as Global.AccessAppEnvironmentVariable (container env var, then configuration), with
        // the CR/LF trim the VM .env needs, but falling back to a default instead of throwing.
        private string ConfigValue(string configKey, string? containerEnvVar, string fallback)
        {
            var value = containerEnvVar != null ? Environment.GetEnvironmentVariable(containerEnvVar) : null;
            if (string.IsNullOrWhiteSpace(value)) value = _configuration[configKey];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        // ── Effective (override ?? defaults) ─────────────────────────────────

        public async Task<AiFeatureSettings> GetEffectiveAsync(string key, CancellationToken ct = default)
        {
            var row = await GetOverrideAsync(key, ct);
            return row != null ? Deserialize(row.SettingsJson) ?? GetDefaults(key) : GetDefaults(key);
        }

        public async Task<AiFeatureConfigDto> GetConfigAsync(string key, CancellationToken ct = default)
        {
            var def = FindFeature(key) ?? throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown AI feature key.");
            var row = await GetOverrideAsync(key, ct);
            var defaults = GetDefaults(key);
            var overridden = row != null ? Deserialize(row.SettingsJson) : null;
            return new AiFeatureConfigDto
            {
                Key = def.Key,
                Kind = def.Kind,
                RequiresVideo = def.RequiresVideo,
                RequiresGoogleSearch = def.RequiresGoogleSearch,
                IsOverridden = overridden != null,
                UpdatedAt = overridden != null ? row!.UpdatedAt : null,
                UpdatedBy = overridden != null ? row!.UpdatedBy : null,
                Defaults = defaults,
                Effective = overridden ?? defaults,
            };
        }

        public async Task<AiFeatureConfigDto> SaveOverrideAsync(string key, AiFeatureSettings settings, string? updatedBy, CancellationToken ct = default)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
                var row = await db.AiFeatureConfigs.FirstOrDefaultAsync(c => c.FeatureKey == key, ct);
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                if (row == null)
                {
                    db.AiFeatureConfigs.Add(new AiFeatureConfig { FeatureKey = key, SettingsJson = json, UpdatedBy = updatedBy });
                }
                else
                {
                    row.SettingsJson = json;
                    row.UpdatedAt = DateTime.UtcNow;
                    row.UpdatedBy = updatedBy;
                }
                await db.SaveChangesAsync(ct);
            }
            _cache.Remove(CacheKey(key));
            _logger.LogInformation("AI Console: {User} set {Feature} to model {Model} ({Location}).", updatedBy, key, settings.ModelId, settings.Location);
            return await GetConfigAsync(key, ct);
        }

        public async Task<AiFeatureConfigDto> ResetAsync(string key, CancellationToken ct = default)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
                var row = await db.AiFeatureConfigs.FirstOrDefaultAsync(c => c.FeatureKey == key, ct);
                if (row != null)
                {
                    db.AiFeatureConfigs.Remove(row);
                    await db.SaveChangesAsync(ct);
                }
            }
            _cache.Remove(CacheKey(key));
            _logger.LogInformation("AI Console: {Feature} reset to defaults.", key);
            return await GetConfigAsync(key, ct);
        }

        // ── Storage ──────────────────────────────────────────────────────────

        private static string CacheKey(string key) => $"ai-feature-config:{key}";

        private async Task<AiFeatureConfig?> GetOverrideAsync(string key, CancellationToken ct)
        {
            if (_cache.TryGetValue(CacheKey(key), out AiFeatureConfig? cached)) return cached;

            AiFeatureConfig? row;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MyDatabaseContext>();
                row = await db.AiFeatureConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.FeatureKey == key, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A DB hiccup (or the table not migrated yet) must never take an AI feature down — run on defaults.
                _logger.LogWarning(ex, "Could not read AI Console override for {Feature}; using defaults.", key);
                return null;
            }

            _cache.Set(CacheKey(key), row, CacheTtl);
            return row;
        }

        private AiFeatureSettings? Deserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<AiFeatureSettings>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Ignoring unreadable AI Console override JSON.");
                return null;
            }
        }
    }
}

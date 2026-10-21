using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using CodeJitsu.Tests.Helpers;
using SharedEntities.Data;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Models.Dtos;

namespace CodeJitsu.Tests.VideoAnalysis.Services;

/// <summary>Model catalog request shaping, console settings validation, and override persistence.</summary>
public class AiModelConfigurationTests
{
    // ── GeminiModelCatalog ───────────────────────────────────────────────

    [Fact]
    public void Should_EmitThinkingLevel_AndDropSampling_ForArgon()
    {
        var argon = GeminiModelCatalog.Resolve("gemini-4-argon");

        var config = GeminiModelCatalog.BuildGenerationConfig(argon, null, thinkingBudget: 16384, temperature: 0.4f, maxOutputTokens: 8192, topP: 1.0f);

        var json = JsonSerializer.SerializeToElement(config);
        Assert.Equal("HIGH", json.GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString()); // budget translated
        Assert.False(json.GetProperty("thinkingConfig").TryGetProperty("thinkingBudget", out _));
        Assert.False(json.TryGetProperty("temperature", out _));
        Assert.False(json.TryGetProperty("topP", out _));
        Assert.Equal(8192, json.GetProperty("maxOutputTokens").GetInt32());
        Assert.False(argon.SupportsExplicitCache);
    }

    [Fact]
    public void Should_EmitThinkingBudget_ForGemini3()
    {
        var pro = GeminiModelCatalog.Resolve("gemini-3.1-pro-preview");

        var config = GeminiModelCatalog.BuildGenerationConfig(pro, thinkingLevel: "low", thinkingBudget: null, temperature: 0.2f, maxOutputTokens: null);

        var json = JsonSerializer.SerializeToElement(config);
        Assert.Equal(2048, json.GetProperty("thinkingConfig").GetProperty("thinkingBudget").GetInt32()); // level translated
        Assert.Equal(0.2f, json.GetProperty("temperature").GetSingle());
        Assert.False(json.TryGetProperty("maxOutputTokens", out _));
    }

    [Theory]
    [InlineData("gemini-4-flash", ThinkingMode.Level, false)]
    [InlineData("gemini-10-pro", ThinkingMode.Level, false)]
    [InlineData("gemini-3.5-flash", ThinkingMode.Budget, true)]
    public void Should_InferCapabilities_ForCustomModels(string id, ThinkingMode mode, bool explicitCache)
    {
        var info = GeminiModelCatalog.Resolve(id);
        Assert.False(info.IsKnown);
        Assert.Equal(mode, info.ThinkingMode);
        Assert.Equal(explicitCache, info.SupportsExplicitCache);
    }

    // ── AiFeatureSettingsValidator ───────────────────────────────────────

    private static readonly AiFeatureDefinition Pipeline = new(AiFeatureKeys.VideoAnalysisPipeline, AiFeatureKinds.Pipeline, true, false);
    private static readonly AiFeatureDefinition Search = new(AiFeatureKeys.TechniqueSearch, AiFeatureKinds.Search, false, true);

    [Theory]
    [InlineData("gemini-4-argon/../../x", "global")]
    [InlineData("gemini-4-argon", "evil.com/x")]
    [InlineData("GEMINI 4", "global")]
    public void Should_RejectModelOrLocation_ThatCouldAlterTheVertexUrl(string modelId, string location)
    {
        var settings = AiModelConfigService.PipelineDefaults(new VertexAiPipelineOptions());
        settings.ModelId = modelId;
        settings.Location = location;

        var errors = AiFeatureSettingsValidator.Validate(Pipeline, settings);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Should_RejectUnsupportedLocation_ForKnownModel()
    {
        var settings = AiModelConfigService.PipelineDefaults(new VertexAiPipelineOptions());
        settings.Location = "us-central1";

        var errors = AiFeatureSettingsValidator.Validate(Pipeline, settings);

        Assert.Contains("location", errors.Keys);
    }

    [Fact]
    public void Should_ReportPhaseFieldErrors_WithCamelCasePaths()
    {
        var settings = AiModelConfigService.PipelineDefaults(new VertexAiPipelineOptions());
        settings.Phases!.EventLogger.MaxOutputTokens = 100_000;
        settings.Phases.HeadCoach.ThinkingLevel = "MINIMAL";
        settings.Phases.Profiler.Fps = 30;

        var errors = AiFeatureSettingsValidator.Validate(Pipeline, settings);

        Assert.Contains("phases.eventLogger.maxOutputTokens", errors.Keys);
        Assert.Contains("phases.headCoach.thinkingLevel", errors.Keys);
        Assert.Contains("phases.profiler.fps", errors.Keys);
    }

    [Fact]
    public void Should_NormalizeSettings_ForFeatureKind()
    {
        var settings = new AiFeatureSettings
        {
            ModelId = " Gemini-4-Argon ",
            Location = "GLOBAL",
            ThinkingLevel = "medium",
            Phases = new AiPipelinePhaseSettings(),
        };

        var errors = AiFeatureSettingsValidator.Validate(Search, settings);

        Assert.Empty(errors);
        Assert.Equal("gemini-4-argon", settings.ModelId);
        Assert.Equal("global", settings.Location);
        Assert.Equal("MEDIUM", settings.ThinkingLevel);
        Assert.Null(settings.Phases);           // not a pipeline feature
        Assert.NotNull(settings.Search);        // search feature gets defaults
    }

    // ── AiModelConfigService ─────────────────────────────────────────────

    private static AiModelConfigService CreateConfigService(MyDatabaseContext db, VertexAiPipelineOptions? pipeline = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["GeminiVision:TextModel"] = "gemini-3.1-flash-lite-preview" })
            .Build();
        return new AiModelConfigService(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            new MemoryCache(new MemoryCacheOptions()),
            config,
            Options.Create(pipeline ?? new VertexAiPipelineOptions()),
            Options.Create(new TechniqueSearchOptions()),
            new Mock<ILogger<AiModelConfigService>>().Object);
    }

    [Fact]
    public async Task Should_ReturnDefaults_ThenOverride_ThenDefaultsAfterReset()
    {
        using var db = InMemoryDbContextFactory.Create();
        var svc = CreateConfigService(db);

        var before = await svc.GetEffectiveAsync(AiFeatureKeys.VideoAnalysisPipeline);
        Assert.Equal("gemini-4-argon", before.ModelId);
        Assert.Equal("HIGH", before.Phases!.HeadCoach.ThinkingLevel);

        var update = AiModelConfigService.PipelineDefaults(new VertexAiPipelineOptions());
        update.ModelId = "gemini-3.1-pro-preview";
        var saved = await svc.SaveOverrideAsync(AiFeatureKeys.VideoAnalysisPipeline, update, "admin@codejitsu.com");

        Assert.True(saved.IsOverridden);
        Assert.Equal("admin@codejitsu.com", saved.UpdatedBy);
        Assert.Equal("gemini-4-argon", saved.Defaults.ModelId);
        Assert.Equal("gemini-3.1-pro-preview", (await svc.GetEffectiveAsync(AiFeatureKeys.VideoAnalysisPipeline)).ModelId);

        var reset = await svc.ResetAsync(AiFeatureKeys.VideoAnalysisPipeline);

        Assert.False(reset.IsOverridden);
        Assert.Equal("gemini-4-argon", (await svc.GetEffectiveAsync(AiFeatureKeys.VideoAnalysisPipeline)).ModelId);
    }

    [Fact]
    public async Task Should_ExposeDefaults_ForEveryFeature()
    {
        using var db = InMemoryDbContextFactory.Create();
        var svc = CreateConfigService(db);

        foreach (var feature in svc.Features)
        {
            var config = await svc.GetConfigAsync(feature.Key);
            Assert.False(string.IsNullOrEmpty(config.Effective.ModelId));
            Assert.Empty(AiFeatureSettingsValidator.Validate(feature, config.Defaults)); // shipped defaults are valid
        }
        Assert.Equal("gemini-3.1-flash-lite-preview", (await svc.GetEffectiveAsync(AiFeatureKeys.ClassCurriculum)).ModelId);
        Assert.Equal("gemini-4-argon", (await svc.GetEffectiveAsync(AiFeatureKeys.TechniqueSearch)).ModelId);
    }
}

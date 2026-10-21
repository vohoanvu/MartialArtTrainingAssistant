using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedEntities.Data;
using VideoAnalysis.Server.Configuration;
using VideoAnalysis.Server.Domain.AIServices;
using VideoAnalysis.Server.Models.Dtos;

namespace VideoAnalysis.Server.Controllers
{
    /// <summary>
    /// AI Console: view and override the model configuration of every GenAI feature. Restricted to the emails
    /// in <c>AiConsole:AdminEmails</c> — every SSO user is an Instructor, so roles can't gate this.
    /// </summary>
    [Route("api/ai-console")]
    [ApiController]
    [Authorize]
    public class AiConsoleController(
        IAiModelConfigService modelConfig,
        IVertexRestClient vertex,
        MyDatabaseContext db,
        IOptions<AiConsoleOptions> consoleOptions,
        ILogger<AiConsoleController> logger) : ControllerBase
    {
        private const string TestPrompt = "Reply with the single word OK.";

        /// <summary>Whether the caller may use the console (drives the client nav link).</summary>
        [HttpGet("access")]
        public async Task<ActionResult<AiConsoleAccessDto>> GetAccessAsync()
        {
            return new AiConsoleAccessDto { IsAdmin = await GetAdminEmailAsync() != null };
        }

        [HttpGet]
        public async Task<ActionResult<AiConsoleOverviewDto>> GetOverviewAsync(CancellationToken ct)
        {
            if (await GetAdminEmailAsync() == null) return Forbid();

            var features = new List<AiFeatureConfigDto>();
            foreach (var feature in modelConfig.Features)
                features.Add(await modelConfig.GetConfigAsync(feature.Key, ct));

            return new AiConsoleOverviewDto
            {
                Models = GeminiModelCatalog.Models.Select(ToDto).ToList(),
                Features = features,
            };
        }

        [HttpPut("features/{key}")]
        public async Task<ActionResult<AiFeatureConfigDto>> UpdateAsync(string key, [FromBody] AiFeatureSettings settings, CancellationToken ct)
        {
            var admin = await GetAdminEmailAsync();
            if (admin == null) return Forbid();
            var feature = modelConfig.FindFeature(key);
            if (feature == null) return NotFound(new { message = $"Unknown AI feature '{key}'." });

            var errors = AiFeatureSettingsValidator.Validate(feature, settings);
            if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

            return await modelConfig.SaveOverrideAsync(key, settings, admin, ct);
        }

        [HttpDelete("features/{key}")]
        public async Task<ActionResult<AiFeatureConfigDto>> ResetAsync(string key, CancellationToken ct)
        {
            var admin = await GetAdminEmailAsync();
            if (admin == null) return Forbid();
            if (modelConfig.FindFeature(key) == null) return NotFound(new { message = $"Unknown AI feature '{key}'." });

            logger.LogInformation("AI Console: {User} reset {Feature}.", admin, key);
            return await modelConfig.ResetAsync(key, ct);
        }

        /// <summary>
        /// Sends a tiny text prompt with the (unsaved) settings so an admin can confirm the model id, location
        /// and thinking config are accepted by Vertex before saving. For the pipeline the Event Logger phase is used.
        /// </summary>
        [HttpPost("features/{key}/test")]
        public async Task<ActionResult<AiModelTestResultDto>> TestAsync(string key, [FromBody] AiFeatureSettings settings, CancellationToken ct)
        {
            if (await GetAdminEmailAsync() == null) return Forbid();
            var feature = modelConfig.FindFeature(key);
            if (feature == null) return NotFound(new { message = $"Unknown AI feature '{key}'." });

            var errors = AiFeatureSettingsValidator.Validate(feature, settings);
            if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

            var model = GeminiModelCatalog.Resolve(settings.ModelId);
            var gen = settings.Phases?.EventLogger;
            var body = new
            {
                contents = new[] { new { role = "user", parts = new object[] { new { text = TestPrompt } } } },
                generationConfig = GeminiModelCatalog.BuildGenerationConfig(
                    model,
                    gen?.ThinkingLevel ?? settings.ThinkingLevel,
                    gen?.ThinkingBudget ?? settings.ThinkingBudget,
                    gen?.Temperature ?? settings.Temperature,
                    gen?.MaxOutputTokens ?? settings.MaxOutputTokens ?? 1024),
            };

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await vertex.GenerateContentAsync(model.Id, settings.Location, body, ct);
                var text = vertex.ExtractText(response).Trim();
                return new AiModelTestResultDto
                {
                    Ok = true,
                    ModelId = model.Id,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    ResponseModelVersion = response.TryGetProperty("modelVersion", out var v) ? v.GetString() : null,
                    Sample = text.Length > 200 ? text[..200] : text,
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
            {
                logger.LogWarning(ex, "AI Console model test failed for {Feature} on {Model}.", key, model.Id);
                return new AiModelTestResultDto
                {
                    Ok = false,
                    ModelId = model.Id,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    Error = ex.Message,
                };
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>The caller's email when it is on the admin allow-list, else null.</summary>
        private async Task<string?> GetAdminEmailAsync()
        {
            var admins = consoleOptions.Value.AdminEmailSet();
            if (admins.Count == 0) return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var email = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync();
            return email != null && admins.Contains(email) ? email : null;
        }

        private static AiModelInfoDto ToDto(GeminiModelInfo m) => new()
        {
            Id = m.Id,
            DisplayName = m.DisplayName,
            LaunchStage = m.LaunchStage,
            ThinkingMode = m.ThinkingMode == ThinkingMode.Level ? "level" : "budget",
            SupportsExplicitCache = m.SupportsExplicitCache,
            SupportsVideo = m.SupportsVideo,
            SupportsGoogleSearch = m.SupportsGoogleSearch,
            SamplingParamsIgnored = m.SamplingParamsIgnored,
            SupportedLocations = m.SupportedLocations,
            Notes = m.Notes,
        };
    }
}

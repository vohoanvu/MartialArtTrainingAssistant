using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoAnalysis.Server.Configuration;

namespace VideoAnalysis.Server.Domain.AccessControl
{
    public static class AnalysisKinds
    {
        public const string V2 = "v2";
        public const string V1 = "v1";
    }

    public class AnalysisQuotaSettings
    {
        public bool Enabled { get; set; }
        public int MaxPerWindow { get; set; }
        public int WindowHours { get; set; }
    }

    public class AnalysisQuotaStatusDto
    {
        public bool Enabled { get; set; }
        public bool Exempt { get; set; }
        public int Limit { get; set; }
        public int Used { get; set; }
        public int Remaining { get; set; }
        public int WindowHours { get; set; }
        public DateTime? ResetsAt { get; set; }

        /// <summary>True when this user can't start another analysis right now.</summary>
        public bool IsExhausted => Enabled && !Exempt && Remaining <= 0;
    }

    public class AnalysisQuotaConfigDto
    {
        public bool IsOverridden { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public required AnalysisQuotaSettings Defaults { get; set; }
        public required AnalysisQuotaSettings Effective { get; set; }
    }

    /// <summary>Outcome of <see cref="IAnalysisQuotaService.TryReserveAsync"/>.</summary>
    public sealed record QuotaReservation(bool Allowed, AnalysisQuotaStatusDto Status);

    /// <summary>
    /// Free-demo quota on video analyses: N started analyses per user per rolling window, refunded when a
    /// run fails, admins exempt. Usage is a ledger (<c>AnalysisUsages</c>) so deleting a video doesn't
    /// return its quota. Settings = AI Console override (AiFeatureConfigs row <c>analysis-quota</c>) ?? appsettings.
    /// </summary>
    public interface IAnalysisQuotaService
    {
        Task<AnalysisQuotaStatusDto> GetStatusAsync(string userId, CancellationToken ct = default);

        /// <summary>Charges one analysis to <paramref name="userId"/> if the quota allows it.</summary>
        Task<QuotaReservation> TryReserveAsync(string userId, int videoId, string kind, CancellationToken ct = default);

        /// <summary>Returns the most recent charge for this video's run (called when the run fails).</summary>
        Task RefundAsync(int videoId, string kind, CancellationToken ct = default);

        /// <summary>True while an agentic (v2) run of this video is queued or running, so it isn't started (and charged) twice.</summary>
        Task<bool> IsAnalysisInProgressAsync(int videoId, CancellationToken ct = default);

        Task<AnalysisQuotaConfigDto> GetConfigAsync(CancellationToken ct = default);
        Task<AnalysisQuotaConfigDto> SaveOverrideAsync(AnalysisQuotaSettings settings, string? updatedBy, CancellationToken ct = default);
        Task<AnalysisQuotaConfigDto> ResetAsync(CancellationToken ct = default);
    }

    /// <inheritdoc cref="IAnalysisQuotaService"/>
    public class AnalysisQuotaService(
        MyDatabaseContext db,
        IAdminAccessService adminAccess,
        IMemoryCache cache,
        IOptions<AnalysisQuotaOptions> options,
        ILogger<AnalysisQuotaService> logger) : IAnalysisQuotaService
    {
        public const string ConfigKey = "analysis-quota";
        public const int MaxPerWindowLimit = 1000;
        public const int MaxWindowHours = 720;
        public const int InProgressStaleMinutes = 45;

        private const string CacheKey = "analysis-quota-settings";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // ── Quota ────────────────────────────────────────────────────────────

        public async Task<AnalysisQuotaStatusDto> GetStatusAsync(string userId, CancellationToken ct = default)
        {
            var settings = await GetEffectiveAsync(ct);
            var exempt = await adminAccess.GetAdminEmailAsync(userId, ct) != null;
            return await BuildStatusAsync(userId, settings, exempt, ct);
        }

        public async Task<QuotaReservation> TryReserveAsync(string userId, int videoId, string kind, CancellationToken ct = default)
        {
            var settings = await GetEffectiveAsync(ct);
            var exempt = await adminAccess.GetAdminEmailAsync(userId, ct) != null;
            var limited = settings.Enabled && !exempt;

            if (limited)
            {
                var before = await BuildStatusAsync(userId, settings, exempt, ct);
                if (before.Remaining <= 0) return new QuotaReservation(false, before);
            }

            // Every start is recorded (also for admins / when disabled) so usage stays auditable.
            var usage = new AnalysisUsage { UserId = userId, VideoId = videoId, Kind = kind, StartedAt = DateTime.UtcNow };
            db.AnalysisUsages.Add(usage);
            await db.SaveChangesAsync(ct);

            if (limited)
            {
                // Race-safe without locks: two concurrent requests both insert, and only those whose row is
                // within the first N of the window keep their reservation.
                var since = usage.StartedAt.AddHours(-settings.WindowHours);
                var earlier = await db.AnalysisUsages.CountAsync(u =>
                    u.UserId == userId && u.RefundedAt == null && u.StartedAt > since && u.Id < usage.Id, ct);
                if (earlier >= settings.MaxPerWindow)
                {
                    db.AnalysisUsages.Remove(usage);
                    await db.SaveChangesAsync(ct);
                    return new QuotaReservation(false, await BuildStatusAsync(userId, settings, exempt, ct));
                }
            }

            logger.LogInformation("Analysis quota: charged {Kind} analysis of video {VideoId} to user {UserId}.", kind, videoId, userId);
            return new QuotaReservation(true, await BuildStatusAsync(userId, settings, exempt, ct));
        }

        public async Task RefundAsync(int videoId, string kind, CancellationToken ct = default)
        {
            var usage = await db.AnalysisUsages
                .Where(u => u.VideoId == videoId && u.Kind == kind && u.RefundedAt == null)
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync(ct);
            if (usage == null) return;

            usage.RefundedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Analysis quota: refunded failed {Kind} analysis of video {VideoId} to user {UserId}.", kind, videoId, usage.UserId);
        }

        public async Task<bool> IsAnalysisInProgressAsync(int videoId, CancellationToken ct = default)
        {
            // Past this age a "running" status is a crashed run, not a live one, and may be restarted.
            var cutoff = DateTime.UtcNow.AddMinutes(-InProgressStaleMinutes);
            var result = await db.AiAnalysisResults.AsNoTracking()
                .Where(a => a.VideoId == videoId)
                .Select(a => new { a.PipelineStatus, a.LastUpdatedAt })
                .FirstOrDefaultAsync(ct);

            var lastUpdate = result?.LastUpdatedAt ?? DateTime.MinValue;
            if (result?.PipelineStatus is AnalysisPipelineStatus.Profiling or AnalysisPipelineStatus.Logging
                    or AnalysisPipelineStatus.Verifying or AnalysisPipelineStatus.Coaching
                && lastUpdate > cutoff)
                return true;

            // Charged but not yet picked up by the pipeline (queued): a failed run is refunded, a finished
            // one updates LastUpdatedAt past its start, so only a pending run matches.
            return await db.AnalysisUsages.AnyAsync(u =>
                u.VideoId == videoId && u.Kind == AnalysisKinds.V2 && u.RefundedAt == null
                && u.StartedAt > cutoff && u.StartedAt > lastUpdate, ct);
        }

        private async Task<AnalysisQuotaStatusDto> BuildStatusAsync(string userId, AnalysisQuotaSettings settings, bool exempt, CancellationToken ct)
        {
            var since = DateTime.UtcNow.AddHours(-settings.WindowHours);
            var counted = await db.AnalysisUsages
                .Where(u => u.UserId == userId && u.RefundedAt == null && u.StartedAt > since)
                .Select(u => u.StartedAt)
                .OrderBy(s => s)
                .ToListAsync(ct);

            // The next slot frees up when enough of the oldest uses age out to get back under the limit.
            DateTime? resetsAt = null;
            if (counted.Count > 0)
            {
                var index = Math.Clamp(counted.Count - settings.MaxPerWindow, 0, counted.Count - 1);
                resetsAt = DateTime.SpecifyKind(counted[index], DateTimeKind.Utc).AddHours(settings.WindowHours);
            }

            return new AnalysisQuotaStatusDto
            {
                Enabled = settings.Enabled,
                Exempt = exempt,
                Limit = settings.MaxPerWindow,
                Used = counted.Count,
                Remaining = Math.Max(0, settings.MaxPerWindow - counted.Count),
                WindowHours = settings.WindowHours,
                ResetsAt = resetsAt,
            };
        }

        // ── Settings (AI Console override ?? appsettings) ────────────────────

        public static Dictionary<string, string[]> Validate(AnalysisQuotaSettings s)
        {
            var errors = new Dictionary<string, string[]>();
            if (s.MaxPerWindow is < 0 or > MaxPerWindowLimit)
                errors["maxPerWindow"] = [$"Max analyses must be between 0 and {MaxPerWindowLimit}."];
            if (s.WindowHours is < 1 or > MaxWindowHours)
                errors["windowHours"] = [$"Window must be between 1 and {MaxWindowHours} hours."];
            return errors;
        }

        private AnalysisQuotaSettings Defaults() => new()
        {
            Enabled = options.Value.Enabled,
            MaxPerWindow = options.Value.MaxPerWindow,
            WindowHours = Math.Max(1, options.Value.WindowHours),
        };

        private async Task<AnalysisQuotaSettings> GetEffectiveAsync(CancellationToken ct) =>
            Deserialize((await GetOverrideAsync(ct))?.SettingsJson) ?? Defaults();

        public async Task<AnalysisQuotaConfigDto> GetConfigAsync(CancellationToken ct = default)
        {
            var row = await GetOverrideAsync(ct);
            var overridden = Deserialize(row?.SettingsJson);
            return new AnalysisQuotaConfigDto
            {
                IsOverridden = overridden != null,
                UpdatedAt = overridden != null ? row!.UpdatedAt : null,
                UpdatedBy = overridden != null ? row!.UpdatedBy : null,
                Defaults = Defaults(),
                Effective = overridden ?? Defaults(),
            };
        }

        public async Task<AnalysisQuotaConfigDto> SaveOverrideAsync(AnalysisQuotaSettings settings, string? updatedBy, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var row = await db.AiFeatureConfigs.FirstOrDefaultAsync(c => c.FeatureKey == ConfigKey, ct);
            if (row == null)
            {
                db.AiFeatureConfigs.Add(new AiFeatureConfig { FeatureKey = ConfigKey, SettingsJson = json, UpdatedBy = updatedBy });
            }
            else
            {
                row.SettingsJson = json;
                row.UpdatedAt = DateTime.UtcNow;
                row.UpdatedBy = updatedBy;
            }
            await db.SaveChangesAsync(ct);
            cache.Remove(CacheKey);
            logger.LogInformation("AI Console: {User} set analysis quota to {Max} per {Hours}h (enabled: {Enabled}).",
                updatedBy, settings.MaxPerWindow, settings.WindowHours, settings.Enabled);
            return await GetConfigAsync(ct);
        }

        public async Task<AnalysisQuotaConfigDto> ResetAsync(CancellationToken ct = default)
        {
            var row = await db.AiFeatureConfigs.FirstOrDefaultAsync(c => c.FeatureKey == ConfigKey, ct);
            if (row != null)
            {
                db.AiFeatureConfigs.Remove(row);
                await db.SaveChangesAsync(ct);
            }
            cache.Remove(CacheKey);
            return await GetConfigAsync(ct);
        }

        private async Task<AiFeatureConfig?> GetOverrideAsync(CancellationToken ct)
        {
            if (cache.TryGetValue(CacheKey, out AiFeatureConfig? cached)) return cached;
            AiFeatureConfig? row;
            try
            {
                row = await db.AiFeatureConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.FeatureKey == ConfigKey, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not read the analysis quota override; using defaults.");
                return null;
            }
            cache.Set(CacheKey, row, CacheTtl);
            return row;
        }

        private AnalysisQuotaSettings? Deserialize(string? json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonSerializer.Deserialize<AnalysisQuotaSettings>(json, JsonOptions); }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Ignoring unreadable analysis quota override JSON.");
                return null;
            }
        }
    }
}

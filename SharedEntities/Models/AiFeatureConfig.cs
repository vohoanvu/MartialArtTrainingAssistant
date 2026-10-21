using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Models;

/// <summary>
/// Admin-console override of the GenAI model settings for one feature (e.g. the agentic video-analysis
/// pipeline, curriculum generation). A row only exists while an override is active; deleting it falls
/// the feature back to its appsettings/env defaults.
/// </summary>
public class AiFeatureConfig
{
    /// <summary>Stable feature key, e.g. <c>video-analysis-pipeline</c>.</summary>
    [Key]
    [MaxLength(64)]
    public required string FeatureKey { get; set; }

    /// <summary>Serialized override settings (model id, location, thinking, per-phase knobs, ...).</summary>
    public required string SettingsJson { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Email of the admin who last saved the override.</summary>
    [MaxLength(256)]
    public string? UpdatedBy { get; set; }
}

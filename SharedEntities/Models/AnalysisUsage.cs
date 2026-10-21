using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Models;

/// <summary>
/// One started video analysis, charged against the user's free-demo quota. Kept separately from the
/// analysis results so deleting a video doesn't hand back its quota. A failed run is refunded by
/// setting <see cref="RefundedAt"/>; refunded rows don't count.
/// </summary>
public class AnalysisUsage
{
    public long Id { get; set; }

    [MaxLength(450)]
    public required string UserId { get; set; }

    public int VideoId { get; set; }

    /// <summary><c>v2</c> (agentic pipeline) or <c>v1</c> (single-shot).</summary>
    [MaxLength(8)]
    public required string Kind { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RefundedAt { get; set; }
}

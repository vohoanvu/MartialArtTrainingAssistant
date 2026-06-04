using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedEntities.Models
{
    /// <summary>
    /// Status of the multi-step (agentic) video-analysis pipeline. Mirrors the PRD status
    /// transitions: NotStarted → Profiling → Logging → Verifying → Coaching → Complete | Failed.
    /// </summary>
    public enum AnalysisPipelineStatus
    {
        NotStarted = 0,
        Profiling = 1,
        Logging = 2,
        Verifying = 3,
        Coaching = 4,
        Complete = 5,
        Failed = 6,
    }

    public enum EventActor
    {
        Student = 0,
        Opponent = 1,
    }

    public enum EventOutcome
    {
        Successful = 0,
        Failed = 1,
        Partial = 2,
        Countered = 3,
        InProgress = 4,
    }

    public enum WeaknessSeverity
    {
        Critical = 0,
        Major = 1,
        Minor = 2,
    }

    /// <summary>
    /// A single timestamped exchange in a match, produced by the Event Logger (Phase 2) and
    /// corrected by the Event Verifier (Phase 3). Timestamps are absolute milliseconds from the
    /// start of the video (distinct from the legacy <see cref="VideoSegmentFeedback"/> TimeSpan path).
    /// </summary>
    public class MatchEvent
    {
        [Key]
        public int Id { get; set; }

        public int AiAnalysisResultId { get; set; }
        [ForeignKey(nameof(AiAnalysisResultId))]
        public virtual AiAnalysisResult AiAnalysisResult { get; set; }

        public int StartTimestampMs { get; set; }
        public int EndTimestampMs { get; set; }

        public EventActor Actor { get; set; }

        /// <summary>One of the 12 technique categories. Stored as string for taxonomy evolution.</summary>
        public required string TechniqueCategory { get; set; }

        /// <summary>Free-text specific technique name (e.g. "Double Leg", "Armbar from Guard").</summary>
        public string? TechniqueName { get; set; }

        /// <summary>One of the 13 position values. Stored as string.</summary>
        public string? PositionBefore { get; set; }
        public string? PositionAfter { get; set; }

        public EventOutcome Outcome { get; set; }

        public string? GuardType { get; set; }
        public string? SubmissionType { get; set; }

        public string? ActionsDescription { get; set; }

        /// <summary>VLM self-assessed confidence 0.0–1.0 (min of actor/technique/position certainty).</summary>
        public double Confidence { get; set; }

        /// <summary>Preserves the model's event ordering for stable reads.</summary>
        public int SequenceIndex { get; set; }
    }

    /// <summary>
    /// The Head Coach (Phase 4) synthesis: one per analysis. Strengths/weaknesses/drills live in
    /// child tables; grade/label/tip/summary are mirrored onto <see cref="AiAnalysisResult"/> for
    /// quick list reads but owned here.
    /// </summary>
    public class CoachingReport
    {
        [Key]
        public int Id { get; set; }

        public int AiAnalysisResultId { get; set; }
        [ForeignKey(nameof(AiAnalysisResultId))]
        public virtual AiAnalysisResult AiAnalysisResult { get; set; }

        public required string MatchSummary { get; set; }
        public int TechnicalGrade { get; set; }
        public string? GradeLabel { get; set; }
        public string? EliteTip { get; set; }

        public virtual ICollection<CoachingStrength> Strengths { get; set; } = [];
        public virtual ICollection<CoachingWeakness> Weaknesses { get; set; } = [];
        public virtual ICollection<PrescribedDrill> PrescribedDrills { get; set; } = [];
    }

    public class CoachingStrength
    {
        [Key]
        public int Id { get; set; }

        public int CoachingReportId { get; set; }
        [ForeignKey(nameof(CoachingReportId))]
        public virtual CoachingReport CoachingReport { get; set; }

        public required string Title { get; set; }
        public string? Explanation { get; set; }
        public int? TimestampStartMs { get; set; }
        public int? TimestampEndMs { get; set; }
        public int SortOrder { get; set; }
    }

    public class CoachingWeakness
    {
        [Key]
        public int Id { get; set; }

        public int CoachingReportId { get; set; }
        [ForeignKey(nameof(CoachingReportId))]
        public virtual CoachingReport CoachingReport { get; set; }

        public required string Title { get; set; }
        public string? Explanation { get; set; }
        public int? TimestampStartMs { get; set; }
        public int? TimestampEndMs { get; set; }
        public WeaknessSeverity Severity { get; set; }
        public string? ScoringImpact { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// A drill prescribed by the Head Coach. Intentionally distinct from <see cref="Drills"/>
    /// (which requires a Technique FK and carries Focus/Duration); this shape is drill/goal/instructions.
    /// </summary>
    public class PrescribedDrill
    {
        [Key]
        public int Id { get; set; }

        public int CoachingReportId { get; set; }
        [ForeignKey(nameof(CoachingReportId))]
        public virtual CoachingReport CoachingReport { get; set; }

        public required string DrillName { get; set; }
        public string? Instructions { get; set; }
        public string? Goal { get; set; }
        public int SortOrder { get; set; }
    }
}

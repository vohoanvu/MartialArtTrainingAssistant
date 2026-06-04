using MyCoach.Application.Pipeline.Models;
using MyCoach.Domain.Entities;

namespace MyCoach.Application.Interfaces;

public interface IVertexAiService
{
    /// <summary>Phase 1: Create context cache and generate Visual DNA.</summary>
    Task<(string CacheName, string VisualDna)> CreateCacheAndGenerateVisualDnaAsync(
        string videoGcsUri,
        string studentIdentifier,
        CancellationToken ct);

    /// <summary>Phase 2: Full-video event logging using cached video.</summary>
    Task<List<RawMatchEvent>> LogEventsAsync(
        string cacheName,
        string visualDna,
        CancellationToken ct);

    /// <summary>Phase 3: Verify events against Visual DNA using cached video.</summary>
    Task<List<RawMatchEvent>> VerifyEventsAsync(
        string cacheName,
        string visualDna,
        List<RawMatchEvent> events,
        CancellationToken ct);

    /// <summary>Phase 4: Generate coaching report using cached video + verified events.</summary>
    Task<CoachingResult> GenerateCoachingReportAsync(
        string cacheName,
        string verifiedEventsJson,
        string? beltLevel,
        string? experienceLevel,
        string? primaryGoal,
        int? staminaMinutes,
        decimal? heightCm,
        decimal? weightKg,
        CancellationToken ct);

    /// <summary>Delete a context cache resource.</summary>
    Task DeleteCacheAsync(string cacheName, CancellationToken ct);

    /// <summary>
    /// Phase 2.8 v2 (ADR-054 §5) — single-shot structured extraction from a
    /// document already in GCS. Used by
    /// <see cref="IMemberDocumentExtractor"/>'s LLM-fallback branch for
    /// PDF / Word / header-less spreadsheets where deterministic parsing
    /// won't work. Builds a Gemini request with a single <c>fileData</c>
    /// part referencing the GCS object, applies the supplied JSON Schema
    /// as <c>responseSchema</c>, and returns Gemini's response text — the
    /// caller deserializes into its own DTO shape so this interface stays
    /// free of feature-specific types.
    ///
    /// <paramref name="responseSchema"/> is the JSON Schema literal the
    /// extractor wants enforced (the same shape passed today to
    /// <c>responseSchema</c> in <see cref="VerifyEventsAsync"/> +
    /// <see cref="GenerateCoachingReportAsync"/> — built as an anonymous
    /// object or <see cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>).
    /// </summary>
    Task<string> ExtractStructuredAsync(
        string gcsUri,
        string mimeType,
        string promptText,
        object responseSchema,
        CancellationToken ct);
}

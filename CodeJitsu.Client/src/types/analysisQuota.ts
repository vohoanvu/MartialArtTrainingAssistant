/** Per-user demo analysis quota (GET /vid/api/video/analysis-quota). */
export interface AnalysisQuotaStatus {
    /** false = no limit is enforced. */
    enabled: boolean;
    /** true for AI Console admins (unlimited). */
    exempt: boolean;
    limit: number;
    used: number;
    /** max(0, limit - used); meaningless when !enabled || exempt. */
    remaining: number;
    windowHours: number;
    /** ISO time when the oldest counted use expires (next slot frees up); null if used == 0. */
    resetsAt: string | null;
}

/** Body of the HTTP 429 returned when the user has no analyses left. */
export interface AnalysisQuotaExceeded {
    code: 'ANALYSIS_QUOTA_EXCEEDED';
    message: string;
    limit: number;
    used: number;
    windowHours: number;
    resetsAt: string | null;
}

/** Body of the HTTP 409 returned when the video's pipeline is already running. */
export interface AnalysisInProgress {
    code: 'ANALYSIS_IN_PROGRESS';
    message: string;
}

/** AI Console: editable quota settings (maxPerWindow 0..1000, windowHours 1..720). */
export interface AnalysisQuotaSettings {
    enabled: boolean;
    maxPerWindow: number;
    windowHours: number;
}

export interface AnalysisQuotaConfig {
    isOverridden: boolean;
    updatedAt: string | null;
    updatedBy: string | null;
    defaults: AnalysisQuotaSettings;
    effective: AnalysisQuotaSettings;
}

/** True when the user can no longer start an analysis right now. */
export function isQuotaBlocked(quota: AnalysisQuotaStatus | null | undefined): boolean {
    return !!quota && quota.enabled && !quota.exempt && quota.remaining <= 0;
}

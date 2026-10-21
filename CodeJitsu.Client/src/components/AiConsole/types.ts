export type FeatureKey =
    | 'video-analysis-pipeline'
    | 'video-analysis-single-shot'
    | 'class-curriculum'
    | 'fighter-pairing'
    | 'technique-search';

export type ThinkingLevel = 'LOW' | 'MEDIUM' | 'HIGH';
export type ThinkingMode = 'level' | 'budget';
export type FeatureKind = 'pipeline' | 'generation' | 'search';
export type PhaseKey = 'profiler' | 'eventLogger' | 'verifier' | 'headCoach';

export interface AiModelInfo {
    id: string;
    displayName: string;
    launchStage: 'GA' | 'Preview';
    thinkingMode: ThinkingMode;
    supportsExplicitCache: boolean;
    supportsVideo: boolean;
    supportsGoogleSearch: boolean;
    samplingParamsIgnored: boolean;
    supportedLocations: string[];
    notes: string | null;
}

export interface AiPhaseSettings {
    thinkingLevel: ThinkingLevel | null;
    thinkingBudget: number | null;
    temperature: number | null;
    maxOutputTokens: number | null;
    fps: number | null;
}

export type AiPhases = Record<PhaseKey, AiPhaseSettings>;

export interface AiSearchSettings {
    maxResults: number;
    verifyWithYouTube: boolean;
}

export interface AiFeatureSettings {
    modelId: string;
    location: string;
    thinkingLevel: ThinkingLevel | null;
    thinkingBudget: number | null;
    temperature: number | null;
    maxOutputTokens: number | null;
    phases: AiPhases | null;
    search: AiSearchSettings | null;
}

export interface AiFeatureConfig {
    key: FeatureKey;
    kind: FeatureKind;
    requiresVideo: boolean;
    requiresGoogleSearch: boolean;
    isOverridden: boolean;
    updatedAt: string | null;
    updatedBy: string | null;
    defaults: AiFeatureSettings;
    effective: AiFeatureSettings;
}

export interface AiConsoleOverview {
    models: AiModelInfo[];
    features: AiFeatureConfig[];
}

export interface AiModelTestResult {
    ok: boolean;
    modelId: string;
    latencyMs: number;
    responseModelVersion: string | null;
    sample: string | null;
    error: string | null;
}

export const PHASE_KEYS: PhaseKey[] = ['profiler', 'eventLogger', 'verifier', 'headCoach'];
export const THINKING_LEVELS: ThinkingLevel[] = ['LOW', 'MEDIUM', 'HIGH'];

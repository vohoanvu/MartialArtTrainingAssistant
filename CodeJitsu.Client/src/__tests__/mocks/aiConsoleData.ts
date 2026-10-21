import type {
    AiConsoleOverview,
    AiFeatureConfig,
    AiFeatureSettings,
    AiModelInfo,
    AiModelTestResult,
    AiPhaseSettings,
} from '@/components/AiConsole/types';

export const mockModels: AiModelInfo[] = [
    {
        id: 'gemini-4-argon',
        displayName: 'Gemini 4 Argon',
        launchStage: 'GA',
        thinkingMode: 'level',
        supportsExplicitCache: true,
        supportsVideo: true,
        supportsGoogleSearch: true,
        samplingParamsIgnored: false,
        supportedLocations: ['global'],
        notes: 'Best quality, highest cost.',
    },
    {
        id: 'gemini-3-budget',
        displayName: 'Gemini 3 Budget',
        launchStage: 'Preview',
        thinkingMode: 'budget',
        supportsExplicitCache: false,
        supportsVideo: true,
        supportsGoogleSearch: false,
        samplingParamsIgnored: true,
        supportedLocations: ['global', 'us-central1'],
        notes: null,
    },
    {
        id: 'gemini-text-only',
        displayName: 'Gemini Text Only',
        launchStage: 'GA',
        thinkingMode: 'level',
        supportsExplicitCache: true,
        supportsVideo: false,
        supportsGoogleSearch: true,
        samplingParamsIgnored: false,
        supportedLocations: ['global'],
        notes: null,
    },
];

const phase = (overrides: Partial<AiPhaseSettings> = {}): AiPhaseSettings => ({
    thinkingLevel: 'MEDIUM',
    thinkingBudget: null,
    temperature: 0.4,
    maxOutputTokens: 8192,
    fps: 2,
    ...overrides,
});

export const pipelineSettings: AiFeatureSettings = {
    modelId: 'gemini-4-argon',
    location: 'global',
    thinkingLevel: null,
    thinkingBudget: null,
    temperature: null,
    maxOutputTokens: null,
    phases: {
        profiler: phase({ fps: 1 }),
        eventLogger: phase({ thinkingLevel: 'HIGH', maxOutputTokens: 32768, fps: 4 }),
        verifier: phase({ fps: 0 }),
        headCoach: phase({ fps: 0 }),
    },
    search: null,
};

export const curriculumSettings: AiFeatureSettings = {
    modelId: 'gemini-4-argon',
    location: 'global',
    thinkingLevel: 'LOW',
    thinkingBudget: null,
    temperature: 0.7,
    maxOutputTokens: 4096,
    phases: null,
    search: null,
};

export const searchSettings: AiFeatureSettings = {
    modelId: 'gemini-4-argon',
    location: 'global',
    thinkingLevel: 'LOW',
    thinkingBudget: null,
    temperature: 0.2,
    maxOutputTokens: 2048,
    phases: null,
    search: { maxResults: 8, verifyWithYouTube: true },
};

export const makeFeature = (overrides: Partial<AiFeatureConfig> & Pick<AiFeatureConfig, 'key' | 'kind' | 'effective'>): AiFeatureConfig => ({
    requiresVideo: false,
    requiresGoogleSearch: false,
    isOverridden: false,
    updatedAt: null,
    updatedBy: null,
    defaults: overrides.effective,
    ...overrides,
});

export const mockOverview = (): AiConsoleOverview => ({
    models: mockModels,
    features: [
        makeFeature({
            key: 'video-analysis-pipeline',
            kind: 'pipeline',
            requiresVideo: true,
            effective: pipelineSettings,
        }),
        makeFeature({
            key: 'class-curriculum',
            kind: 'generation',
            effective: curriculumSettings,
            isOverridden: true,
            updatedAt: '2026-10-01T12:00:00Z',
            updatedBy: 'admin@example.com',
            defaults: { ...curriculumSettings, temperature: 0.5 },
        }),
        makeFeature({
            key: 'technique-search',
            kind: 'search',
            requiresGoogleSearch: true,
            effective: searchSettings,
        }),
    ],
});

export const mockTestResult: AiModelTestResult = {
    ok: true,
    modelId: 'gemini-4-argon',
    latencyMs: 812,
    responseModelVersion: 'gemini-4-argon-001',
    sample: 'pong',
    error: null,
};

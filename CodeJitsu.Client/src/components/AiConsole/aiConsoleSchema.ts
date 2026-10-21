import { z } from 'zod';
import {
    AiFeatureSettings,
    AiModelInfo,
    AiPhaseSettings,
    AiPhases,
    PHASE_KEYS,
    THINKING_LEVELS,
    ThinkingMode,
} from './types';

/** Sentinel Select value for the free-text "Custom model ID" option. */
export const CUSTOM_MODEL_VALUE = '__custom__';

export const MODEL_ID_REGEX = /^[a-z0-9][a-z0-9.-]{1,62}$/;
export const LOCATION_REGEX = /^(global|[a-z]+-[a-z]+[0-9]{1,2})$/;

export const LIMITS = {
    thinkingBudget: { min: 0, max: 32768 },
    temperature: { min: 0, max: 2 },
    maxOutputTokens: { min: 1, max: 65536 },
    fps: { min: 0, max: 10 },
    maxResults: { min: 1, max: 20 },
} as const;

/** Validation messages are i18n keys; the UI translates them. */
const MSG = {
    modelId: 'aiConsole.validation.modelId',
    location: 'aiConsole.validation.location',
    thinkingLevel: 'aiConsole.validation.thinkingLevel',
    thinkingBudget: 'aiConsole.validation.thinkingBudget',
    temperature: 'aiConsole.validation.temperature',
    maxOutputTokens: 'aiConsole.validation.maxOutputTokens',
    fps: 'aiConsole.validation.fps',
    maxResults: 'aiConsole.validation.maxResults',
};

const nullableNumber = (min: number, max: number, msg: string, integer: boolean) => {
    let base = z.number({ invalid_type_error: msg, required_error: msg });
    if (integer) base = base.int(msg);
    return base.min(min, msg).max(max, msg).nullable();
};

const thinkingLevelSchema = z.enum(THINKING_LEVELS as [string, ...string[]], { message: MSG.thinkingLevel }).nullable();
const thinkingBudgetSchema = nullableNumber(LIMITS.thinkingBudget.min, LIMITS.thinkingBudget.max, MSG.thinkingBudget, true);
const temperatureSchema = nullableNumber(LIMITS.temperature.min, LIMITS.temperature.max, MSG.temperature, false);
const maxOutputTokensSchema = nullableNumber(LIMITS.maxOutputTokens.min, LIMITS.maxOutputTokens.max, MSG.maxOutputTokens, true);

const phaseSchema = z.object({
    thinkingLevel: thinkingLevelSchema,
    thinkingBudget: thinkingBudgetSchema,
    temperature: temperatureSchema,
    maxOutputTokens: maxOutputTokensSchema,
    fps: nullableNumber(LIMITS.fps.min, LIMITS.fps.max, MSG.fps, true),
});

export const featureSettingsSchema = z.object({
    modelId: z.string().regex(MODEL_ID_REGEX, MSG.modelId),
    location: z.string().regex(LOCATION_REGEX, MSG.location),
    thinkingLevel: thinkingLevelSchema,
    thinkingBudget: thinkingBudgetSchema,
    temperature: temperatureSchema,
    maxOutputTokens: maxOutputTokensSchema,
    phases: z
        .object({
            profiler: phaseSchema,
            eventLogger: phaseSchema,
            verifier: phaseSchema,
            headCoach: phaseSchema,
        })
        .nullable(),
    search: z
        .object({
            maxResults: z
                .number({ invalid_type_error: MSG.maxResults, required_error: MSG.maxResults })
                .int(MSG.maxResults)
                .min(LIMITS.maxResults.min, MSG.maxResults)
                .max(LIMITS.maxResults.max, MSG.maxResults),
            verifyWithYouTube: z.boolean(),
        })
        .nullable(),
});

/** Returns a map of field path (e.g. `phases.eventLogger.fps`) to an i18n message key. */
export function validateSettings(settings: AiFeatureSettings): Record<string, string> {
    const result = featureSettingsSchema.safeParse(settings);
    if (result.success) return {};
    const errors: Record<string, string> = {};
    for (const issue of result.error.issues) {
        const path = issue.path.join('.');
        if (!(path in errors)) errors[path] = issue.message;
    }
    return errors;
}

const num = (v: unknown): number | null => (typeof v === 'number' ? v : null);

function normalizePhase(p: Partial<AiPhaseSettings> | null | undefined): AiPhaseSettings {
    return {
        thinkingLevel: p?.thinkingLevel ?? null,
        thinkingBudget: num(p?.thinkingBudget),
        temperature: num(p?.temperature),
        maxOutputTokens: num(p?.maxOutputTokens),
        fps: num(p?.fps),
    };
}

/** Builds a key-ordered, null-filled copy so comparisons and request bodies are stable. */
export function normalizeSettings(s: AiFeatureSettings): AiFeatureSettings {
    let phases: AiPhases | null = null;
    if (s.phases) {
        phases = {
            profiler: normalizePhase(s.phases.profiler),
            eventLogger: normalizePhase(s.phases.eventLogger),
            verifier: normalizePhase(s.phases.verifier),
            headCoach: normalizePhase(s.phases.headCoach),
        };
    }
    return {
        modelId: s.modelId ?? '',
        location: s.location ?? '',
        thinkingLevel: s.thinkingLevel ?? null,
        thinkingBudget: num(s.thinkingBudget),
        temperature: num(s.temperature),
        maxOutputTokens: num(s.maxOutputTokens),
        phases,
        search: s.search
            ? { maxResults: s.search.maxResults, verifyWithYouTube: !!s.search.verifyWithYouTube }
            : null,
    };
}

export function settingsEqual(a: AiFeatureSettings, b: AiFeatureSettings): boolean {
    return JSON.stringify(normalizeSettings(a)) === JSON.stringify(normalizeSettings(b));
}

type ParamPatch = (p: AiPhaseSettings) => AiPhaseSettings;

/** Apply `fn` to the top-level params and to every pipeline phase. */
function mapParams(s: AiFeatureSettings, fn: ParamPatch): AiFeatureSettings {
    const top = fn({ ...normalizePhase(s), fps: null });
    const next: AiFeatureSettings = {
        ...s,
        thinkingLevel: top.thinkingLevel,
        thinkingBudget: top.thinkingBudget,
        temperature: top.temperature,
        maxOutputTokens: top.maxOutputTokens,
    };
    if (s.phases) {
        const phases = { ...s.phases };
        for (const key of PHASE_KEYS) phases[key] = fn(normalizePhase(s.phases[key]));
        next.phases = phases;
    }
    return next;
}

/** Null out the thinking field that does not apply to the given thinking mode. */
export function applyThinkingMode(s: AiFeatureSettings, mode: ThinkingMode): AiFeatureSettings {
    return mapParams(s, (p) =>
        mode === 'level' ? { ...p, thinkingBudget: null } : { ...p, thinkingLevel: null }
    );
}

/** Switch model: fix location, drop non-applicable thinking field and ignored sampling params. */
export function applyModelChange(
    s: AiFeatureSettings,
    modelId: string,
    model: AiModelInfo | undefined
): AiFeatureSettings {
    let next: AiFeatureSettings = { ...s, modelId };
    if (!model) return next;
    if (model.supportedLocations.length > 0 && !model.supportedLocations.includes(next.location)) {
        next.location = model.supportedLocations[0];
    }
    next = applyThinkingMode(next, model.thinkingMode);
    if (model.samplingParamsIgnored) {
        next = mapParams(next, (p) => ({ ...p, temperature: null }));
    }
    return next;
}

/** Infer the thinking mode for a model that is not in the catalogue. */
export function inferThinkingMode(s: AiFeatureSettings): ThinkingMode {
    const phases = s.phases;
    const budgetUsed =
        s.thinkingBudget != null ||
        (phases != null && PHASE_KEYS.some((k) => phases[k].thinkingBudget != null));
    return budgetUsed ? 'budget' : 'level';
}

/**
 * Map a ValidationProblemDetails `errors` object to field-path -> message.
 * Tolerates PascalCase segments (`Phases.EventLogger.Fps`).
 */
export function mapServerErrors(
    errors: Record<string, string[] | string> | undefined | null
): Record<string, string> {
    const out: Record<string, string> = {};
    if (!errors) return out;
    for (const [rawPath, messages] of Object.entries(errors)) {
        const path = rawPath
            .split('.')
            .map((seg) => (seg ? seg.charAt(0).toLowerCase() + seg.slice(1) : seg))
            .join('.');
        out[path] = Array.isArray(messages) ? messages.join(' ') : String(messages);
    }
    return out;
}

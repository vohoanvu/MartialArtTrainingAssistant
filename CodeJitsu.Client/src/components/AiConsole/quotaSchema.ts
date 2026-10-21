import { z } from 'zod';
import type { AnalysisQuotaSettings } from '@/types/analysisQuota';

export const QUOTA_LIMITS = {
    maxPerWindow: { min: 0, max: 1000 },
    windowHours: { min: 1, max: 720 },
} as const;

/** Validation messages are i18n keys; the UI translates them. */
const MSG = {
    maxPerWindow: 'aiConsole.quota.validation.maxPerWindow',
    windowHours: 'aiConsole.quota.validation.windowHours',
};

const wholeNumber = (min: number, max: number, msg: string) =>
    z.number({ invalid_type_error: msg, required_error: msg }).int(msg).min(min, msg).max(max, msg);

export const quotaSettingsSchema = z.object({
    enabled: z.boolean(),
    maxPerWindow: wholeNumber(QUOTA_LIMITS.maxPerWindow.min, QUOTA_LIMITS.maxPerWindow.max, MSG.maxPerWindow),
    windowHours: wholeNumber(QUOTA_LIMITS.windowHours.min, QUOTA_LIMITS.windowHours.max, MSG.windowHours),
});

/** Map of field name to i18n message key; empty when valid. An emptied field arrives as NaN/null. */
export function validateQuotaSettings(settings: AnalysisQuotaSettings): Record<string, string> {
    const result = quotaSettingsSchema.safeParse(settings);
    if (result.success) return {};
    const errors: Record<string, string> = {};
    for (const issue of result.error.issues) {
        const path = issue.path.join('.');
        if (!(path in errors)) errors[path] = issue.message;
    }
    return errors;
}

export function quotaSettingsEqual(a: AnalysisQuotaSettings, b: AnalysisQuotaSettings): boolean {
    return a.enabled === b.enabled && a.maxPerWindow === b.maxPerWindow && a.windowHours === b.windowHours;
}

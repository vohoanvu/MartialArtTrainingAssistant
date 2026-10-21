import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Loader2, RefreshCw, RotateCcw, Save } from 'lucide-react';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { AlertDialog } from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { useToast } from '@/hooks/use-toast';
import {
    AiConsoleApiError,
    getAnalysisQuotaConfig,
    resetAnalysisQuotaConfig,
    updateAnalysisQuotaConfig,
} from '@/services/api';
import type { AnalysisQuotaConfig, AnalysisQuotaSettings } from '@/types/analysisQuota';
import { Field } from './Field';
import { NumberField } from './NumberField';
import { mapServerErrors } from './aiConsoleSchema';
import { QUOTA_LIMITS, quotaSettingsEqual, validateQuotaSettings } from './quotaSchema';

interface QuotaConfigCardProps {
    jwtToken: string | null;
    hydrate?: () => Promise<void> | void;
}

type LoadState =
    | { status: 'loading' }
    | { status: 'error'; message: string }
    | { status: 'ready'; config: AnalysisQuotaConfig };

/** Admin card for the free demo-analysis quota: enabled switch, analyses per window, window hours. */
export function QuotaConfigCard({ jwtToken, hydrate }: QuotaConfigCardProps) {
    const { t } = useTranslation();
    const [state, setState] = useState<LoadState>({ status: 'loading' });
    const [reloadKey, setReloadKey] = useState(0);

    useEffect(() => {
        let cancelled = false;
        getAnalysisQuotaConfig({ jwtToken, hydrate })
            .then((config) => {
                if (!cancelled) setState({ status: 'ready', config });
            })
            .catch((e: unknown) => {
                if (!cancelled) {
                    setState({ status: 'error', message: e instanceof Error ? e.message : t('aiConsole.quota.loadError') });
                }
            });
        return () => {
            cancelled = true;
        };
        // hydrate/t are stable enough; reloadKey drives manual retries.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [jwtToken, reloadKey]);

    const retry = useCallback(() => {
        setState({ status: 'loading' });
        setReloadKey((k) => k + 1);
    }, []);

    if (state.status === 'loading') {
        return (
            <div
                role="status"
                aria-label={t('aiConsole.quota.loading')}
                data-testid="ai-quota-loading"
                className="space-y-3 rounded-xl border border-[rgba(60,50,40,0.10)] bg-parchment-50 p-5"
            >
                <Skeleton className="h-6 w-1/3" />
                <Skeleton className="h-4 w-2/3" />
                <span className="sr-only">{t('aiConsole.quota.loading')}</span>
            </div>
        );
    }

    if (state.status === 'error') {
        return (
            <Alert variant="destructive" role="alert" data-testid="ai-quota-error">
                <AlertTitle>{t('aiConsole.quota.title')}</AlertTitle>
                <AlertDescription className="space-y-3">
                    <p className="break-words">{state.message}</p>
                    <Button type="button" variant="secondary" size="sm" onClick={retry}>
                        <RefreshCw className="h-3.5 w-3.5" aria-hidden="true" />
                        {t('aiConsole.error.retry')}
                    </Button>
                </AlertDescription>
            </Alert>
        );
    }

    return (
        <QuotaForm
            config={state.config}
            jwtToken={jwtToken}
            hydrate={hydrate}
            onChange={(config) => setState({ status: 'ready', config })}
        />
    );
}

interface QuotaFormProps extends QuotaConfigCardProps {
    config: AnalysisQuotaConfig;
    onChange: (config: AnalysisQuotaConfig) => void;
}

function QuotaForm({ config, jwtToken, hydrate, onChange }: QuotaFormProps) {
    const { t, i18n } = useTranslation();
    const { toast } = useToast();
    const saved = config.effective;
    const [form, setForm] = useState<AnalysisQuotaSettings>(saved);
    const [serverErrors, setServerErrors] = useState<Record<string, string>>({});
    const [actionError, setActionError] = useState<string | null>(null);
    const [saving, setSaving] = useState(false);
    const [resetting, setResetting] = useState(false);
    const [confirmOpen, setConfirmOpen] = useState(false);

    const clientErrors = useMemo(() => validateQuotaSettings(form), [form]);
    const isValid = Object.keys(clientErrors).length === 0;
    const isDirty = !quotaSettingsEqual(form, saved);
    const busy = saving || resetting;
    const errorFor = (path: string): string | undefined =>
        clientErrors[path] ? t(clientErrors[path]) : serverErrors[path];

    const edit = (next: AnalysisQuotaSettings) => {
        setForm(next);
        setServerErrors({});
        setActionError(null);
    };

    const adopt = (next: AnalysisQuotaConfig) => {
        setForm(next.effective);
        setServerErrors({});
        setActionError(null);
        onChange(next);
    };

    const handleSave = async () => {
        setSaving(true);
        setServerErrors({});
        setActionError(null);
        try {
            adopt(await updateAnalysisQuotaConfig({ settings: form, jwtToken, hydrate }));
            toast({ title: t('aiConsole.quota.toast.savedTitle'), description: t('aiConsole.quota.toast.savedBody') });
        } catch (e) {
            if (e instanceof AiConsoleApiError && e.status === 400) {
                const fields = mapServerErrors(e.fieldErrors);
                setServerErrors(fields);
                const summary = Object.entries(fields).map(([path, msg]) => `${path}: ${msg}`).join(' | ');
                setActionError(summary || e.message);
            } else {
                setActionError(e instanceof Error ? e.message : t('aiConsole.toast.genericError'));
            }
        } finally {
            setSaving(false);
        }
    };

    const handleReset = async () => {
        setResetting(true);
        try {
            adopt(await resetAnalysisQuotaConfig({ jwtToken, hydrate }));
            setConfirmOpen(false);
            toast({ title: t('aiConsole.quota.toast.resetTitle'), description: t('aiConsole.quota.toast.resetBody') });
        } catch (e) {
            setConfirmOpen(false);
            setActionError(e instanceof Error ? e.message : t('aiConsole.toast.genericError'));
        } finally {
            setResetting(false);
        }
    };

    const updatedAt = config.updatedAt ? new Date(config.updatedAt) : null;
    const updatedAtText =
        updatedAt && !Number.isNaN(updatedAt.getTime()) ? updatedAt.toLocaleString(i18n.language) : config.updatedAt;
    const defaults = config.defaults;

    return (
        <Card aria-labelledby="ai-quota-title" role="region" data-testid="ai-quota-card">
            <CardHeader>
                <div className="flex flex-wrap items-start justify-between gap-2">
                    <CardTitle id="ai-quota-title">{t('aiConsole.quota.title')}</CardTitle>
                    <div className="flex flex-wrap items-center gap-2">
                        {isDirty && <Badge variant="gold">{t('aiConsole.badge.unsaved')}</Badge>}
                        <Badge variant={config.isOverridden ? 'blue' : 'parch'}>
                            {config.isOverridden ? t('aiConsole.badge.overridden') : t('aiConsole.badge.default')}
                        </Badge>
                    </div>
                </div>
                <CardDescription>{t('aiConsole.quota.description')}</CardDescription>
                {config.isOverridden && (config.updatedBy || updatedAtText) && (
                    <p className="text-xs text-slate-zen300">
                        {t('aiConsole.updatedBy', { by: config.updatedBy ?? '—', at: updatedAtText ?? '—' })}
                    </p>
                )}
                <p className="text-xs text-slate-zen300">
                    {t('aiConsole.quota.defaultsLine', {
                        max: defaults.maxPerWindow,
                        hours: defaults.windowHours,
                        state: defaults.enabled ? t('aiConsole.quota.stateOn') : t('aiConsole.quota.stateOff'),
                    })}
                </p>
            </CardHeader>

            <CardContent className="space-y-5">
                <div className="flex items-center gap-3">
                    <Switch
                        id="ai-quota-enabled"
                        checked={form.enabled}
                        onCheckedChange={(enabled) => edit({ ...form, enabled })}
                        disabled={busy}
                    />
                    <label htmlFor="ai-quota-enabled" className="font-sans text-sm text-ink-300">
                        {t('aiConsole.quota.enabled')}
                    </label>
                </div>
                {errorFor('enabled') && (
                    <p role="alert" className="text-xs text-blood-300">{errorFor('enabled')}</p>
                )}

                <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <Field
                        id="ai-quota-max"
                        label={t('aiConsole.quota.maxPerWindow')}
                        hint={t('aiConsole.quota.maxPerWindowHint')}
                        error={errorFor('maxPerWindow')}
                    >
                        <NumberField
                            id="ai-quota-max"
                            value={Number.isNaN(form.maxPerWindow) ? null : form.maxPerWindow}
                            // An emptied field becomes NaN so the schema flags it (both numbers are required).
                            onChange={(v) => edit({ ...form, maxPerWindow: v ?? Number.NaN })}
                            min={QUOTA_LIMITS.maxPerWindow.min}
                            max={QUOTA_LIMITS.maxPerWindow.max}
                            step={1}
                            disabled={busy}
                            invalid={!!errorFor('maxPerWindow')}
                            describedBy="ai-quota-max-hint"
                        />
                    </Field>
                    <Field
                        id="ai-quota-hours"
                        label={t('aiConsole.quota.windowHours')}
                        hint={t('aiConsole.quota.windowHoursHint')}
                        error={errorFor('windowHours')}
                    >
                        <NumberField
                            id="ai-quota-hours"
                            value={Number.isNaN(form.windowHours) ? null : form.windowHours}
                            onChange={(v) => edit({ ...form, windowHours: v ?? Number.NaN })}
                            min={QUOTA_LIMITS.windowHours.min}
                            max={QUOTA_LIMITS.windowHours.max}
                            step={1}
                            disabled={busy}
                            invalid={!!errorFor('windowHours')}
                            describedBy="ai-quota-hours-hint"
                        />
                    </Field>
                </div>

                {actionError && (
                    <Alert variant="destructive" role="alert">
                        <AlertTitle>{t('aiConsole.toast.saveFailedTitle')}</AlertTitle>
                        <AlertDescription className="break-words">{actionError}</AlertDescription>
                    </Alert>
                )}
            </CardContent>

            <CardFooter className="flex-wrap gap-2">
                <Button type="button" variant="primary" onClick={handleSave} disabled={!isDirty || !isValid || busy}>
                    {saving ? (
                        <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                    ) : (
                        <Save className="h-4 w-4" aria-hidden="true" />
                    )}
                    {saving ? t('aiConsole.actions.saving') : t('aiConsole.actions.save')}
                </Button>
                {isDirty && (
                    <Button type="button" variant="ghost" onClick={() => edit(saved)} disabled={busy}>
                        {t('aiConsole.actions.discard')}
                    </Button>
                )}
                <Button
                    type="button"
                    variant="ghost"
                    className="sm:ml-auto"
                    onClick={() => setConfirmOpen(true)}
                    disabled={!config.isOverridden || busy}
                >
                    <RotateCcw className="h-4 w-4" aria-hidden="true" />
                    {t('aiConsole.actions.reset')}
                </Button>
            </CardFooter>

            <AlertDialog
                open={confirmOpen}
                title={t('aiConsole.quota.confirmReset.title')}
                description={t('aiConsole.quota.confirmReset.body')}
                confirmLabel={t('aiConsole.confirmReset.confirm')}
                cancelLabel={t('aiConsole.confirmReset.cancel')}
                onConfirm={handleReset}
                onCancel={() => setConfirmOpen(false)}
                destructive
                busy={resetting}
            />
        </Card>
    );
}

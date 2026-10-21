import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Info } from 'lucide-react';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { AlertDialog } from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { useToast } from '@/hooks/use-toast';
import {
    AiConsoleApiError,
    resetAiFeatureConfig,
    testAiFeatureConfig,
    updateAiFeatureConfig,
} from '@/services/api';
import { FeatureCardActions } from './FeatureCardActions';
import { ModelSettingsFields } from './ModelSettingsFields';
import { PipelinePhasesTable } from './PipelinePhasesTable';
import { SearchSettingsFields } from './SearchSettingsFields';
import { TestResultAlert } from './TestResultAlert';
import { inferThinkingMode, mapServerErrors, normalizeSettings, settingsEqual, validateSettings } from './aiConsoleSchema';
import { AiFeatureConfig, AiFeatureSettings, AiModelInfo, AiModelTestResult, ThinkingMode } from './types';

interface FeatureConfigCardProps {
    feature: AiFeatureConfig;
    models: AiModelInfo[];
    jwtToken: string | null;
    hydrate?: () => Promise<void> | void;
    onUpdated: (config: AiFeatureConfig) => void;
}

export function FeatureConfigCard({ feature, models, jwtToken, hydrate, onUpdated }: FeatureConfigCardProps) {
    const { t, i18n } = useTranslation();
    const { toast } = useToast();
    const idPrefix = `ai-${feature.key}`;
    const featureTitle = t(`aiConsole.features.${feature.key}.title`, { defaultValue: feature.key });

    const savedSettings = useMemo(() => normalizeSettings(feature.effective), [feature.effective]);
    const [form, setForm] = useState<AiFeatureSettings>(savedSettings);
    const [customMode, setCustomMode] = useState(() => !models.some((m) => m.id === savedSettings.modelId));
    const [customThinking, setCustomThinking] = useState<ThinkingMode>(() => inferThinkingMode(savedSettings));
    const [serverErrors, setServerErrors] = useState<Record<string, string>>({});
    const [actionError, setActionError] = useState<string | null>(null);
    const [saving, setSaving] = useState(false);
    const [resetting, setResetting] = useState(false);
    const [confirmOpen, setConfirmOpen] = useState(false);
    const [testing, setTesting] = useState(false);
    const [testResult, setTestResult] = useState<AiModelTestResult | null>(null);
    const [testError, setTestError] = useState<string | null>(null);

    const modelInfo = models.find((m) => m.id === form.modelId);
    const thinkingMode: ThinkingMode = modelInfo?.thinkingMode ?? customThinking;
    const clientErrors = useMemo(() => validateSettings(form), [form]);
    const isValid = Object.keys(clientErrors).length === 0;
    const isDirty = !settingsEqual(form, savedSettings);
    const busy = saving || resetting;

    const errorFor = (path: string): string | undefined =>
        clientErrors[path] ? t(clientErrors[path]) : serverErrors[path];

    const edit = (next: AiFeatureSettings) => {
        setForm(next);
        setServerErrors({});
        setActionError(null);
    };

    const adopt = (config: AiFeatureConfig) => {
        const next = normalizeSettings(config.effective);
        setForm(next);
        setCustomMode(!models.some((m) => m.id === next.modelId));
        setCustomThinking(inferThinkingMode(next));
        setServerErrors({});
        setActionError(null);
        setTestResult(null);
        setTestError(null);
        onUpdated(config);
    };

    const handleSave = async () => {
        setSaving(true);
        setServerErrors({});
        setActionError(null);
        try {
            const updated = await updateAiFeatureConfig({ key: feature.key, settings: normalizeSettings(form), jwtToken, hydrate });
            adopt(updated);
            toast({ title: t('aiConsole.toast.savedTitle'), description: t('aiConsole.toast.savedBody', { feature: featureTitle }) });
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
            const updated = await resetAiFeatureConfig({ key: feature.key, jwtToken, hydrate });
            setConfirmOpen(false);
            adopt(updated);
            toast({ title: t('aiConsole.toast.resetTitle'), description: t('aiConsole.toast.resetBody', { feature: featureTitle }) });
        } catch (e) {
            setConfirmOpen(false);
            setActionError(e instanceof Error ? e.message : t('aiConsole.toast.genericError'));
        } finally {
            setResetting(false);
        }
    };

    const handleTest = async () => {
        setTesting(true);
        setTestResult(null);
        setTestError(null);
        try {
            setTestResult(await testAiFeatureConfig({ key: feature.key, settings: normalizeSettings(form), jwtToken, hydrate }));
        } catch (e) {
            setTestError(e instanceof Error ? e.message : t('aiConsole.toast.genericError'));
        } finally {
            setTesting(false);
        }
    };

    const handleDiscard = () => {
        edit(savedSettings);
        setCustomMode(!models.some((m) => m.id === savedSettings.modelId));
        setCustomThinking(inferThinkingMode(savedSettings));
    };

    const featureDescription = t(`aiConsole.features.${feature.key}.description`, { defaultValue: '' });
    const updatedAt = feature.updatedAt ? new Date(feature.updatedAt) : null;
    const updatedAtText = updatedAt && !Number.isNaN(updatedAt.getTime()) ? updatedAt.toLocaleString(i18n.language) : feature.updatedAt;
    const showCacheNote = feature.kind === 'pipeline' && !!modelInfo && modelInfo.supportsExplicitCache === false;

    return (
        <Card aria-labelledby={`${idPrefix}-title`} role="region" data-testid={`ai-feature-${feature.key}`}>
            <CardHeader>
                <div className="flex flex-wrap items-start justify-between gap-2">
                    <CardTitle id={`${idPrefix}-title`}>{featureTitle}</CardTitle>
                    <div className="flex flex-wrap items-center gap-2">
                        {isDirty && <Badge variant="gold">{t('aiConsole.badge.unsaved')}</Badge>}
                        <Badge variant={feature.isOverridden ? 'blue' : 'parch'}>
                            {feature.isOverridden ? t('aiConsole.badge.overridden') : t('aiConsole.badge.default')}
                        </Badge>
                    </div>
                </div>
                {featureDescription && <CardDescription>{featureDescription}</CardDescription>}
                {feature.isOverridden && (feature.updatedBy || updatedAtText) && (
                    <p className="text-xs text-slate-zen300">
                        {t('aiConsole.updatedBy', { by: feature.updatedBy ?? '—', at: updatedAtText ?? '—' })}
                    </p>
                )}
                <p className="text-xs text-slate-zen300">
                    {t('aiConsole.defaultsLine', { model: feature.defaults.modelId, location: feature.defaults.location })}
                </p>
            </CardHeader>

            <CardContent className="space-y-5">
                <ModelSettingsFields
                    idPrefix={idPrefix}
                    kind={feature.kind}
                    settings={form}
                    models={models}
                    requiresVideo={feature.requiresVideo}
                    requiresGoogleSearch={feature.requiresGoogleSearch}
                    customMode={customMode}
                    onCustomModeChange={setCustomMode}
                    thinkingMode={thinkingMode}
                    onThinkingModeChange={setCustomThinking}
                    modelInfo={modelInfo}
                    onChange={edit}
                    errorFor={errorFor}
                    disabled={busy}
                />

                {feature.kind === 'pipeline' && form.phases && (
                    <PipelinePhasesTable
                        idPrefix={idPrefix}
                        phases={form.phases}
                        thinkingMode={thinkingMode}
                        modelInfo={modelInfo}
                        onChange={(phases) => edit({ ...form, phases })}
                        errorFor={errorFor}
                        disabled={busy}
                    />
                )}

                {feature.kind === 'search' && form.search && (
                    <SearchSettingsFields
                        idPrefix={idPrefix}
                        search={form.search}
                        onChange={(search) => edit({ ...form, search })}
                        errorFor={errorFor}
                        disabled={busy}
                    />
                )}

                {showCacheNote && (
                    <Alert variant="info" className="flex gap-3">
                        <Info className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
                        <AlertDescription>{t('aiConsole.phases.implicitCacheNote')}</AlertDescription>
                    </Alert>
                )}

                {actionError && (
                    <Alert variant="destructive" role="alert">
                        <AlertTitle>{t('aiConsole.toast.saveFailedTitle')}</AlertTitle>
                        <AlertDescription className="break-words">{actionError}</AlertDescription>
                    </Alert>
                )}

                <div aria-live="polite">
                    <TestResultAlert result={testResult} requestError={testError} />
                </div>
            </CardContent>

            <CardFooter className="flex-wrap gap-2">
                <FeatureCardActions
                    testing={testing}
                    saving={saving}
                    busy={busy}
                    isDirty={isDirty}
                    isValid={isValid}
                    isOverridden={feature.isOverridden}
                    onTest={handleTest}
                    onSave={handleSave}
                    onDiscard={handleDiscard}
                    onReset={() => setConfirmOpen(true)}
                />
            </CardFooter>

            <AlertDialog
                open={confirmOpen}
                title={t('aiConsole.confirmReset.title')}
                description={t('aiConsole.confirmReset.body', { feature: featureTitle })}
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

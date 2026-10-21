import { useTranslation } from 'react-i18next';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Field } from './Field';
import { NumberField } from './NumberField';
import { ThinkingControl } from './ThinkingControl';
import { CUSTOM_MODEL_VALUE, LIMITS, applyModelChange, applyThinkingMode } from './aiConsoleSchema';
import { AiFeatureSettings, AiModelInfo, FeatureKind, ThinkingMode } from './types';

interface ModelSettingsFieldsProps {
    idPrefix: string;
    kind: FeatureKind;
    settings: AiFeatureSettings;
    models: AiModelInfo[];
    requiresVideo: boolean;
    requiresGoogleSearch: boolean;
    customMode: boolean;
    onCustomModeChange: (custom: boolean) => void;
    thinkingMode: ThinkingMode;
    onThinkingModeChange: (mode: ThinkingMode) => void;
    modelInfo: AiModelInfo | undefined;
    onChange: (next: AiFeatureSettings) => void;
    errorFor: (path: string) => string | undefined;
    disabled?: boolean;
}

export function ModelSettingsFields({
    idPrefix,
    kind,
    settings,
    models,
    requiresVideo,
    requiresGoogleSearch,
    customMode,
    onCustomModeChange,
    thinkingMode,
    onThinkingModeChange,
    modelInfo,
    onChange,
    errorFor,
    disabled,
}: ModelSettingsFieldsProps) {
    const { t } = useTranslation();
    const showLocationSelect = !customMode && !!modelInfo && modelInfo.supportedLocations.length > 0;
    const isPipeline = kind === 'pipeline';
    const thinkingPath = thinkingMode === 'level' ? 'thinkingLevel' : 'thinkingBudget';

    const unsupportedReason = (m: AiModelInfo): string | null => {
        if (requiresVideo && !m.supportsVideo) return t('aiConsole.fields.noVideo');
        if (requiresGoogleSearch && !m.supportsGoogleSearch) return t('aiConsole.fields.noGoogleSearch');
        return null;
    };

    const handleModelSelect = (value: string) => {
        if (value === CUSTOM_MODEL_VALUE) {
            onCustomModeChange(true);
            return;
        }
        onCustomModeChange(false);
        const model = models.find((m) => m.id === value);
        onChange(applyModelChange(settings, value, model));
        if (model) onThinkingModeChange(model.thinkingMode);
    };

    const handleCustomId = (value: string) => {
        const known = models.find((m) => m.id === value);
        onChange(known ? applyModelChange(settings, value, known) : { ...settings, modelId: value });
    };

    const handleThinkingMode = (value: string) => {
        const mode = value as ThinkingMode;
        onThinkingModeChange(mode);
        onChange(applyThinkingMode(settings, mode));
    };

    const modelId = `${idPrefix}-model`;
    const customId = `${idPrefix}-custom-model`;
    const locationId = `${idPrefix}-location`;

    return (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div className="flex flex-col gap-4">
                <Field id={modelId} label={t('aiConsole.fields.model')}>
                    <Select
                        value={customMode ? CUSTOM_MODEL_VALUE : settings.modelId}
                        onValueChange={handleModelSelect}
                        disabled={disabled}
                    >
                        <SelectTrigger id={modelId} aria-label={t('aiConsole.fields.model')}>
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            {models.map((m) => {
                                const reason = unsupportedReason(m);
                                return (
                                    <SelectItem key={m.id} value={m.id} disabled={!!reason}>
                                        <span className="inline-flex flex-wrap items-center gap-2">
                                            <span>{m.displayName}</span>
                                            <Badge variant={m.launchStage === 'GA' ? 'blue' : 'gold'}>{m.launchStage}</Badge>
                                            {reason && <span className="text-xs text-slate-zen300">{reason}</span>}
                                        </span>
                                    </SelectItem>
                                );
                            })}
                            <SelectItem value={CUSTOM_MODEL_VALUE}>{t('aiConsole.fields.customModelOption')}</SelectItem>
                        </SelectContent>
                    </Select>
                    {!customMode && modelInfo?.notes && (
                        <p className="text-xs text-slate-zen300">{modelInfo.notes}</p>
                    )}
                </Field>

                {customMode && (
                    <Field
                        id={customId}
                        label={t('aiConsole.fields.customModelId')}
                        hint={t('aiConsole.fields.customModelHint')}
                        error={errorFor('modelId')}
                    >
                        <Input
                            id={customId}
                            value={settings.modelId}
                            onChange={(e) => handleCustomId(e.target.value.trim())}
                            placeholder="gemini-x-y"
                            autoComplete="off"
                            spellCheck={false}
                            disabled={disabled}
                            aria-invalid={!!errorFor('modelId') || undefined}
                            aria-describedby={`${customId}-hint`}
                        />
                    </Field>
                )}
                {!customMode && errorFor('modelId') && (
                    <p role="alert" className="text-xs text-blood-300">{errorFor('modelId')}</p>
                )}
            </div>

            <Field id={locationId} label={t('aiConsole.fields.location')} error={errorFor('location')}>
                {showLocationSelect ? (
                    <Select
                        value={settings.location}
                        onValueChange={(v) => onChange({ ...settings, location: v })}
                        disabled={disabled}
                    >
                        <SelectTrigger id={locationId} aria-label={t('aiConsole.fields.location')}>
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            {modelInfo!.supportedLocations.map((loc) => (
                                <SelectItem key={loc} value={loc}>{loc}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                ) : (
                    <Input
                        id={locationId}
                        value={settings.location}
                        onChange={(e) => onChange({ ...settings, location: e.target.value.trim() })}
                        placeholder="global"
                        autoComplete="off"
                        spellCheck={false}
                        disabled={disabled}
                        aria-invalid={!!errorFor('location') || undefined}
                    />
                )}
            </Field>

            {!modelInfo && (
                <Field id={`${idPrefix}-thinking-style`} label={t('aiConsole.fields.thinkingStyle')}>
                    <Select value={thinkingMode} onValueChange={handleThinkingMode} disabled={disabled}>
                        <SelectTrigger id={`${idPrefix}-thinking-style`} aria-label={t('aiConsole.fields.thinkingStyle')}>
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="level">{t('aiConsole.fields.thinkingStyleLevel')}</SelectItem>
                            <SelectItem value="budget">{t('aiConsole.fields.thinkingStyleBudget')}</SelectItem>
                        </SelectContent>
                    </Select>
                </Field>
            )}

            {!isPipeline && (
                <>
                    <Field
                        id={`${idPrefix}-thinking`}
                        label={thinkingMode === 'level' ? t('aiConsole.fields.thinkingLevel') : t('aiConsole.fields.thinkingBudget')}
                        hint={thinkingMode === 'budget' ? t('aiConsole.fields.thinkingBudgetHint') : undefined}
                        error={errorFor(thinkingPath)}
                    >
                        <ThinkingControl
                            id={`${idPrefix}-thinking`}
                            mode={thinkingMode}
                            level={settings.thinkingLevel}
                            budget={settings.thinkingBudget}
                            onLevelChange={(thinkingLevel) => onChange({ ...settings, thinkingLevel })}
                            onBudgetChange={(thinkingBudget) => onChange({ ...settings, thinkingBudget })}
                            invalid={!!errorFor(thinkingPath)}
                            disabled={disabled}
                        />
                    </Field>

                    <Field
                        id={`${idPrefix}-temperature`}
                        label={t('aiConsole.fields.temperature')}
                        hint={modelInfo?.samplingParamsIgnored ? t('aiConsole.fields.temperatureIgnored') : t('aiConsole.fields.temperatureHint')}
                        error={errorFor('temperature')}
                    >
                        <NumberField
                            id={`${idPrefix}-temperature`}
                            value={settings.temperature}
                            onChange={(temperature) => onChange({ ...settings, temperature })}
                            min={LIMITS.temperature.min}
                            max={LIMITS.temperature.max}
                            step={0.1}
                            placeholder={t('aiConsole.fields.modelDefault')}
                            disabled={disabled || modelInfo?.samplingParamsIgnored}
                            invalid={!!errorFor('temperature')}
                            describedBy={`${idPrefix}-temperature-hint`}
                        />
                    </Field>

                    <Field
                        id={`${idPrefix}-max-tokens`}
                        label={t('aiConsole.fields.maxOutputTokens')}
                        error={errorFor('maxOutputTokens')}
                    >
                        <NumberField
                            id={`${idPrefix}-max-tokens`}
                            value={settings.maxOutputTokens}
                            onChange={(maxOutputTokens) => onChange({ ...settings, maxOutputTokens })}
                            min={LIMITS.maxOutputTokens.min}
                            max={LIMITS.maxOutputTokens.max}
                            step={1}
                            placeholder={t('aiConsole.fields.modelDefault')}
                            disabled={disabled}
                            invalid={!!errorFor('maxOutputTokens')}
                        />
                    </Field>
                </>
            )}
        </div>
    );
}

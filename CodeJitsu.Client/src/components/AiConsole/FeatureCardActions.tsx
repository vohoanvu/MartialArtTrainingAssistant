import { useTranslation } from 'react-i18next';
import { FlaskConical, Loader2, RotateCcw, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface FeatureCardActionsProps {
    testing: boolean;
    saving: boolean;
    busy: boolean;
    isDirty: boolean;
    isValid: boolean;
    isOverridden: boolean;
    onTest: () => void;
    onSave: () => void;
    onDiscard: () => void;
    onReset: () => void;
}

export function FeatureCardActions({
    testing,
    saving,
    busy,
    isDirty,
    isValid,
    isOverridden,
    onTest,
    onSave,
    onDiscard,
    onReset,
}: FeatureCardActionsProps) {
    const { t } = useTranslation();

    return (
        <>
            <Button type="button" variant="secondary" onClick={onTest} disabled={testing || busy || !isValid} aria-busy={testing}>
                {testing ? (
                    <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                ) : (
                    <FlaskConical className="h-4 w-4" aria-hidden="true" />
                )}
                {testing ? t('aiConsole.actions.testing') : t('aiConsole.actions.test')}
            </Button>
            <Button type="button" variant="primary" onClick={onSave} disabled={!isDirty || !isValid || busy || testing}>
                {saving ? (
                    <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                ) : (
                    <Save className="h-4 w-4" aria-hidden="true" />
                )}
                {saving ? t('aiConsole.actions.saving') : t('aiConsole.actions.save')}
            </Button>
            {isDirty && (
                <Button type="button" variant="ghost" onClick={onDiscard} disabled={busy}>
                    {t('aiConsole.actions.discard')}
                </Button>
            )}
            <Button
                type="button"
                variant="ghost"
                className="sm:ml-auto"
                onClick={onReset}
                disabled={!isOverridden || busy || testing}
            >
                <RotateCcw className="h-4 w-4" aria-hidden="true" />
                {t('aiConsole.actions.reset')}
            </Button>
        </>
    );
}

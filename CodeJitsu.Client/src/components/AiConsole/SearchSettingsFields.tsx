import { useTranslation } from 'react-i18next';
import { Switch } from '@/components/ui/switch';
import { Field } from './Field';
import { NumberField } from './NumberField';
import { LIMITS } from './aiConsoleSchema';
import { AiSearchSettings } from './types';

interface SearchSettingsFieldsProps {
    idPrefix: string;
    search: AiSearchSettings;
    onChange: (search: AiSearchSettings) => void;
    errorFor: (path: string) => string | undefined;
    disabled?: boolean;
}

export function SearchSettingsFields({ idPrefix, search, onChange, errorFor, disabled }: SearchSettingsFieldsProps) {
    const { t } = useTranslation();
    const maxId = `${idPrefix}-max-results`;
    const verifyId = `${idPrefix}-verify-youtube`;

    return (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <Field id={maxId} label={t('aiConsole.search.maxResults')} error={errorFor('search.maxResults')}>
                <NumberField
                    id={maxId}
                    value={Number.isNaN(search.maxResults) ? null : search.maxResults}
                    // An emptied field becomes NaN so the schema flags it (maxResults is required).
                    onChange={(v) => onChange({ ...search, maxResults: v ?? Number.NaN })}
                    min={LIMITS.maxResults.min}
                    max={LIMITS.maxResults.max}
                    step={1}
                    disabled={disabled}
                    invalid={!!errorFor('search.maxResults')}
                />
            </Field>
            <div className="flex items-center gap-3 md:pt-6">
                <Switch
                    id={verifyId}
                    checked={search.verifyWithYouTube}
                    onCheckedChange={(verifyWithYouTube) => onChange({ ...search, verifyWithYouTube })}
                    disabled={disabled}
                />
                <label htmlFor={verifyId} className="font-sans text-sm text-ink-300">
                    {t('aiConsole.search.verifyWithYouTube')}
                </label>
            </div>
        </div>
    );
}

import { useTranslation } from 'react-i18next';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { NumberField } from './NumberField';
import { LIMITS } from './aiConsoleSchema';
import { THINKING_LEVELS, ThinkingLevel, ThinkingMode } from './types';

interface ThinkingControlProps {
    id?: string;
    mode: ThinkingMode;
    level: ThinkingLevel | null;
    budget: number | null;
    onLevelChange: (level: ThinkingLevel | null) => void;
    onBudgetChange: (budget: number | null) => void;
    invalid?: boolean;
    disabled?: boolean;
    'aria-label'?: string;
    className?: string;
}

const MODEL_DEFAULT = '__default__';

/** Level select (LOW/MEDIUM/HIGH or model default) or numeric budget input, depending on `mode`. */
export function ThinkingControl({
    id,
    mode,
    level,
    budget,
    onLevelChange,
    onBudgetChange,
    invalid,
    disabled,
    'aria-label': ariaLabel,
    className,
}: ThinkingControlProps) {
    const { t } = useTranslation();

    if (mode === 'budget') {
        return (
            <NumberField
                id={id}
                value={budget}
                onChange={onBudgetChange}
                min={LIMITS.thinkingBudget.min}
                max={LIMITS.thinkingBudget.max}
                step={1}
                placeholder={t('aiConsole.fields.modelDefault')}
                invalid={invalid}
                disabled={disabled}
                aria-label={ariaLabel}
                className={className}
            />
        );
    }

    return (
        <Select
            value={level ?? MODEL_DEFAULT}
            onValueChange={(v) => onLevelChange(v === MODEL_DEFAULT ? null : (v as ThinkingLevel))}
            disabled={disabled}
        >
            <SelectTrigger id={id} aria-label={ariaLabel} aria-invalid={invalid || undefined} className={className}>
                <SelectValue />
            </SelectTrigger>
            <SelectContent>
                <SelectItem value={MODEL_DEFAULT}>{t('aiConsole.fields.modelDefault')}</SelectItem>
                {THINKING_LEVELS.map((l) => (
                    <SelectItem key={l} value={l}>
                        {t(`aiConsole.thinkingLevels.${l}`)}
                    </SelectItem>
                ))}
            </SelectContent>
        </Select>
    );
}

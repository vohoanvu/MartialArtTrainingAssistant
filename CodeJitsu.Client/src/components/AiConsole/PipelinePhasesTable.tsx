import { useTranslation } from 'react-i18next';
import { NumberField } from './NumberField';
import { ThinkingControl } from './ThinkingControl';
import { LIMITS } from './aiConsoleSchema';
import { AiModelInfo, AiPhaseSettings, AiPhases, PHASE_KEYS, PhaseKey, ThinkingMode } from './types';

interface PipelinePhasesTableProps {
    idPrefix: string;
    phases: AiPhases;
    thinkingMode: ThinkingMode;
    modelInfo: AiModelInfo | undefined;
    onChange: (phases: AiPhases) => void;
    errorFor: (path: string) => string | undefined;
    disabled?: boolean;
}

const headerClass = 'px-2 py-2 text-left font-sans text-xs font-medium text-slate-zen400 whitespace-nowrap';
const cellClass = 'px-2 py-2 align-top';

/** One row per pipeline phase; columns: thinking, max output tokens, temperature, fps. */
export function PipelinePhasesTable({
    idPrefix,
    phases,
    thinkingMode,
    modelInfo,
    onChange,
    errorFor,
    disabled,
}: PipelinePhasesTableProps) {
    const { t } = useTranslation();
    const temperatureLocked = !!modelInfo?.samplingParamsIgnored;

    const patch = (key: PhaseKey, change: Partial<AiPhaseSettings>) =>
        onChange({ ...phases, [key]: { ...phases[key], ...change } });

    const thinkingPath = thinkingMode === 'level' ? 'thinkingLevel' : 'thinkingBudget';

    const rowErrors = (key: PhaseKey): string[] =>
        [thinkingPath, 'maxOutputTokens', 'temperature', 'fps']
            .map((f) => errorFor(`phases.${key}.${f}`))
            .filter((m): m is string => !!m);

    return (
        <div className="flex flex-col gap-2">
            <h4 className="font-sans text-sm font-semibold text-ink-300">{t('aiConsole.phases.title')}</h4>
            <div className="overflow-x-auto rounded-lg border border-[rgba(60,50,40,0.10)]">
                <table className="w-full min-w-[640px] border-collapse">
                    <caption className="sr-only">{t('aiConsole.phases.title')}</caption>
                    <thead className="bg-parchment-100">
                        <tr>
                            <th scope="col" className={headerClass}>{t('aiConsole.phases.phase')}</th>
                            <th scope="col" className={headerClass}>
                                {thinkingMode === 'level'
                                    ? t('aiConsole.fields.thinkingLevel')
                                    : t('aiConsole.fields.thinkingBudget')}
                            </th>
                            <th scope="col" className={headerClass}>{t('aiConsole.fields.maxOutputTokens')}</th>
                            <th scope="col" className={headerClass}>{t('aiConsole.fields.temperature')}</th>
                            <th scope="col" className={headerClass}>{t('aiConsole.phases.fps')}</th>
                        </tr>
                    </thead>
                    <tbody>
                        {PHASE_KEYS.map((key) => {
                            const phase = phases[key];
                            const name = t(`aiConsole.phases.names.${key}`);
                            const id = `${idPrefix}-${key}`;
                            const errors = rowErrors(key);
                            return (
                                <tr key={key} className="border-t border-[rgba(60,50,40,0.08)]">
                                    <th scope="row" className={`${cellClass} text-left font-sans text-sm font-medium text-ink-400 whitespace-nowrap`}>
                                        {name}
                                        {errors.length > 0 && (
                                            <p role="alert" className="mt-1 text-xs font-normal text-blood-300 whitespace-normal">
                                                {errors.join(' ')}
                                            </p>
                                        )}
                                    </th>
                                    <td className={cellClass}>
                                        <ThinkingControl
                                            id={`${id}-thinking`}
                                            mode={thinkingMode}
                                            level={phase.thinkingLevel}
                                            budget={phase.thinkingBudget}
                                            onLevelChange={(thinkingLevel) => patch(key, { thinkingLevel })}
                                            onBudgetChange={(thinkingBudget) => patch(key, { thinkingBudget })}
                                            invalid={!!errorFor(`phases.${key}.${thinkingPath}`)}
                                            disabled={disabled}
                                            aria-label={`${name}: ${t('aiConsole.fields.thinking')}`}
                                        />
                                    </td>
                                    <td className={cellClass}>
                                        <NumberField
                                            id={`${id}-max-tokens`}
                                            value={phase.maxOutputTokens}
                                            onChange={(maxOutputTokens) => patch(key, { maxOutputTokens })}
                                            min={LIMITS.maxOutputTokens.min}
                                            max={LIMITS.maxOutputTokens.max}
                                            step={1}
                                            placeholder={t('aiConsole.fields.modelDefault')}
                                            invalid={!!errorFor(`phases.${key}.maxOutputTokens`)}
                                            disabled={disabled}
                                            aria-label={`${name}: ${t('aiConsole.fields.maxOutputTokens')}`}
                                        />
                                    </td>
                                    <td className={cellClass}>
                                        <NumberField
                                            id={`${id}-temperature`}
                                            value={phase.temperature}
                                            onChange={(temperature) => patch(key, { temperature })}
                                            min={LIMITS.temperature.min}
                                            max={LIMITS.temperature.max}
                                            step={0.1}
                                            placeholder={t('aiConsole.fields.modelDefault')}
                                            invalid={!!errorFor(`phases.${key}.temperature`)}
                                            disabled={disabled || temperatureLocked}
                                            aria-label={`${name}: ${t('aiConsole.fields.temperature')}`}
                                        />
                                    </td>
                                    <td className={cellClass}>
                                        <NumberField
                                            id={`${id}-fps`}
                                            value={phase.fps}
                                            onChange={(fps) => patch(key, { fps })}
                                            min={LIMITS.fps.min}
                                            max={LIMITS.fps.max}
                                            step={1}
                                            placeholder={t('aiConsole.fields.modelDefault')}
                                            invalid={!!errorFor(`phases.${key}.fps`)}
                                            disabled={disabled}
                                            aria-label={`${name}: ${t('aiConsole.phases.fps')}`}
                                        />
                                    </td>
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            </div>
            <p className="text-xs text-slate-zen300">{t('aiConsole.phases.fpsHint')}</p>
            {temperatureLocked && (
                <p className="text-xs text-slate-zen300">{t('aiConsole.fields.temperatureIgnored')}</p>
            )}
        </div>
    );
}

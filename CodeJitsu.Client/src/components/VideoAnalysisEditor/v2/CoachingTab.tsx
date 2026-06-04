import React from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { KeyStrength, CriticalWeakness, WeaknessSeverity } from '@/types/global';
import { severityKey, severityBadgeClass, formatMs, ALL_SEVERITIES } from './enumLabels';

interface CoachingTabProps {
    strengths: KeyStrength[];
    weaknesses: CriticalWeakness[];
    onChangeStrengths: (s: KeyStrength[]) => void;
    onChangeWeaknesses: (w: CriticalWeakness[]) => void;
    onSeekMs: (ms: number) => void;
}

const fieldClass = 'w-full text-sm bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded px-2 py-1 focus:outline-none focus:border-samurai-400';

const CoachingTab: React.FC<CoachingTabProps> = ({ strengths, weaknesses, onChangeStrengths, onChangeWeaknesses, onSeekMs }) => {
    const { t } = useTranslation();

    const setStrength = (i: number, next: KeyStrength) => onChangeStrengths(strengths.map((s, idx) => (idx === i ? next : s)));
    const setWeakness = (i: number, next: CriticalWeakness) => onChangeWeaknesses(weaknesses.map((w, idx) => (idx === i ? next : w)));

    return (
        <div className="space-y-6">
            {/* Strengths */}
            <section>
                <div className="flex items-center justify-between mb-2">
                    <h3 className="font-serif text-lg font-bold text-green-700">{t('videoReviewV2.coaching.strengthsTitle')}</h3>
                    <Button type="button" size="sm" onClick={() => onChangeStrengths([...strengths, { title: '', explanation: '' }])}>
                        + {t('videoReviewV2.coaching.addStrength')}
                    </Button>
                </div>
                {strengths.length === 0 ? (
                    <p className="text-sm text-slate-zen400">{t('videoReviewV2.coaching.noStrengths')}</p>
                ) : strengths.map((s, i) => (
                    <div key={i} className="rounded-md border border-[rgba(60,50,40,0.12)] bg-parchment-100 p-3 mb-2">
                        <div className="flex items-center gap-2 mb-1">
                            <input className={fieldClass} placeholder={t('videoReviewV2.coaching.titlePlaceholder')} value={s.title} onChange={(e) => setStrength(i, { ...s, title: e.target.value })} />
                            {s.timestampStartMs != null && (
                                <Button type="button" size="sm" variant="secondary" onClick={() => onSeekMs(s.timestampStartMs!)}>▶ {formatMs(s.timestampStartMs)}</Button>
                            )}
                            <Button type="button" size="sm" variant="secondary" className="text-blood-500" onClick={() => onChangeStrengths(strengths.filter((_, idx) => idx !== i))}>✕</Button>
                        </div>
                        <textarea className={fieldClass} rows={2} value={s.explanation ?? ''} onChange={(e) => setStrength(i, { ...s, explanation: e.target.value })} />
                    </div>
                ))}
            </section>

            {/* Weaknesses */}
            <section>
                <div className="flex items-center justify-between mb-2">
                    <h3 className="font-serif text-lg font-bold text-blood-600">{t('videoReviewV2.coaching.weaknessesTitle')}</h3>
                    <Button type="button" size="sm" onClick={() => onChangeWeaknesses([...weaknesses, { title: '', explanation: '', severity: 'Major' }])}>
                        + {t('videoReviewV2.coaching.addWeakness')}
                    </Button>
                </div>
                {weaknesses.length === 0 ? (
                    <p className="text-sm text-slate-zen400">{t('videoReviewV2.coaching.noWeaknesses')}</p>
                ) : weaknesses.map((w, i) => (
                    <div key={i} className="rounded-md border border-[rgba(60,50,40,0.12)] bg-parchment-100 p-3 mb-2">
                        <div className="flex items-center gap-2 mb-1">
                            <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${severityBadgeClass[w.severity]}`}>{t(severityKey(w.severity))}</span>
                            <input className={fieldClass} placeholder={t('videoReviewV2.coaching.titlePlaceholder')} value={w.title} onChange={(e) => setWeakness(i, { ...w, title: e.target.value })} />
                            {w.timestampStartMs != null && (
                                <Button type="button" size="sm" variant="secondary" onClick={() => onSeekMs(w.timestampStartMs!)}>▶ {formatMs(w.timestampStartMs)}</Button>
                            )}
                            <Button type="button" size="sm" variant="secondary" className="text-blood-500" onClick={() => onChangeWeaknesses(weaknesses.filter((_, idx) => idx !== i))}>✕</Button>
                        </div>
                        <textarea className={fieldClass} rows={2} value={w.explanation ?? ''} onChange={(e) => setWeakness(i, { ...w, explanation: e.target.value })} />
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-2">
                            <label className="text-xs text-slate-zen400">
                                {t('videoReviewV2.coaching.severity')}
                                <select className={fieldClass} value={w.severity} onChange={(e) => setWeakness(i, { ...w, severity: e.target.value as WeaknessSeverity })}>
                                    {ALL_SEVERITIES.map((sev) => <option key={sev} value={sev}>{t(severityKey(sev))}</option>)}
                                </select>
                            </label>
                            <label className="text-xs text-slate-zen400">
                                {t('videoReviewV2.coaching.scoringImpact')}
                                <input className={fieldClass} value={w.scoringImpact ?? ''} onChange={(e) => setWeakness(i, { ...w, scoringImpact: e.target.value })} />
                            </label>
                        </div>
                    </div>
                ))}
            </section>
        </div>
    );
};

export default CoachingTab;

import React from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { PrescribedDrill } from '@/types/global';

interface PrescribedDrillsTabProps {
    drills: PrescribedDrill[];
    onChange: (drills: PrescribedDrill[]) => void;
}

const fieldClass = 'w-full text-sm bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded px-2 py-1 focus:outline-none focus:border-samurai-400';

const PrescribedDrillsTab: React.FC<PrescribedDrillsTabProps> = ({ drills, onChange }) => {
    const { t } = useTranslation();
    const setAt = (i: number, next: PrescribedDrill) => onChange(drills.map((d, idx) => (idx === i ? next : d)));

    return (
        <div>
            <div className="flex items-center justify-between mb-3">
                <h3 className="font-serif text-lg font-bold text-ink-400">{t('videoReviewV2.drills.title')}</h3>
                <Button type="button" size="sm" onClick={() => onChange([...drills, { drillName: '', instructions: '', goal: '' }])}>
                    + {t('videoReviewV2.drills.addDrill')}
                </Button>
            </div>

            {drills.length === 0 ? (
                <p className="text-sm text-slate-zen400">{t('videoReviewV2.drills.empty')}</p>
            ) : drills.map((d, i) => (
                <div key={i} className="rounded-md border border-[rgba(60,50,40,0.12)] bg-parchment-100 p-3 mb-3">
                    <div className="flex items-center gap-2 mb-2">
                        <input className={fieldClass} placeholder={t('videoReviewV2.drills.namePlaceholder')} value={d.drillName} onChange={(e) => setAt(i, { ...d, drillName: e.target.value })} />
                        <Button type="button" size="sm" variant="secondary" className="text-blood-500" onClick={() => onChange(drills.filter((_, idx) => idx !== i))}>✕</Button>
                    </div>
                    <label className="text-xs text-slate-zen400 block mb-2">
                        {t('videoReviewV2.drills.goalLabel')}
                        <input className={fieldClass} value={d.goal ?? ''} onChange={(e) => setAt(i, { ...d, goal: e.target.value })} />
                    </label>
                    <label className="text-xs text-slate-zen400 block">
                        {t('videoReviewV2.drills.instructionsLabel')}
                        <textarea className={fieldClass} rows={2} value={d.instructions ?? ''} onChange={(e) => setAt(i, { ...d, instructions: e.target.value })} />
                    </label>
                </div>
            ))}
        </div>
    );
};

export default PrescribedDrillsTab;

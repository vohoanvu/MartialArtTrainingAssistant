import React from 'react';
import { useTranslation } from 'react-i18next';
import GradeGauge from './GradeGauge';

interface AnalysisSummaryHeaderProps {
    visualDna: string;
    matchSummary: string;
    gradeLabel: string;
    eliteTip: string;
    technicalGrade: number;
    onChange: (patch: Partial<{
        visualDna: string;
        matchSummary: string;
        gradeLabel: string;
        eliteTip: string;
        technicalGrade: number;
    }>) => void;
}

const fieldClass = 'w-full text-sm bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded px-2 py-1 focus:outline-none focus:border-samurai-400';

const AnalysisSummaryHeader: React.FC<AnalysisSummaryHeaderProps> = ({
    visualDna, matchSummary, gradeLabel, eliteTip, technicalGrade, onChange,
}) => {
    const { t } = useTranslation();

    return (
        <div className="rounded-md border border-[rgba(60,50,40,0.12)] bg-parchment-100 p-4 mb-4">
            <div className="flex items-start gap-4">
                <div className="flex flex-col items-center">
                    <GradeGauge grade={technicalGrade} editable onChange={(g) => onChange({ technicalGrade: g })} />
                    <span className="mt-1 text-xs text-slate-zen400">{t('videoReviewV2.summary.gradeLabel')}</span>
                </div>
                <div className="flex-1 space-y-2">
                    <label className="block text-xs text-slate-zen400">
                        {t('videoReviewV2.summary.gradeLabelField')}
                        <input className={fieldClass} value={gradeLabel} onChange={(e) => onChange({ gradeLabel: e.target.value })} />
                    </label>
                    <div className="rounded bg-samurai-50 border-l-4 border-samurai-400 p-2">
                        <span className="block text-xs font-semibold text-samurai-600">{t('videoReviewV2.summary.eliteTipTitle')}</span>
                        <input className={`${fieldClass} mt-1`} value={eliteTip} onChange={(e) => onChange({ eliteTip: e.target.value })} />
                    </div>
                </div>
            </div>

            <label className="block text-xs text-slate-zen400 mt-3">
                {t('videoReviewV2.summary.matchSummaryTitle')}
                <textarea className={fieldClass} rows={3} value={matchSummary} onChange={(e) => onChange({ matchSummary: e.target.value })} />
            </label>

            <label className="block text-xs text-slate-zen400 mt-3">
                {t('videoReviewV2.summary.visualDnaTitle')}
                <textarea className={fieldClass} rows={3} value={visualDna} onChange={(e) => onChange({ visualDna: e.target.value })} />
            </label>
        </div>
    );
};

export default AnalysisSummaryHeader;

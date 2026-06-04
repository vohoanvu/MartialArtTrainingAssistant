import React, { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { AnalysisV2Dto, CoachingReport, MatchEvent, KeyStrength, CriticalWeakness, PrescribedDrill } from '@/types/global';
import AnalysisSummaryHeader from './AnalysisSummaryHeader';
import MatchTimelineTab from './MatchTimelineTab';
import CoachingTab from './CoachingTab';
import PrescribedDrillsTab from './PrescribedDrillsTab';

interface AnalysisV2PanelProps {
    analysis: AnalysisV2Dto;
    onSeekMs: (ms: number) => void;
    onSave: (draft: AnalysisV2Dto) => Promise<void>;
    isSaving?: boolean;
    selectedSegmentMs?: { startMs: number; endMs: number } | null;
}

type Tab = 'timeline' | 'coaching' | 'drills';

const emptyReport = (): CoachingReport => ({
    matchSummary: '', technicalGrade: 0, gradeLabel: '', eliteTip: '',
    keyStrengths: [], criticalWeaknesses: [], prescribedDrills: [],
});

/** Normalises an analysis into an editable draft with a guaranteed coaching report. */
const toDraft = (a: AnalysisV2Dto): AnalysisV2Dto => ({
    ...a,
    visualDna: a.visualDna ?? '',
    matchEvents: a.matchEvents ?? [],
    coachingReport: a.coachingReport
        ? {
            ...a.coachingReport,
            keyStrengths: a.coachingReport.keyStrengths ?? [],
            criticalWeaknesses: a.coachingReport.criticalWeaknesses ?? [],
            prescribedDrills: a.coachingReport.prescribedDrills ?? [],
        }
        : emptyReport(),
});

const tabButtonClass = (active: boolean) =>
    `shadcn-ui-tab px-4 py-2 rounded-t-md font-medium transition-colors duration-150 focus:outline-none ` +
    (active
        ? 'bg-parchment-100 border-b-2 border-samurai-400 text-samurai-400 shadow-zen-sm'
        : 'bg-parchment-200 text-slate-zen400 hover:text-ink-400 hover:bg-parchment-300');

const AnalysisV2Panel: React.FC<AnalysisV2PanelProps> = ({ analysis, onSeekMs, onSave, isSaving, selectedSegmentMs }) => {
    const { t } = useTranslation();
    const [activeTab, setActiveTab] = useState<Tab>('timeline');
    const [draft, setDraft] = useState<AnalysisV2Dto>(() => toDraft(analysis));
    const [dirty, setDirty] = useState(false);

    // Reset the working copy whenever a different analysis (or fresh server data) arrives.
    useEffect(() => {
        setDraft(toDraft(analysis));
        setDirty(false);
    }, [analysis]);

    const report = draft.coachingReport as CoachingReport;

    const patch = (next: Partial<AnalysisV2Dto>) => { setDraft((d) => ({ ...d, ...next })); setDirty(true); };
    const patchReport = (next: Partial<CoachingReport>) => {
        setDraft((d) => ({ ...d, coachingReport: { ...(d.coachingReport as CoachingReport), ...next } }));
        setDirty(true);
    };

    const setEvents = (events: MatchEvent[]) => patch({ matchEvents: events });
    const setStrengths = (keyStrengths: KeyStrength[]) => patchReport({ keyStrengths });
    const setWeaknesses = (criticalWeaknesses: CriticalWeakness[]) => patchReport({ criticalWeaknesses });
    const setDrills = (prescribedDrills: PrescribedDrill[]) => patchReport({ prescribedDrills });

    const eventCount = useMemo(() => draft.matchEvents.length, [draft.matchEvents]);

    return (
        <div className="feedback-container p-4 rounded-md shadow-zen-md border border-[rgba(60,50,40,0.10)]">
            <AnalysisSummaryHeader
                visualDna={draft.visualDna ?? ''}
                matchSummary={report.matchSummary}
                gradeLabel={report.gradeLabel ?? ''}
                eliteTip={report.eliteTip ?? ''}
                technicalGrade={report.technicalGrade}
                onChange={(p) => {
                    if ('visualDna' in p) patch({ visualDna: p.visualDna });
                    const reportPatch: Partial<CoachingReport> = {};
                    if ('matchSummary' in p) reportPatch.matchSummary = p.matchSummary;
                    if ('gradeLabel' in p) reportPatch.gradeLabel = p.gradeLabel;
                    if ('eliteTip' in p) reportPatch.eliteTip = p.eliteTip;
                    if ('technicalGrade' in p) reportPatch.technicalGrade = p.technicalGrade as number;
                    if (Object.keys(reportPatch).length > 0) patchReport(reportPatch);
                }}
            />

            <div className="flex space-x-2 border-b mb-4">
                <button type="button" className={tabButtonClass(activeTab === 'timeline')} onClick={() => setActiveTab('timeline')}>
                    {t('videoReviewV2.tabs.timeline')} ({eventCount})
                </button>
                <button type="button" className={tabButtonClass(activeTab === 'coaching')} onClick={() => setActiveTab('coaching')}>
                    {t('videoReviewV2.tabs.coaching')}
                </button>
                <button type="button" className={tabButtonClass(activeTab === 'drills')} onClick={() => setActiveTab('drills')}>
                    {t('videoReviewV2.tabs.drills')}
                </button>
            </div>

            <div className="mt-4">
                {activeTab === 'timeline' && (
                    <MatchTimelineTab events={draft.matchEvents} onChange={setEvents} onSeekMs={onSeekMs} selectedSegmentMs={selectedSegmentMs} />
                )}
                {activeTab === 'coaching' && (
                    <CoachingTab
                        strengths={report.keyStrengths}
                        weaknesses={report.criticalWeaknesses}
                        onChangeStrengths={setStrengths}
                        onChangeWeaknesses={setWeaknesses}
                        onSeekMs={onSeekMs}
                    />
                )}
                {activeTab === 'drills' && (
                    <PrescribedDrillsTab drills={report.prescribedDrills} onChange={setDrills} />
                )}
            </div>

            <div className="mt-4 flex items-center justify-end gap-3">
                {dirty && <span className="text-xs text-amber-600">{t('videoReviewV2.unsavedChanges')}</span>}
                <Button type="button" disabled={isSaving || !dirty} onClick={() => onSave(draft)}>
                    {isSaving ? t('videoReviewV2.saving') : t('videoReviewV2.saveChanges')}
                </Button>
            </div>
        </div>
    );
};

export default AnalysisV2Panel;

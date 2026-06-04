import React from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { MatchEvent, Actor, TechniqueCategory, Position, Outcome } from '@/types/global';
import ConfidenceBadge from './ConfidenceBadge';
import {
    actorKey, techniqueCategoryKey, positionKey, outcomeKey,
    actorBadgeClass, outcomeBadgeClass, formatMs,
    ALL_ACTORS, ALL_TECHNIQUE_CATEGORIES, ALL_POSITIONS, ALL_OUTCOMES,
} from './enumLabels';

interface MatchEventCardProps {
    event: MatchEvent;
    index: number;
    onChange: (next: MatchEvent) => void;
    onDelete: () => void;
    onSeekMs: (ms: number) => void;
    selectedSegmentMs?: { startMs: number; endMs: number } | null;
}

const fieldClass = 'w-full text-sm bg-parchment-50 border border-[rgba(60,50,40,0.18)] rounded px-2 py-1 focus:outline-none focus:border-samurai-400';

const MatchEventCard: React.FC<MatchEventCardProps> = ({ event, onChange, onDelete, onSeekMs, selectedSegmentMs }) => {
    const { t } = useTranslation();
    const set = <K extends keyof MatchEvent>(key: K, value: MatchEvent[K]) => onChange({ ...event, [key]: value });

    return (
        <div className="rounded-md border border-[rgba(60,50,40,0.12)] bg-parchment-100 p-3 mb-3">
            <div className="flex items-center justify-between gap-2 mb-2">
                <div className="flex items-center gap-2 flex-wrap">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${actorBadgeClass[event.actor]}`}>
                        {t(actorKey(event.actor))}
                    </span>
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${outcomeBadgeClass[event.outcome]}`}>
                        {t(outcomeKey(event.outcome))}
                    </span>
                    <ConfidenceBadge confidence={event.confidence} />
                </div>
                <div className="flex items-center gap-2">
                    <Button type="button" size="sm" variant="secondary" onClick={() => onSeekMs(event.startTimestampMs)}>
                        ▶ {formatMs(event.startTimestampMs)}
                    </Button>
                    <Button type="button" size="sm" variant="secondary" className="text-blood-500" onClick={onDelete}>
                        ✕
                    </Button>
                </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                <label className="text-xs text-slate-zen400">
                    {t('videoReviewV2.enums.actor.Student')}/{t('videoReviewV2.enums.actor.Opponent')}
                    <select className={fieldClass} value={event.actor} onChange={(e) => set('actor', e.target.value as Actor)}>
                        {ALL_ACTORS.map((a) => <option key={a} value={a}>{t(actorKey(a))}</option>)}
                    </select>
                </label>
                <label className="text-xs text-slate-zen400">
                    {t('videoReviewV2.event.category')}
                    <select className={fieldClass} value={event.techniqueCategory} onChange={(e) => set('techniqueCategory', e.target.value as TechniqueCategory)}>
                        {ALL_TECHNIQUE_CATEGORIES.map((c) => <option key={c} value={c}>{t(techniqueCategoryKey(c))}</option>)}
                    </select>
                </label>
                <label className="text-xs text-slate-zen400 sm:col-span-2">
                    {t('videoReviewV2.event.techniqueName')}
                    <input className={fieldClass} value={event.techniqueName ?? ''} onChange={(e) => set('techniqueName', e.target.value)} />
                </label>
                <label className="text-xs text-slate-zen400">
                    {t('videoReviewV2.event.positionBefore')}
                    <select className={fieldClass} value={event.positionBefore ?? ''} onChange={(e) => set('positionBefore', e.target.value as Position)}>
                        <option value="">—</option>
                        {ALL_POSITIONS.map((p) => <option key={p} value={p}>{t(positionKey(p))}</option>)}
                    </select>
                </label>
                <label className="text-xs text-slate-zen400">
                    {t('videoReviewV2.event.positionAfter')}
                    <select className={fieldClass} value={event.positionAfter ?? ''} onChange={(e) => set('positionAfter', e.target.value as Position)}>
                        <option value="">—</option>
                        {ALL_POSITIONS.map((p) => <option key={p} value={p}>{t(positionKey(p))}</option>)}
                    </select>
                </label>
                <label className="text-xs text-slate-zen400">
                    {t('videoReviewV2.event.outcome')}
                    <select className={fieldClass} value={event.outcome} onChange={(e) => set('outcome', e.target.value as Outcome)}>
                        {ALL_OUTCOMES.map((o) => <option key={o} value={o}>{t(outcomeKey(o))}</option>)}
                    </select>
                </label>
                <label className="text-xs text-slate-zen400">
                    {t('videoReviewV2.event.guardType')}
                    <input className={fieldClass} value={event.guardType ?? ''} onChange={(e) => set('guardType', e.target.value)} />
                </label>
                <label className="text-xs text-slate-zen400">
                    {t('videoReviewV2.event.startMs')}
                    <input type="number" className={fieldClass} value={event.startTimestampMs} onChange={(e) => set('startTimestampMs', Number(e.target.value))} />
                </label>
                <label className="text-xs text-slate-zen400">
                    {t('videoReviewV2.event.endMs')}
                    <input type="number" className={fieldClass} value={event.endTimestampMs} onChange={(e) => set('endTimestampMs', Number(e.target.value))} />
                </label>
                <label className="text-xs text-slate-zen400 sm:col-span-2">
                    {t('videoReviewV2.event.submissionType')}
                    <input className={fieldClass} value={event.submissionType ?? ''} onChange={(e) => set('submissionType', e.target.value)} />
                </label>
                <label className="text-xs text-slate-zen400 sm:col-span-2">
                    {t('videoReviewV2.timeline.actionsTitle')}
                    <textarea className={fieldClass} rows={2} value={event.actionsDescription ?? ''} onChange={(e) => set('actionsDescription', e.target.value)} />
                </label>
            </div>

            {selectedSegmentMs && (
                <Button
                    type="button"
                    size="sm"
                    variant="secondary"
                    className="mt-2"
                    onClick={() => onChange({ ...event, startTimestampMs: selectedSegmentMs.startMs, endTimestampMs: selectedSegmentMs.endMs })}
                >
                    {t('videoReviewV2.event.useSelection')}
                </Button>
            )}
        </div>
    );
};

export default MatchEventCard;

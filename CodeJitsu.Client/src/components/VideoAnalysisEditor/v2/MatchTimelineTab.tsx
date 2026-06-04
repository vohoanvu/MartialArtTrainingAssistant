import React from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { MatchEvent } from '@/types/global';
import MatchEventCard from './MatchEventCard';

interface MatchTimelineTabProps {
    events: MatchEvent[];
    onChange: (events: MatchEvent[]) => void;
    onSeekMs: (ms: number) => void;
    selectedSegmentMs?: { startMs: number; endMs: number } | null;
}

const newEvent = (selection?: { startMs: number; endMs: number } | null): MatchEvent => ({
    startTimestampMs: selection?.startMs ?? 0,
    endTimestampMs: selection?.endMs ?? 0,
    actor: 'Student',
    techniqueCategory: 'Transition',
    techniqueName: '',
    positionBefore: null,
    positionAfter: null,
    outcome: 'InProgress',
    guardType: null,
    submissionType: null,
    actionsDescription: '',
    confidence: 1.0,
});

const MatchTimelineTab: React.FC<MatchTimelineTabProps> = ({ events, onChange, onSeekMs, selectedSegmentMs }) => {
    const { t } = useTranslation();

    const updateAt = (index: number, next: MatchEvent) => {
        const copy = [...events];
        copy[index] = next;
        onChange(copy);
    };
    const deleteAt = (index: number) => onChange(events.filter((_, i) => i !== index));
    const add = () => onChange([...events, newEvent(selectedSegmentMs)]);

    const sorted = [...events]
        .map((e, originalIndex) => ({ e, originalIndex }))
        .sort((a, b) => a.e.startTimestampMs - b.e.startTimestampMs);

    return (
        <div>
            <div className="flex items-center justify-between mb-3">
                <h3 className="font-serif text-lg font-bold text-ink-400">{t('videoReviewV2.tabs.timeline')}</h3>
                <Button type="button" size="sm" onClick={add}>+ {t('videoReviewV2.timeline.addEvent')}</Button>
            </div>

            {events.length === 0 ? (
                <p className="text-sm text-slate-zen400">{t('videoReviewV2.timeline.empty')}</p>
            ) : (
                sorted.map(({ e, originalIndex }) => (
                    <MatchEventCard
                        key={originalIndex}
                        event={e}
                        index={originalIndex}
                        onChange={(next) => updateAt(originalIndex, next)}
                        onDelete={() => deleteAt(originalIndex)}
                        onSeekMs={onSeekMs}
                        selectedSegmentMs={selectedSegmentMs}
                    />
                ))
            )}
        </div>
    );
};

export default MatchTimelineTab;

import React from 'react';
import { useTranslation } from 'react-i18next';
import { LOW_CONFIDENCE_THRESHOLD } from './enumLabels';

interface ConfidenceBadgeProps {
    confidence: number; // 0.0 - 1.0
}

/** Shows confidence as a percentage and flags low-confidence (<0.7) events for review. */
const ConfidenceBadge: React.FC<ConfidenceBadgeProps> = ({ confidence }) => {
    const { t } = useTranslation();
    const pct = Math.round((confidence ?? 0) * 100);
    const isLow = (confidence ?? 0) < LOW_CONFIDENCE_THRESHOLD;

    return (
        <span
            className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${
                isLow ? 'bg-amber-100 text-amber-700' : 'bg-parchment-200 text-slate-zen400'
            }`}
            title={isLow ? t('videoReviewV2.confidence.lowTooltip') : undefined}
        >
            {t('videoReviewV2.confidence.label', { percent: pct })}
            {isLow && <span aria-label={t('videoReviewV2.confidence.low')}>⚠</span>}
        </span>
    );
};

export default ConfidenceBadge;

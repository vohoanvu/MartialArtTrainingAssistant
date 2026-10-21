import { useTranslation } from 'react-i18next';
import { Infinity as InfinityIcon, Hourglass, Sparkles } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { cn } from '@/lib/utils';
import { formatQuotaResetTime } from '@/lib/analysisQuota';
import { AnalysisQuotaStatus, isQuotaBlocked } from '@/types/analysisQuota';

interface AnalysisQuotaIndicatorProps {
    quota: AnalysisQuotaStatus | null;
    className?: string;
}

/**
 * Calm one-line indicator of the user's free demo analyses. Hidden when no limit is enforced (or the
 * status is unknown); a warning panel replaces it when the user is out of analyses.
 */
export function AnalysisQuotaIndicator({ quota, className }: AnalysisQuotaIndicatorProps) {
    const { t, i18n } = useTranslation();
    if (!quota || !quota.enabled) return null;

    if (quota.exempt) {
        return (
            <p
                data-testid="analysis-quota"
                className={cn('flex items-center gap-1.5 font-sans text-xs text-slate-zen400', className)}
            >
                <InfinityIcon className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
                {t('analysisQuota.unlimited')}
            </p>
        );
    }

    const resetTime = formatQuotaResetTime(quota.resetsAt, i18n.language);
    const windowLabel = t('analysisQuota.window', { count: quota.windowHours });

    if (isQuotaBlocked(quota)) {
        const body =
            quota.limit <= 0
                ? t('analysisQuota.disabledBody')
                : resetTime
                  ? t('analysisQuota.exhaustedBody', { limit: quota.limit, window: windowLabel, time: resetTime })
                  : t('analysisQuota.exhaustedBodyNoTime', { limit: quota.limit, window: windowLabel });
        return (
            <Alert variant="warning" role="status" data-testid="analysis-quota" className={cn('flex gap-2.5', className)}>
                <Hourglass className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
                <AlertDescription>
                    <span className="font-semibold">{t('analysisQuota.exhaustedTitle')}</span> {body}
                </AlertDescription>
            </Alert>
        );
    }

    return (
        <p
            data-testid="analysis-quota"
            role="status"
            className={cn('flex flex-wrap items-center gap-x-1.5 gap-y-0.5 font-sans text-xs text-slate-zen400', className)}
        >
            <Sparkles className="h-3.5 w-3.5 shrink-0 text-gold-text" aria-hidden="true" />
            <span>
                {t('analysisQuota.remaining', { remaining: quota.remaining, limit: quota.limit, window: windowLabel })}
                {resetTime && ` · ${t('analysisQuota.nextFreesUp', { time: resetTime })}`}
            </span>
        </p>
    );
}

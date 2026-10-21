import { beforeAll, describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AnalysisQuotaIndicator } from '@/components/VideoAnalysisEditor/AnalysisQuotaIndicator';
import { formatQuotaResetTime } from '@/lib/analysisQuota';
import { isQuotaBlocked } from '@/types/analysisQuota';
import { mockQuotaStatus } from '../mocks/aiConsoleData';
import { initTestI18n } from '../helpers/i18nTest';

beforeAll(async () => {
    await initTestI18n();
});

describe('AnalysisQuotaIndicator', () => {
    it('shows the remaining count, the window and when the next slot frees up', () => {
        const quota = mockQuotaStatus();
        render(<AnalysisQuotaIndicator quota={quota} />);

        const indicator = screen.getByTestId('analysis-quota');
        expect(indicator).toHaveTextContent('2 of 3 free analyses left per 24 hours');
        expect(indicator).toHaveTextContent(`next one frees up at ${formatQuotaResetTime(quota.resetsAt, 'en')}`);
    });

    it('does not mention a reset time when nothing has been used yet', () => {
        render(<AnalysisQuotaIndicator quota={mockQuotaStatus({ used: 0, remaining: 3, resetsAt: null })} />);
        const indicator = screen.getByTestId('analysis-quota');
        expect(indicator).toHaveTextContent('3 of 3 free analyses left per 24 hours');
        expect(indicator).not.toHaveTextContent('frees up');
    });

    it('does not hard-code 24 hours', () => {
        render(<AnalysisQuotaIndicator quota={mockQuotaStatus({ windowHours: 168, limit: 5, remaining: 4 })} />);
        expect(screen.getByTestId('analysis-quota')).toHaveTextContent('4 of 5 free analyses left per 168 hours');
    });

    it('explains when the next analysis frees up once every analysis is used', () => {
        const quota = mockQuotaStatus({ used: 3, remaining: 0 });
        render(<AnalysisQuotaIndicator quota={quota} />);

        const indicator = screen.getByTestId('analysis-quota');
        expect(indicator).toHaveTextContent('No free analyses left.');
        expect(indicator).toHaveTextContent(`Your next one frees up at ${formatQuotaResetTime(quota.resetsAt, 'en')}`);
        expect(isQuotaBlocked(quota)).toBe(true);
    });

    it('says analysis is turned off when the limit is 0', () => {
        render(<AnalysisQuotaIndicator quota={mockQuotaStatus({ limit: 0, used: 0, remaining: 0, resetsAt: null })} />);
        expect(screen.getByTestId('analysis-quota')).toHaveTextContent('turned off for demo accounts');
    });

    it('shows unlimited for exempt (admin) users, even with remaining 0', () => {
        const quota = mockQuotaStatus({ exempt: true, used: 9, remaining: 0 });
        render(<AnalysisQuotaIndicator quota={quota} />);
        expect(screen.getByTestId('analysis-quota')).toHaveTextContent('Unlimited analyses (admin)');
        expect(isQuotaBlocked(quota)).toBe(false);
    });

    it('renders nothing when the limit is not enforced or the status is unknown', () => {
        const { rerender } = render(<AnalysisQuotaIndicator quota={mockQuotaStatus({ enabled: false, remaining: 0 })} />);
        expect(screen.queryByTestId('analysis-quota')).not.toBeInTheDocument();
        rerender(<AnalysisQuotaIndicator quota={null} />);
        expect(screen.queryByTestId('analysis-quota')).not.toBeInTheDocument();
        expect(isQuotaBlocked(mockQuotaStatus({ enabled: false, remaining: 0 }))).toBe(false);
        expect(isQuotaBlocked(null)).toBe(false);
    });
});

describe('formatQuotaResetTime', () => {
    it('returns null for missing or invalid input', () => {
        expect(formatQuotaResetTime(null, 'en')).toBeNull();
        expect(formatQuotaResetTime('not a date', 'en')).toBeNull();
    });

    it('shows only the time for today and adds the date for later days', () => {
        const now = new Date(2026, 9, 21, 9, 0, 0);
        const sameDay = formatQuotaResetTime(new Date(2026, 9, 21, 18, 40).toISOString(), 'en', now) as string;
        const nextDay = formatQuotaResetTime(new Date(2026, 9, 22, 18, 40).toISOString(), 'en', now) as string;
        expect(sameDay).not.toMatch(/Oct/);
        expect(nextDay).toMatch(/Oct/);
        expect(nextDay).toMatch(/22/);
    });
});

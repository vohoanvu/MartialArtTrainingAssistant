import { useCallback, useEffect, useRef, useState } from 'react';
import { getAnalysisQuota } from '@/services/api';
import { analysisConnection } from '@/services/SignalRService';
import useAuthStore from '@/store/authStore';
import type { AnalysisQuotaStatus } from '@/types/analysisQuota';

/** setTimeout cannot exceed a signed 32-bit int; windows can be up to 720h. */
const MAX_TIMER_MS = 2_000_000_000;
/** Small cushion so the server has definitely released the slot when we re-check. */
const RESET_CUSHION_MS = 1_500;

// Completion refreshes the "used" count; a failure refunds a use, so every terminal event matters.
const TERMINAL_EVENTS = ['AnalysisV2Completed', 'AnalysisCompleted', 'AnalysisV2Failed'] as const;

export interface UseAnalysisQuotaResult {
    /** null until the first successful load (or when the endpoint is unreachable). */
    quota: AnalysisQuotaStatus | null;
    loading: boolean;
    refresh: () => Promise<void>;
}

/**
 * Loads the signed-in user's free-analysis quota and keeps it fresh: after the global analysis
 * completed/failed SignalR events (a failure refunds a use) and when the next slot is due to free up.
 * A failed fetch leaves the previous value in place; the server stays the source of truth for
 * enforcement, so the UI simply stops showing the indicator if the status is unknown.
 */
export function useAnalysisQuota(): UseAnalysisQuotaResult {
    const accessToken = useAuthStore((s) => s.accessToken);
    const hydrate = useAuthStore((s) => s.hydrate);
    const [quota, setQuota] = useState<AnalysisQuotaStatus | null>(null);
    const [loading, setLoading] = useState(false);
    const mounted = useRef(true);
    const requestId = useRef(0);

    useEffect(() => {
        mounted.current = true;
        return () => {
            mounted.current = false;
        };
    }, []);

    const refresh = useCallback(async () => {
        if (!accessToken) {
            setQuota(null);
            return;
        }
        const id = ++requestId.current;
        setLoading(true);
        try {
            const status = await getAnalysisQuota({ jwtToken: accessToken, hydrate });
            if (mounted.current && id === requestId.current) setQuota(status);
        } catch (e) {
            console.error('Could not load the analysis quota:', e);
        } finally {
            if (mounted.current && id === requestId.current) setLoading(false);
        }
    }, [accessToken, hydrate]);

    useEffect(() => {
        void refresh();
    }, [refresh]);

    useEffect(() => {
        const onTerminal = () => void refresh();
        TERMINAL_EVENTS.forEach((evt) => analysisConnection.on(evt, onTerminal));
        return () => {
            TERMINAL_EVENTS.forEach((evt) => analysisConnection.off(evt, onTerminal));
        };
    }, [refresh]);

    // When the user is out of analyses, re-check as soon as the oldest use expires.
    useEffect(() => {
        if (!quota || !quota.enabled || quota.exempt || quota.remaining > 0 || !quota.resetsAt) return;
        const due = new Date(quota.resetsAt).getTime();
        if (Number.isNaN(due)) return;
        // Floor of 5s so a clock skew can never turn this into a tight polling loop.
        const delay = Math.min(Math.max(due - Date.now() + RESET_CUSHION_MS, 5_000), MAX_TIMER_MS);
        const timer = setTimeout(() => void refresh(), delay);
        return () => clearTimeout(timer);
    }, [quota, refresh]);

    return { quota, loading, refresh };
}

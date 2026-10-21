import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../mocks/server';
import { mockQuotaStatus } from '../mocks/aiConsoleData';
import { initTestI18n, installRadixPolyfills } from '../helpers/i18nTest';
import VideoAnalysisManagement from '@/pages/VideoAnalysisManagement';
import { Toaster } from '@/components/ui/toaster';
import useAuthStore from '@/store/authStore';

// The uploaded-videos table is irrelevant here (and needs a Router).
vi.mock('@/pages/VideoStorageListing', () => ({ default: () => null }));

// A tiny in-memory stand-in for the SignalR hub so tests can emit server events.
const hub = vi.hoisted(() => {
    const handlers = new Map<string, Set<(...args: unknown[]) => void>>();
    const connection = {
        on: (evt: string, fn: (...args: unknown[]) => void) => {
            if (!handlers.has(evt)) handlers.set(evt, new Set());
            handlers.get(evt)!.add(fn);
        },
        off: (evt: string, fn: (...args: unknown[]) => void) => handlers.get(evt)?.delete(fn),
        state: 'Disconnected',
        start: () => Promise.resolve(),
    };
    return { connection, emit: (evt: string, ...args: unknown[]) => handlers.get(evt)?.forEach((fn) => fn(...args)) };
});
vi.mock('@/services/SignalRService', () => ({ analysisConnection: hub.connection, connection: hub.connection }));

beforeAll(async () => {
    await initTestI18n();
    installRadixPolyfills();
});

beforeEach(() => {
    useAuthStore.setState({
        accessToken: 'test-token',
        refreshToken: 'r',
        loginStatus: 'authenticated',
        user: { fighterInfo: { role: 1 } },
    } as never);
    server.use(http.put('https://storage.example.com/upload/10', () => new HttpResponse(null, { status: 200 })));
});

const renderPage = () =>
    render(
        <>
            <VideoAnalysisManagement />
            <Toaster />
        </>
    );

const upload = async (user: ReturnType<typeof userEvent.setup>) => {
    await user.upload(
        document.getElementById('videoFile') as HTMLInputElement,
        new File(['x'], 'tape.mp4', { type: 'video/mp4' })
    );
    await user.type(screen.getByLabelText(/Specify which fighter/), 'the one in blue');
    await user.click(screen.getByRole('button', { name: 'Upload Video' }));
};

describe('VideoAnalysisManagement - quota', () => {
    it('loads the quota status and shows the indicator beside the upload form', async () => {
        renderPage();
        expect(await screen.findByTestId('analysis-quota')).toHaveTextContent('2 of 3 free analyses left per 24 hours');
    });

    it('blocks the upload button when the loaded quota is used up', async () => {
        const user = userEvent.setup();
        server.use(http.get('/vid/api/video/analysis-quota', () => HttpResponse.json(mockQuotaStatus({ used: 3, remaining: 0 }))));
        renderPage();

        expect(await screen.findByText('No free analyses left.')).toBeInTheDocument();
        await user.upload(
            document.getElementById('videoFile') as HTMLInputElement,
            new File(['x'], 'tape.mp4', { type: 'video/mp4' })
        );
        expect(screen.getByRole('button', { name: 'Upload Video' })).toBeDisabled();
    });

    it('refreshes the quota when an analysis fails, because the failure refunds a use', async () => {
        let quotaCalls = 0;
        server.use(
            http.get('/vid/api/video/analysis-quota', () => {
                quotaCalls += 1;
                return HttpResponse.json(quotaCalls === 1 ? mockQuotaStatus({ used: 3, remaining: 0 }) : mockQuotaStatus());
            })
        );
        renderPage();
        expect(await screen.findByText('No free analyses left.')).toBeInTheDocument();

        hub.emit('AnalysisV2Failed', 10);

        expect(await screen.findByText(/2 of 3 free analyses left/)).toBeInTheDocument();
        expect(screen.queryByText('No free analyses left.')).not.toBeInTheDocument();
    });

    it('starts the v2 analysis after the upload and refreshes the quota', async () => {
        const user = userEvent.setup();
        let quotaCalls = 0;
        let analyzed = 0;
        let auth: string | null = null;
        server.use(
            http.get('/vid/api/video/analysis-quota', () => {
                quotaCalls += 1;
                return HttpResponse.json(quotaCalls === 1 ? mockQuotaStatus() : mockQuotaStatus({ used: 2, remaining: 1 }));
            }),
            http.post('/vid/api/video/analyze-v2/:videoId', ({ request, params }) => {
                analyzed += 1;
                auth = request.headers.get('Authorization');
                expect(params.videoId).toBe('10');
                return new HttpResponse(null, { status: 202 });
            })
        );
        renderPage();
        await screen.findByText(/2 of 3 free analyses left/);
        await upload(user);

        expect(await screen.findByText('Analysis Started')).toBeInTheDocument();
        expect(analyzed).toBe(1);
        expect(auth).toBe('Bearer test-token');
        expect(await screen.findByText(/1 of 3 free analyses left/)).toBeInTheDocument();
    });

    it('shows the in-progress message for a 409 and does not claim the analysis started', async () => {
        const user = userEvent.setup();
        server.use(
            http.post('/vid/api/video/analyze-v2/:videoId', () =>
                HttpResponse.json(
                    { code: 'ANALYSIS_IN_PROGRESS', message: 'Hold on, this tape is already being analyzed (409).' },
                    { status: 409 }
                )
            )
        );
        renderPage();
        await screen.findByTestId('analysis-quota');
        await upload(user);

        expect(await screen.findByText('Analysis already running')).toBeInTheDocument();
        expect(screen.getByText('Hold on, this tape is already being analyzed (409).')).toBeInTheDocument();
        expect(screen.queryByText('Analysis Started')).not.toBeInTheDocument();
        expect(screen.queryByText('Analysis Failed')).not.toBeInTheDocument();
    });

    it('shows the server message for a 429 on analyze-v2 and refreshes the quota indicator', async () => {
        const user = userEvent.setup();
        let quotaCalls = 0;
        server.use(
            http.get('/vid/api/video/analysis-quota', () => {
                quotaCalls += 1;
                return HttpResponse.json(quotaCalls === 1 ? mockQuotaStatus() : mockQuotaStatus({ used: 3, remaining: 0 }));
            }),
            http.post('/vid/api/video/analyze-v2/:videoId', () =>
                HttpResponse.json(
                    {
                        code: 'ANALYSIS_QUOTA_EXCEEDED',
                        message: 'You have used all 3 analyses (analyze-v2 429).',
                        limit: 3,
                        used: 3,
                        windowHours: 24,
                        resetsAt: '2099-01-01T18:40:00Z',
                    },
                    { status: 429, headers: { 'Retry-After': '60' } }
                )
            )
        );
        renderPage();
        await screen.findByText(/2 of 3 free analyses left/);
        await upload(user);

        expect(await screen.findByText('Free analysis limit reached')).toBeInTheDocument();
        expect(screen.getByText('You have used all 3 analyses (analyze-v2 429).')).toBeInTheDocument();
        expect(screen.queryByText('Analysis Failed')).not.toBeInTheDocument();
        await waitFor(() => expect(screen.getByText('No free analyses left.')).toBeInTheDocument());
    });
});

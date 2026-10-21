import { beforeAll, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../mocks/server';
import { mockQuotaStatus } from '../mocks/aiConsoleData';
import { initTestI18n, installRadixPolyfills } from '../helpers/i18nTest';
import VideoUploadForm from '@/components/VideoAnalysisEditor/VideoUploadForm';
import { Toaster } from '@/components/ui/toaster';
import type { AnalysisQuotaStatus } from '@/types/analysisQuota';

const GCS_URL = 'https://storage.example.com/upload/10';

beforeAll(async () => {
    await initTestI18n();
    installRadixPolyfills();
});

const renderForm = (quota: AnalysisQuotaStatus | null) => {
    const props = {
        fighterRole: 1,
        jwtToken: 'test-token',
        hydrateFn: vi.fn().mockResolvedValue(undefined),
        isUploading: false,
        setIsUploading: vi.fn(),
        onUploadSuccess: vi.fn(),
        onQuotaRejected: vi.fn(),
        quota,
    };
    render(
        <>
            <VideoUploadForm {...props} />
            <Toaster />
        </>
    );
    return props;
};

const pickFile = async (user: ReturnType<typeof userEvent.setup>) => {
    await user.upload(
        document.getElementById('videoFile') as HTMLInputElement,
        new File(['x'], 'tape.mp4', { type: 'video/mp4' })
    );
    await user.type(screen.getByLabelText(/Specify which fighter/), 'the one in blue');
};

describe('VideoUploadForm - quota indicator and disabled state', () => {
    it('shows the remaining count next to the upload button and keeps it usable', async () => {
        const user = userEvent.setup();
        renderForm(mockQuotaStatus());

        expect(screen.getByTestId('analysis-quota')).toHaveTextContent('2 of 3 free analyses left per 24 hours');
        await pickFile(user);
        expect(screen.getByRole('button', { name: 'Upload Video' })).toBeEnabled();
    });

    it('disables the upload and explains when the next one frees up when remaining is 0', async () => {
        const user = userEvent.setup();
        renderForm(mockQuotaStatus({ used: 3, remaining: 0 }));

        expect(screen.getByTestId('analysis-quota')).toHaveTextContent(/No free analyses left\..*Your next one frees up at/);
        await pickFile(user);
        // A file is selected, yet the button stays disabled so no large upload is attempted.
        expect(screen.getByRole('button', { name: 'Upload Video' })).toBeDisabled();
    });

    it('shows unlimited for admins and never blocks them', async () => {
        const user = userEvent.setup();
        renderForm(mockQuotaStatus({ exempt: true, used: 50, remaining: 0 }));

        expect(screen.getByTestId('analysis-quota')).toHaveTextContent('Unlimited analyses (admin)');
        await pickFile(user);
        expect(screen.getByRole('button', { name: 'Upload Video' })).toBeEnabled();
    });

    it('hides the indicator when the limit is not enforced', () => {
        renderForm(mockQuotaStatus({ enabled: false, remaining: 0 }));
        expect(screen.queryByTestId('analysis-quota')).not.toBeInTheDocument();
    });
});

describe('VideoUploadForm - 429 on upload-url', () => {
    it('shows the server message, never starts the GCS PUT and asks for a quota refresh', async () => {
        const user = userEvent.setup();
        let putCalls = 0;
        server.use(
            http.post('/vid/api/video/upload-url', () =>
                HttpResponse.json(
                    {
                        code: 'ANALYSIS_QUOTA_EXCEEDED',
                        message: 'Quota spent: try again after 18:40 (upload-url).',
                        limit: 3,
                        used: 3,
                        windowHours: 24,
                        resetsAt: '2099-01-01T18:40:00Z',
                    },
                    { status: 429, headers: { 'Retry-After': '3600' } }
                )
            ),
            http.put(GCS_URL, () => {
                putCalls += 1;
                return new HttpResponse(null, { status: 200 });
            })
        );
        // Stale client view: it still thinks a slot is free, so the button is enabled.
        const props = renderForm(mockQuotaStatus());
        await pickFile(user);
        await user.click(screen.getByRole('button', { name: 'Upload Video' }));

        expect(await screen.findByText('Free analysis limit reached')).toBeInTheDocument();
        expect((await screen.findAllByText('Quota spent: try again after 18:40 (upload-url).')).length).toBeGreaterThan(0);
        await waitFor(() => expect(props.onQuotaRejected).toHaveBeenCalledTimes(1));
        expect(props.onUploadSuccess).not.toHaveBeenCalled();
        expect(putCalls).toBe(0);
        expect(props.setIsUploading).toHaveBeenLastCalledWith(false);
    });

    it('falls back to the i18n message when the 429 has no usable body', async () => {
        const user = userEvent.setup();
        server.use(http.post('/vid/api/video/upload-url', () => new HttpResponse('Too Many Requests', { status: 429 })));
        const props = renderForm(mockQuotaStatus());
        await pickFile(user);
        await user.click(screen.getByRole('button', { name: 'Upload Video' }));

        expect((await screen.findAllByText(/You've used all your free analyses for now/)).length).toBeGreaterThan(0);
        expect(props.onQuotaRejected).toHaveBeenCalled();
    });

    it('still uploads straight to storage and reports success when the quota allows it', async () => {
        const user = userEvent.setup();
        let putCalls = 0;
        server.use(
            http.put(GCS_URL, () => {
                putCalls += 1;
                return new HttpResponse(null, { status: 200 });
            })
        );
        const props = renderForm(mockQuotaStatus());
        await pickFile(user);
        await user.click(screen.getByRole('button', { name: 'Upload Video' }));

        await waitFor(() => expect(props.onUploadSuccess).toHaveBeenCalledWith({ videoId: 10 }));
        expect(putCalls).toBe(1);
        expect(props.onQuotaRejected).not.toHaveBeenCalled();
    });
});

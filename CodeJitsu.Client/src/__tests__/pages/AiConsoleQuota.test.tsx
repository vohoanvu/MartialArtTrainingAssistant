import { beforeAll, beforeEach, describe, expect, it } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../mocks/server';
import { mockQuotaConfig } from '../mocks/aiConsoleData';
import { initTestI18n, installRadixPolyfills } from '../helpers/i18nTest';
import AiConsole from '@/pages/AiConsole';
import { Toaster } from '@/components/ui/toaster';
import useAuthStore from '@/store/authStore';

const renderPage = () =>
    render(
        <>
            <AiConsole />
            <Toaster />
        </>
    );

const waitForCard = async () => within(await screen.findByTestId('ai-quota-card'));

beforeAll(async () => {
    await initTestI18n();
    installRadixPolyfills();
});

beforeEach(() => {
    useAuthStore.setState({ accessToken: 'test-token', refreshToken: 'r', loginStatus: 'authenticated' });
});

describe('AiConsole - demo analysis quota card', () => {
    it('loads the effective values, the default badge and the server defaults', async () => {
        let auth: string | null = null;
        server.use(
            http.get('/vid/api/ai-console/quota', ({ request }) => {
                auth = request.headers.get('Authorization');
                return HttpResponse.json(mockQuotaConfig());
            })
        );
        renderPage();
        const card = await waitForCard();

        expect(card.getByText('Demo analysis quota')).toBeInTheDocument();
        expect(card.getByRole('switch', { name: 'Enforce the limit' })).toBeChecked();
        expect(card.getByLabelText('Analyses per window (0–1000)')).toHaveValue(3);
        expect(card.getByLabelText('Window in hours (1–720)')).toHaveValue(24);
        expect(card.getByText('Default')).toBeInTheDocument();
        expect(card.getByText('Server default: 3 per 24 h, limit on')).toBeInTheDocument();
        expect(card.getByRole('button', { name: 'Save' })).toBeDisabled();
        expect(card.getByRole('button', { name: 'Reset to defaults' })).toBeDisabled();
        expect(auth).toBe('Bearer test-token');
    });

    it('shows the overridden badge with who changed it', async () => {
        server.use(
            http.get('/vid/api/ai-console/quota', () =>
                HttpResponse.json(
                    mockQuotaConfig({
                        isOverridden: true,
                        updatedBy: 'boss@example.com',
                        updatedAt: '2026-10-21T08:30:00Z',
                        effective: { enabled: false, maxPerWindow: 10, windowHours: 48 },
                    })
                )
            )
        );
        renderPage();
        const card = await waitForCard();

        expect(card.getByText('Overridden')).toBeInTheDocument();
        expect(card.getByText(/boss@example\.com/)).toBeInTheDocument();
        expect(card.getByRole('switch', { name: 'Enforce the limit' })).not.toBeChecked();
        expect(card.getByLabelText('Analyses per window (0–1000)')).toHaveValue(10);
        expect(card.getByRole('button', { name: 'Reset to defaults' })).toBeEnabled();
    });

    it('PUTs the edited settings, updates the card and shows a toast', async () => {
        const user = userEvent.setup();
        let body: Record<string, unknown> | null = null;
        server.use(
            http.put('/vid/api/ai-console/quota', async ({ request }) => {
                body = (await request.json()) as Record<string, unknown>;
                return HttpResponse.json(
                    mockQuotaConfig({
                        isOverridden: true,
                        updatedBy: 'boss@example.com',
                        updatedAt: '2026-10-21T08:30:00Z',
                        effective: body as never,
                    })
                );
            })
        );
        renderPage();
        const card = await waitForCard();

        const max = card.getByLabelText('Analyses per window (0–1000)');
        await user.clear(max);
        await user.type(max, '5');
        await user.click(card.getByRole('switch', { name: 'Enforce the limit' }));
        expect(card.getByText('Unsaved changes')).toBeInTheDocument();
        await user.click(card.getByRole('button', { name: 'Save' }));

        await waitFor(() => expect(body).toEqual({ enabled: false, maxPerWindow: 5, windowHours: 24 }));
        expect(await screen.findByText('Quota saved')).toBeInTheDocument();
        await waitFor(() => expect(card.getByText('Overridden')).toBeInTheDocument());
        expect(card.getByText(/boss@example\.com/)).toBeInTheDocument();
        expect(card.queryByText('Unsaved changes')).not.toBeInTheDocument();
        expect(card.getByRole('button', { name: 'Save' })).toBeDisabled();
    });

    it('validates the ranges and blocks saving while invalid', async () => {
        const user = userEvent.setup();
        renderPage();
        const card = await waitForCard();
        const save = card.getByRole('button', { name: 'Save' });

        const max = card.getByLabelText('Analyses per window (0–1000)');
        await user.clear(max);
        await user.type(max, '1001');
        expect(card.getByText('Enter a whole number from 0 to 1000.')).toBeInTheDocument();
        expect(save).toBeDisabled();
        await user.clear(max);
        await user.type(max, '0');
        expect(card.queryByText('Enter a whole number from 0 to 1000.')).not.toBeInTheDocument();
        expect(save).toBeEnabled();

        const hours = card.getByLabelText('Window in hours (1–720)');
        await user.clear(hours);
        await user.type(hours, '0');
        expect(card.getByText('Enter a whole number from 1 to 720.')).toBeInTheDocument();
        expect(save).toBeDisabled();
        await user.clear(hours);
        expect(card.getByText('Enter a whole number from 1 to 720.')).toBeInTheDocument();
        await user.type(hours, '720');
        expect(save).toBeEnabled();
    });

    it('maps a 400 validation response onto the field', async () => {
        const user = userEvent.setup();
        server.use(
            http.put('/vid/api/ai-console/quota', () =>
                HttpResponse.json(
                    {
                        title: 'One or more validation errors occurred.',
                        status: 400,
                        errors: { windowHours: ['Window must not exceed 720 hours (server).'] },
                    },
                    { status: 400 }
                )
            )
        );
        renderPage();
        const card = await waitForCard();

        const hours = card.getByLabelText('Window in hours (1–720)');
        await user.clear(hours);
        await user.type(hours, '12');
        await user.click(card.getByRole('button', { name: 'Save' }));

        expect((await card.findAllByText(/Window must not exceed 720 hours \(server\)\./)).length).toBeGreaterThan(0);
        expect(hours).toHaveAttribute('aria-invalid', 'true');
        expect(card.getByText('Unsaved changes')).toBeInTheDocument();
        expect(card.getByRole('button', { name: 'Save' })).toBeEnabled();
    });

    it('calls DELETE only after the confirmation dialog is accepted and adopts the defaults', async () => {
        const user = userEvent.setup();
        let deleteCalls = 0;
        server.use(
            http.get('/vid/api/ai-console/quota', () =>
                HttpResponse.json(
                    mockQuotaConfig({
                        isOverridden: true,
                        updatedBy: 'boss@example.com',
                        updatedAt: '2026-10-21T08:30:00Z',
                        effective: { enabled: true, maxPerWindow: 10, windowHours: 48 },
                    })
                )
            ),
            http.delete('/vid/api/ai-console/quota', () => {
                deleteCalls += 1;
                return HttpResponse.json(mockQuotaConfig());
            })
        );
        renderPage();
        const card = await waitForCard();

        await user.click(card.getByRole('button', { name: 'Reset to defaults' }));
        const dialog = await screen.findByRole('alertdialog');
        expect(within(dialog).getByText('Reset the quota to defaults?')).toBeInTheDocument();
        await user.click(within(dialog).getByRole('button', { name: 'Cancel' }));
        expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
        expect(deleteCalls).toBe(0);

        await user.click(card.getByRole('button', { name: 'Reset to defaults' }));
        await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Reset' }));

        await waitFor(() => expect(deleteCalls).toBe(1));
        await waitFor(() => expect(card.getByText('Default')).toBeInTheDocument());
        expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
        expect(card.getByLabelText('Analyses per window (0–1000)')).toHaveValue(3);
        expect(card.getByLabelText('Window in hours (1–720)')).toHaveValue(24);
    });

    it('shows an error with a working retry when the quota cannot be loaded, without breaking the page', async () => {
        const user = userEvent.setup();
        server.use(http.get('/vid/api/ai-console/quota', () => new HttpResponse(null, { status: 500 }), { once: true }));
        renderPage();

        expect(await screen.findByTestId('ai-quota-error')).toBeInTheDocument();
        // model cards are unaffected
        expect(await screen.findByTestId('ai-feature-class-curriculum')).toBeInTheDocument();
        await user.click(within(screen.getByTestId('ai-quota-error')).getByRole('button', { name: 'Retry' }));
        expect(await screen.findByTestId('ai-quota-card')).toBeInTheDocument();
    });
});

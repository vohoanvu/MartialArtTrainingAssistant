import { beforeAll, beforeEach, describe, expect, it } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { delay, http, HttpResponse } from 'msw';
import { server } from '../mocks/server';
import { mockOverview, mockTestResult } from '../mocks/aiConsoleData';
import { initTestI18n, installRadixPolyfills } from '../helpers/i18nTest';
import AiConsole from '@/pages/AiConsole';
import { Toaster } from '@/components/ui/toaster';
import useAuthStore from '@/store/authStore';

const PIPELINE = 'ai-feature-video-analysis-pipeline';
const CURRICULUM = 'ai-feature-class-curriculum';
const SEARCH = 'ai-feature-technique-search';

const renderPage = () =>
    render(
        <>
            <AiConsole />
            <Toaster />
        </>
    );

const card = (id: string) => within(screen.getByTestId(id));

const waitForCards = async () => {
    await screen.findByTestId(CURRICULUM);
};

beforeAll(async () => {
    await initTestI18n();
    installRadixPolyfills();
});

beforeEach(() => {
    useAuthStore.setState({ accessToken: 'test-token', refreshToken: 'r', loginStatus: 'authenticated' });
});

describe('AiConsole page - rendering', () => {
    it('renders one card per feature from the overview with override badges', async () => {
        renderPage();
        expect(screen.getByTestId('ai-console-loading')).toBeInTheDocument();
        await waitForCards();

        expect(screen.getByRole('heading', { level: 1, name: 'AI Console' })).toBeInTheDocument();
        expect(card(PIPELINE).getByText('Video analysis — agentic pipeline')).toBeInTheDocument();
        expect(card(CURRICULUM).getByText('Class curriculum generator')).toBeInTheDocument();
        expect(card(SEARCH).getByText('Technique resource search')).toBeInTheDocument();

        expect(card(CURRICULUM).getByText('Overridden')).toBeInTheDocument();
        expect(card(CURRICULUM).getByText(/admin@example\.com/)).toBeInTheDocument();
        expect(card(PIPELINE).getByText('Default')).toBeInTheDocument();
        expect(screen.queryByTestId('ai-console-loading')).not.toBeInTheDocument();
    });

    it('sends the bearer token with the overview request', async () => {
        let auth: string | null = null;
        server.use(
            http.get('/vid/api/ai-console', ({ request }) => {
                auth = request.headers.get('Authorization');
                return HttpResponse.json(mockOverview());
            })
        );
        renderPage();
        await waitForCards();
        expect(auth).toBe('Bearer test-token');
    });

    it('shows the pipeline phase table, the search settings and model notes', async () => {
        renderPage();
        await waitForCards();

        const pipeline = card(PIPELINE);
        for (const name of ['Profiler', 'Event Logger', 'Verifier', 'Head Coach']) {
            expect(pipeline.getByRole('rowheader', { name })).toBeInTheDocument();
        }
        expect(pipeline.getByLabelText('Event Logger: FPS')).toHaveValue(4);
        expect(pipeline.getByText('Best quality, highest cost.')).toBeInTheDocument();

        const search = card(SEARCH);
        expect(search.getByLabelText('Max results (1–20)')).toHaveValue(8);
        expect(search.getByRole('switch', { name: 'Verify YouTube links via YouTube Data API' })).toBeChecked();
    });
});

describe('AiConsole page - access and errors', () => {
    it('shows the no-access state on 403 instead of crashing', async () => {
        server.use(http.get('/vid/api/ai-console', () => new HttpResponse(null, { status: 403 })));
        renderPage();

        expect(await screen.findByText("You don't have access to the AI Console")).toBeInTheDocument();
        expect(screen.queryByTestId(CURRICULUM)).not.toBeInTheDocument();
    });

    it('shows an error state with a working retry on server errors', async () => {
        const user = userEvent.setup();
        server.use(http.get('/vid/api/ai-console', () => new HttpResponse(null, { status: 500 }), { once: true }));
        renderPage();

        expect(await screen.findByText('Could not load the AI Console')).toBeInTheDocument();
        await user.click(screen.getByRole('button', { name: 'Retry' }));
        await waitForCards();
        expect(screen.queryByText('Could not load the AI Console')).not.toBeInTheDocument();
    });
});

describe('AiConsole page - model-dependent controls', () => {
    it('switches between the thinking level select and the budget input when the model changes', async () => {
        const user = userEvent.setup();
        renderPage();
        await waitForCards();

        const curriculum = card(CURRICULUM);
        expect(curriculum.getByRole('combobox', { name: 'Thinking level' })).toBeInTheDocument();
        expect(curriculum.queryByRole('spinbutton', { name: /Thinking budget/ })).not.toBeInTheDocument();
        expect(curriculum.getByLabelText('Temperature')).toBeEnabled();

        await user.click(curriculum.getByRole('combobox', { name: 'Model' }));
        await user.click(await screen.findByRole('option', { name: /Gemini 3 Budget/ }));

        expect(curriculum.queryByRole('combobox', { name: 'Thinking level' })).not.toBeInTheDocument();
        expect(curriculum.getByRole('spinbutton', { name: /Thinking budget/ })).toBeInTheDocument();
        // This model ignores temperature
        expect(curriculum.getByLabelText('Temperature')).toBeDisabled();
        expect(curriculum.getByText('This model ignores temperature, so the field is disabled.')).toBeInTheDocument();

        // and back again
        await user.click(curriculum.getByRole('combobox', { name: 'Model' }));
        await user.click(await screen.findByRole('option', { name: /Gemini 4 Argon/ }));
        expect(curriculum.getByRole('combobox', { name: 'Thinking level' })).toBeInTheDocument();
    });

    it('nulls the field that no longer applies, resets the location, and saves the result', async () => {
        const user = userEvent.setup();
        let putBody: Record<string, unknown> | null = null;
        server.use(
            http.put('/vid/api/ai-console/features/:key', async ({ request }) => {
                putBody = (await request.json()) as Record<string, unknown>;
                const feature = mockOverview().features[1];
                return HttpResponse.json({ ...feature, isOverridden: true, effective: putBody });
            })
        );
        renderPage();
        await waitForCards();

        const curriculum = card(CURRICULUM);
        await user.click(curriculum.getByRole('combobox', { name: 'Model' }));
        await user.click(await screen.findByRole('option', { name: /Gemini 3 Budget/ }));
        await user.type(curriculum.getByRole('spinbutton', { name: /Thinking budget/ }), '2048');
        await user.click(curriculum.getByRole('button', { name: 'Save' }));

        await waitFor(() => expect(putBody).not.toBeNull());
        expect(putBody).toMatchObject({
            modelId: 'gemini-3-budget',
            location: 'global',
            thinkingLevel: null,
            thinkingBudget: 2048,
            temperature: null,
            maxOutputTokens: 4096,
        });
    });

    it('disables models that lack required capabilities', async () => {
        const user = userEvent.setup();
        renderPage();
        await waitForCards();

        // technique-search requires Google Search -> Gemini 3 Budget has none
        await user.click(card(SEARCH).getByRole('combobox', { name: 'Model' }));
        expect(await screen.findByRole('option', { name: /Gemini 3 Budget/ })).toHaveAttribute('aria-disabled', 'true');
        expect(screen.getByRole('option', { name: /Gemini 4 Argon/ })).not.toHaveAttribute('aria-disabled', 'true');
    });

    it('shows the implicit-caching callout for models without explicit cache', async () => {
        const user = userEvent.setup();
        renderPage();
        await waitForCards();

        const pipeline = card(PIPELINE);
        expect(pipeline.queryByText(/implicit caching only/)).not.toBeInTheDocument();
        await user.click(pipeline.getByRole('combobox', { name: 'Model' }));
        await user.click(await screen.findByRole('option', { name: /Gemini 3 Budget/ }));
        expect(pipeline.getByText(/implicit caching only/)).toBeInTheDocument();
        // phases now show numeric budget inputs
        expect(pipeline.getByRole('spinbutton', { name: 'Profiler: Thinking' })).toBeInTheDocument();
    });

    it('validates a custom model id and blocks saving while invalid', async () => {
        const user = userEvent.setup();
        renderPage();
        await waitForCards();

        const curriculum = card(CURRICULUM);
        await user.click(curriculum.getByRole('combobox', { name: 'Model' }));
        await user.click(await screen.findByRole('option', { name: /Custom model ID/ }));

        const input = curriculum.getByLabelText('Custom model ID');
        await user.clear(input);
        await user.type(input, 'Bad Id!');
        expect(curriculum.getByText(/Use 2–63 lowercase letters/)).toBeInTheDocument();
        expect(curriculum.getByRole('button', { name: 'Save' })).toBeDisabled();
        expect(curriculum.getByRole('button', { name: 'Test model' })).toBeDisabled();

        await user.clear(input);
        await user.type(input, 'my-model-1');
        expect(curriculum.queryByText(/Use 2–63 lowercase letters/)).not.toBeInTheDocument();
        expect(curriculum.getByRole('button', { name: 'Save' })).toBeEnabled();
    });
});

describe('AiConsole page - save / reset / test', () => {
    it('keeps Save disabled until the form changes and validates ranges', async () => {
        const user = userEvent.setup();
        renderPage();
        await waitForCards();

        const curriculum = card(CURRICULUM);
        const save = curriculum.getByRole('button', { name: 'Save' });
        expect(save).toBeDisabled();

        const temp = curriculum.getByLabelText('Temperature');
        await user.clear(temp);
        await user.type(temp, '5');
        expect(curriculum.getByText('Enter a number from 0 to 2.')).toBeInTheDocument();
        expect(save).toBeDisabled();

        await user.clear(temp);
        await user.type(temp, '1.2');
        expect(save).toBeEnabled();
    });

    it('PUTs the edited values, updates the card and shows a toast', async () => {
        const user = userEvent.setup();
        let body: Record<string, unknown> | null = null;
        server.use(
            http.put('/vid/api/ai-console/features/:key', async ({ request, params }) => {
                body = (await request.json()) as Record<string, unknown>;
                expect(params.key).toBe('technique-search');
                const feature = mockOverview().features[2];
                return HttpResponse.json({
                    ...feature,
                    isOverridden: true,
                    updatedAt: '2026-10-21T08:30:00Z',
                    updatedBy: 'boss@example.com',
                    effective: body,
                });
            })
        );
        renderPage();
        await waitForCards();

        const search = card(SEARCH);
        const max = search.getByLabelText('Max results (1–20)');
        await user.clear(max);
        await user.type(max, '12');
        await user.click(search.getByRole('switch', { name: 'Verify YouTube links via YouTube Data API' }));
        expect(search.getByText('Unsaved changes')).toBeInTheDocument();
        await user.click(search.getByRole('button', { name: 'Save' }));

        await waitFor(() => expect(body).not.toBeNull());
        expect(body).toMatchObject({
            modelId: 'gemini-4-argon',
            search: { maxResults: 12, verifyWithYouTube: false },
        });
        expect(await screen.findByText('Settings saved')).toBeInTheDocument();
        await waitFor(() => expect(search.getByText('Overridden')).toBeInTheDocument());
        expect(search.getByText(/boss@example\.com/)).toBeInTheDocument();
        expect(search.queryByText('Unsaved changes')).not.toBeInTheDocument();
        expect(search.getByRole('button', { name: 'Save' })).toBeDisabled();
    });

    it('maps a 400 validation response onto the field', async () => {
        const user = userEvent.setup();
        server.use(
            http.put('/vid/api/ai-console/features/:key', () =>
                HttpResponse.json(
                    {
                        title: 'One or more validation errors occurred.',
                        status: 400,
                        errors: { maxOutputTokens: ['Model supports at most 8192 output tokens.'] },
                    },
                    { status: 400 }
                )
            )
        );
        renderPage();
        await waitForCards();

        const curriculum = card(CURRICULUM);
        const tokens = curriculum.getByLabelText('Max output tokens');
        await user.clear(tokens);
        await user.type(tokens, '60000');
        await user.click(curriculum.getByRole('button', { name: 'Save' }));

        const messages = await curriculum.findAllByText(/Model supports at most 8192 output tokens\./);
        expect(messages.length).toBeGreaterThan(0);
        expect(tokens).toHaveAttribute('aria-invalid', 'true');
        // the edit stays unsaved (the global toast store is shared across tests, so it is not asserted here)
        expect(curriculum.getByText('Unsaved changes')).toBeInTheDocument();
        expect(curriculum.getByRole('button', { name: 'Save' })).toBeEnabled();
    });

    it('maps nested pipeline phase errors onto the phase row', async () => {
        const user = userEvent.setup();
        server.use(
            http.put('/vid/api/ai-console/features/:key', () =>
                HttpResponse.json(
                    { title: 'Invalid', status: 400, errors: { 'phases.eventLogger.maxOutputTokens': ['Too many tokens for the Event Logger.'] } },
                    { status: 400 }
                )
            )
        );
        renderPage();
        await waitForCards();

        const pipeline = card(PIPELINE);
        const fps = pipeline.getByLabelText('Verifier: FPS');
        await user.clear(fps);
        await user.type(fps, '3');
        await user.click(pipeline.getByRole('button', { name: 'Save' }));

        const rowAlerts = await pipeline.findAllByText(/Too many tokens for the Event Logger\./);
        expect(rowAlerts.length).toBeGreaterThan(0);
        expect(pipeline.getByLabelText('Event Logger: Max output tokens')).toHaveAttribute('aria-invalid', 'true');
    });

    it('calls DELETE only after the confirmation dialog is accepted', async () => {
        const user = userEvent.setup();
        let deleteCalls = 0;
        server.use(
            http.delete('/vid/api/ai-console/features/:key', () => {
                deleteCalls += 1;
                const feature = mockOverview().features[1];
                return HttpResponse.json({ ...feature, isOverridden: false, updatedAt: null, updatedBy: null, effective: feature.defaults });
            })
        );
        renderPage();
        await waitForCards();

        const curriculum = card(CURRICULUM);
        // Not overridden -> reset disabled
        expect(card(PIPELINE).getByRole('button', { name: 'Reset to defaults' })).toBeDisabled();

        await user.click(curriculum.getByRole('button', { name: 'Reset to defaults' }));
        const dialog = await screen.findByRole('alertdialog');
        expect(within(dialog).getByText('Reset to defaults?')).toBeInTheDocument();

        // Cancel does nothing
        await user.click(within(dialog).getByRole('button', { name: 'Cancel' }));
        expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
        expect(deleteCalls).toBe(0);

        await user.click(curriculum.getByRole('button', { name: 'Reset to defaults' }));
        await user.click(within(await screen.findByRole('alertdialog')).getByRole('button', { name: 'Reset' }));

        await waitFor(() => expect(deleteCalls).toBe(1));
        await waitFor(() => expect(curriculum.getByText('Default')).toBeInTheDocument());
        expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
        // temperature went back to the server default (0.5)
        expect(curriculum.getByLabelText('Temperature')).toHaveValue(0.5);
    });

    it('tests the unsaved form values and shows the result while the button is busy', async () => {
        const user = userEvent.setup();
        let body: Record<string, unknown> | null = null;
        server.use(
            http.post('/vid/api/ai-console/features/:key/test', async ({ request }) => {
                body = (await request.json()) as Record<string, unknown>;
                await delay(60);
                return HttpResponse.json(mockTestResult);
            })
        );
        renderPage();
        await waitForCards();

        const curriculum = card(CURRICULUM);
        const tokens = curriculum.getByLabelText('Max output tokens');
        await user.clear(tokens);
        await user.type(tokens, '1000');
        await user.click(curriculum.getByRole('button', { name: 'Test model' }));

        const busyButton = await curriculum.findByRole('button', { name: 'Testing…' });
        expect(busyButton).toBeDisabled();

        expect(await curriculum.findByText('Model responded')).toBeInTheDocument();
        expect(curriculum.getByText('pong')).toBeInTheDocument();
        expect(curriculum.getByText(/Latency: 812 ms/)).toBeInTheDocument();
        expect(body).toMatchObject({ modelId: 'gemini-4-argon', maxOutputTokens: 1000 });
        expect(curriculum.getByRole('button', { name: 'Test model' })).toBeEnabled();
    });

    it('shows a failed test result inline', async () => {
        const user = userEvent.setup();
        server.use(
            http.post('/vid/api/ai-console/features/:key/test', () =>
                HttpResponse.json({ ...mockTestResult, ok: false, sample: null, error: 'Model not found in location' })
            )
        );
        renderPage();
        await waitForCards();

        await user.click(card(CURRICULUM).getByRole('button', { name: 'Test model' }));
        expect(await card(CURRICULUM).findByText('Test failed')).toBeInTheDocument();
        expect(card(CURRICULUM).getByText('Model not found in location')).toBeInTheDocument();
    });
});

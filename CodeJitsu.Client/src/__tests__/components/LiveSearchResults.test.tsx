import { beforeAll, describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import LiveSearchResults from '@/components/ClassSessionManagement/LiveSearchResults';
import { mockTechniqueSearchResults } from '../mocks/handlers';
import { initTestI18n } from '../helpers/i18nTest';

beforeAll(async () => {
    await initTestI18n();
});

const renderResults = (payload: unknown) =>
    render(<LiveSearchResults jsonContent={typeof payload === 'string' ? payload : JSON.stringify(payload)} />);

describe('LiveSearchResults - grounded payload', () => {
    it('renders the YouTube grid with channel, relevance note and formatted date', () => {
        renderResults(mockTechniqueSearchResults);

        expect(screen.getByRole('heading', { name: /YouTube Videos/ })).toBeInTheDocument();
        expect(screen.getByText('Guard Passing Tutorial')).toBeInTheDocument();
        expect(screen.getByText('Grappling Academy')).toBeInTheDocument();
        expect(screen.getByText(/Covers the knee-slice pass step by step\./)).toBeInTheDocument();
        // the raw ISO string is formatted (timezone-safe: either side of midnight UTC)
        expect(screen.queryByText('2024-01-01T00:00:00+00:00')).not.toBeInTheDocument();
        expect(screen.getByText(/(Dec 31, 2023|Jan 1, 2024)/)).toBeInTheDocument();
    });

    it('lists web resources as external links with Free/Paid badges', () => {
        renderResults(mockTechniqueSearchResults);

        const heading = screen.getByRole('heading', { name: /Articles & resources/ });
        const section = heading.closest('section') as HTMLElement;
        const links = within(section).getAllByRole('link');
        expect(links).toHaveLength(2);

        const free = within(section).getByRole('link', { name: /Knee slice breakdown/ });
        expect(free).toHaveAttribute('href', 'https://example.com/knee-slice');
        expect(free).toHaveAttribute('target', '_blank');
        expect(free).toHaveAttribute('rel', 'noopener noreferrer');
        expect(within(section).getByText('Free')).toBeInTheDocument();
        expect(within(section).getByText('Paid')).toBeInTheDocument();
        expect(within(section).getByText('Detailed article')).toBeInTheDocument();
    });

    it('omits the Free/Paid badge when is_free is null and drops non-http(s) urls', () => {
        renderResults({
            web_resources: [
                { title: 'Unknown price', url: 'https://example.com/a', description: null, is_free: null },
                { title: 'Evil', url: 'javascript:alert(1)', description: null, is_free: true },
                { title: 'Data', url: 'data:text/html,<script>1</script>', description: null, is_free: false },
                { title: 'Not a url', url: 'nope', description: null, is_free: false },
            ],
        });

        expect(screen.getByRole('link', { name: /Unknown price/ })).toBeInTheDocument();
        expect(screen.queryByText('Evil')).not.toBeInTheDocument();
        expect(screen.queryByText('Data')).not.toBeInTheDocument();
        expect(screen.queryByText('Not a url')).not.toBeInTheDocument();
        expect(screen.queryByText('Free')).not.toBeInTheDocument();
        expect(screen.queryByText('Paid')).not.toBeInTheDocument();
    });

    it('renders the search entry point unmodified in a script-less sandboxed iframe', () => {
        const { container } = renderResults(mockTechniqueSearchResults);

        expect(screen.getByText('Google Search suggestions')).toBeInTheDocument();
        const frame = container.querySelector('iframe[srcdoc]') as HTMLIFrameElement;
        expect(frame).not.toBeNull();
        expect(frame.getAttribute('srcdoc')).toBe(mockTechniqueSearchResults.search_entry_point_html);
        expect(frame).toHaveAttribute('title', 'Google Search suggestions');

        const sandbox = frame.getAttribute('sandbox') ?? '';
        expect(sandbox.split(/\s+/).sort()).toEqual(['allow-popups', 'allow-popups-to-escape-sandbox']);
        expect(sandbox).not.toContain('allow-scripts');
        expect(sandbox).not.toContain('allow-same-origin');
        // never injected into the host document
        expect(container.querySelector('.chip')).toBeNull();
    });

    it('shows no suggestions frame when search_entry_point_html is null', () => {
        const { container } = renderResults({ ...mockTechniqueSearchResults, search_entry_point_html: null });
        expect(container.querySelector('iframe[srcdoc]')).toBeNull();
        expect(screen.queryByText('Google Search suggestions')).not.toBeInTheDocument();
    });

    it('shows the grounded source badge with the model id, and the YouTube badge for fallback', () => {
        const { unmount } = renderResults(mockTechniqueSearchResults);
        expect(screen.getByText('Grounded with Google Search · gemini-4-argon')).toBeInTheDocument();
        unmount();

        renderResults({ ...mockTechniqueSearchResults, source: 'youtube-fallback', model_id: null });
        expect(screen.getByText('YouTube search')).toBeInTheDocument();
        expect(screen.queryByText(/Grounded with Google Search/)).not.toBeInTheDocument();
    });

    it('opens the video player modal for a video', async () => {
        const user = userEvent.setup();
        const { baseElement } = renderResults(mockTechniqueSearchResults);

        await user.click(screen.getByRole('button', { name: 'Play Video' }));
        expect(screen.getByText('Video Player')).toBeInTheDocument();
        expect(baseElement.querySelector('iframe[src="https://www.youtube.com/embed/abc123"]')).not.toBeNull();
    });
});

describe('LiveSearchResults - robustness', () => {
    it.each([
        ['empty object', {}],
        ['only source', { source: 'youtube-fallback' }],
        ['null arrays', { youtube_videos: null, web_resources: null, web_search_queries: null }],
        ['non-array values', { youtube_videos: 'x', web_resources: { a: 1 } }],
        ['legacy payload', { youtube_videos: [{ video_id: 'v', title: 'Legacy', description: 'd', embed_link: 'https://www.youtube.com/embed/v', publication_date: 'not-a-date', thumbnail_url: '' }] }],
    ])('does not crash on %s', (_name, payload) => {
        expect(() => renderResults(payload)).not.toThrow();
    });

    it('keeps rendering legacy videos (raw date string, no channel / relevance / badge)', () => {
        renderResults({
            youtube_videos: [
                { video_id: 'v', title: 'Legacy', description: 'd', embed_link: 'https://www.youtube.com/embed/v', publication_date: 'not-a-date', thumbnail_url: '' },
            ],
        });
        expect(screen.getByText('Legacy')).toBeInTheDocument();
        expect(screen.getByText('not-a-date')).toBeInTheDocument();
        expect(screen.queryByText(/Why it's relevant/)).not.toBeInTheDocument();
        expect(screen.queryByText(/Articles & resources/)).not.toBeInTheDocument();
        expect(screen.getByAltText('Legacy')).toHaveAttribute('src', 'https://img.youtube.com/vi/v/0.jpg');
    });

    it('shows a friendly message when nothing was found', () => {
        renderResults({});
        expect(screen.getByText('No resources were found for this technique.')).toBeInTheDocument();
    });

    it('shows a parse error for invalid JSON or non-object JSON', () => {
        const { unmount } = renderResults('not json');
        expect(screen.getByText('Failed to parse search results')).toBeInTheDocument();
        unmount();
        renderResults('null');
        expect(screen.getByText('Failed to parse search results')).toBeInTheDocument();
    });
});

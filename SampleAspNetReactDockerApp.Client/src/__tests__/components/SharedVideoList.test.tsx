import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { server } from '../mocks/server';
import { http, HttpResponse } from 'msw';
import SharedVideosList from '@/components/SharedVideoList';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

// Mock useAuthStore — handles both selector and no-selector patterns
vi.mock('@/store/authStore', () => ({
  default: vi.fn((selector?: (s: Record<string, unknown>) => unknown) => {
    const state = {
      accessToken: 'mock-token',
      refreshToken: 'mock-refresh',
      loginStatus: 'authenticated',
      login: vi.fn(),
      logout: vi.fn(),
      hydrate: vi.fn(),
    };
    return selector ? selector(state) : state;
  }),
}));

describe('SharedVideosList', () => {
  it('renders video list with card content after loading', async () => {
    render(<SharedVideosList />);

    await waitFor(() => {
      expect(screen.getByText('BJJ Guard Pass Tutorial')).toBeInTheDocument();
    });
  });

  it('renders shared by username in the card', async () => {
    render(<SharedVideosList />);

    await waitFor(() => {
      expect(screen.getByText(/johndoe/i)).toBeInTheDocument();
    });
  });

  it('renders empty state when no videos are returned', async () => {
    server.use(
      http.get('/vid/api/video/getall', () => HttpResponse.json([]))
    );

    render(<SharedVideosList />);

    await waitFor(() => {
      expect(screen.getByText(/No Video yet shared/i)).toBeInTheDocument();
    });
  });

  it('renders video description in card', async () => {
    render(<SharedVideosList />);

    await waitFor(() => {
      expect(screen.getByText(/Learn the basics of guard passing/i)).toBeInTheDocument();
    });
  });
});

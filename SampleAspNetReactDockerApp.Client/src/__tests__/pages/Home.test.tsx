import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { Home } from '@/pages/Home';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => vi.fn(),
  };
});

vi.mock('@/store/authStore', () => ({
  default: vi.fn((selector) => {
    const state = {
      login: vi.fn(),
      loginStatus: 'unauthenticated',
      accessToken: null,
      refreshToken: null,
    };
    return selector(state);
  }),
}));

const renderHome = () =>
  render(
    <MemoryRouter>
      <Home />
    </MemoryRouter>
  );

describe('Home page', () => {
  it('renders the hero section heading', () => {
    renderHome();
    expect(
      screen.getByText(/Your AI-driven Brazilian JiuJitsu Training Assistant/i)
    ).toBeInTheDocument();
  });

  it('renders the LandingPageForm (Train like a warrior text)', () => {
    renderHome();
    expect(screen.getByText(/Train like a warrior/i)).toBeInTheDocument();
  });

  it('renders the Login form inside the page', () => {
    renderHome();
    expect(screen.getByRole('button', { name: /Login/i })).toBeInTheDocument();
  });
});

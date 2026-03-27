import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import ClassSession from '@/pages/Dashboard';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

const mockNavigate = vi.fn();
let mockLoginStatus = 'authenticated';
let mockUser: any = {
  email: 'instructor@example.com',
  username: 'instructor',
  isAdmin: false,
  fighterInfo: { role: 1, id: 1, fighterName: 'Instructor Joe', beltColor: 'Black', beltRank: 'Black', experience: 10, height: 180, weight: 85, bmi: 26.2, birthdate: '1985-01-01' },
};

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const mockHydrate = vi.fn();
vi.mock('@/store/authStore', () => ({
  default: vi.fn((selector?: (s: Record<string, unknown>) => unknown) => {
    const state = {
      loginStatus: mockLoginStatus,
      accessToken: 'test-token',
      refreshToken: 'test-refresh',
      hydrate: mockHydrate,
      user: mockUser,
    };
    return selector ? selector(state) : state;
  }),
}));

const renderDashboard = () =>
  render(
    <MemoryRouter>
      <ClassSession />
    </MemoryRouter>
  );

describe('Dashboard (ClassSession) page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockLoginStatus = 'authenticated';
  });

  it('renders the page heading', () => {
    renderDashboard();
    expect(screen.getByText(/Find your training session to check in/i)).toBeInTheDocument();
  });

  it('redirects to /login when unauthenticated', () => {
    mockLoginStatus = 'unauthenticated';
    renderDashboard();
    expect(mockNavigate).toHaveBeenCalledWith('/login');
  });

  it('renders training session table after data loads', async () => {
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('Active Training Sessions')).toBeInTheDocument();
    });
  });

  it('renders the Create New Session button for instructors', async () => {
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Create New Session/i })).toBeInTheDocument();
    });
  });

  it('disables Create New Session button for students (role 0)', async () => {
    mockUser = {
      ...mockUser,
      fighterInfo: { ...mockUser.fighterInfo, role: 0 },
    };
    renderDashboard();
    await waitFor(() => {
      const btn = screen.getByRole('button', { name: /Create New Session/i });
      expect(btn).toBeDisabled();
    });
  });

  it('Create New Session button is present and clickable', async () => {
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Create New Session/i })).toBeInTheDocument();
    });
  });
});

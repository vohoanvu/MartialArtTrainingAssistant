import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import Navbar from '@/components/Navbar';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { changeLanguage: vi.fn() },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

// We need to control auth store state per test
const mockLogout = vi.fn();
let mockLoginStatus: string = 'unauthenticated';
let mockUser: { email: string } | null = null;

vi.mock('@/store/authStore', () => ({
  default: vi.fn((selector) => {
    const state = {
      loginStatus: mockLoginStatus,
      logout: mockLogout,
      user: mockUser,
    };
    return selector(state);
  }),
}));

vi.mock('@/store/themeStore', () => ({
  default: vi.fn((selector) => {
    const state = {
      theme: 'light',
      setTheme: vi.fn(),
    };
    return selector(state);
  }),
}));

const renderNavbar = () =>
  render(
    <MemoryRouter>
      <Navbar />
    </MemoryRouter>
  );

describe('Navbar - unauthenticated', () => {
  beforeEach(() => {
    mockLoginStatus = 'unauthenticated';
    mockUser = null;
  });

  it('renders the CodeJitsu brand name', () => {
    renderNavbar();
    expect(screen.getByText('CodeJitsu')).toBeInTheDocument();
  });

  it('renders login and register links when unauthenticated', () => {
    renderNavbar();
    expect(screen.getByText('navbar.login')).toBeInTheDocument();
    expect(screen.getByText('navbar.register')).toBeInTheDocument();
  });

  it('does not render Video Analysis or Manage Classes links when unauthenticated', () => {
    renderNavbar();
    expect(screen.queryByText('Video Analysis')).not.toBeInTheDocument();
    expect(screen.queryByText('Manage Classes')).not.toBeInTheDocument();
  });
});

describe('Navbar - authenticated', () => {
  beforeEach(() => {
    mockLoginStatus = 'authenticated';
    mockUser = { email: 'instructor@example.com' };
  });

  it('renders Video Analysis and Manage Classes links when authenticated', () => {
    renderNavbar();
    expect(screen.getByText('Video Analysis')).toBeInTheDocument();
    expect(screen.getByText('Manage Classes')).toBeInTheDocument();
  });

  it('does not render login and register links when authenticated', () => {
    renderNavbar();
    expect(screen.queryByText('navbar.login')).not.toBeInTheDocument();
    expect(screen.queryByText('navbar.register')).not.toBeInTheDocument();
  });

  it('calls logout when the logout button is clicked', async () => {
    const user = userEvent.setup();
    renderNavbar();
    const logoutButton = screen.getByText('navbar.logout');
    await user.click(logoutButton);
    expect(mockLogout).toHaveBeenCalledTimes(1);
  });
});

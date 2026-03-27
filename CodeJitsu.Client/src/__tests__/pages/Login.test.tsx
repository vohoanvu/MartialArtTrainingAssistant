import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import Login from '@/pages/Login';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

const mockNavigate = vi.fn();
const mockLogin = vi.fn();
let mockLoginStatus = 'unauthenticated';

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

vi.mock('@/store/authStore', () => ({
  default: vi.fn((selector) => {
    const state = {
      login: mockLogin,
      loginStatus: mockLoginStatus,
    };
    return selector(state);
  }),
}));

const renderLogin = () =>
  render(
    <MemoryRouter>
      <Login />
    </MemoryRouter>
  );

describe('Login page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockLoginStatus = 'unauthenticated';
  });

  it('renders the login form with email and password fields', () => {
    renderLogin();
    expect(screen.getByLabelText(/Email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Login/i })).toBeInTheDocument();
  });

  it('navigates to /class-session when login is successful', async () => {
    mockLogin.mockResolvedValue({ successful: true, response: null });
    const user = userEvent.setup();
    renderLogin();

    await user.type(screen.getByLabelText(/Email/i), 'instructor@example.com');
    await user.type(screen.getByLabelText(/Password/i), 'SecurePass1!');
    await user.click(screen.getByRole('button', { name: /Login/i }));

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith({
        email: 'instructor@example.com',
        password: 'SecurePass1!',
      });
    });
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/class-session');
    });
  });

  it('shows error message when login fails', async () => {
    mockLogin.mockResolvedValue({ successful: false, response: 'Incorrect password' });
    const user = userEvent.setup();
    renderLogin();

    await user.type(screen.getByLabelText(/Email/i), 'user@example.com');
    await user.type(screen.getByLabelText(/Password/i), 'wrongpass');
    await user.click(screen.getByRole('button', { name: /Login/i }));

    await waitFor(() => {
      expect(screen.getByText(/Login failed/i)).toBeInTheDocument();
    });
  });
});

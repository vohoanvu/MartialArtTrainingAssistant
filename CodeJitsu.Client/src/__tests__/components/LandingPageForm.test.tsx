import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import LandingPageForm from '@/components/LandingPageForm';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

const mockNavigate = vi.fn();
const mockLogin = vi.fn();

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
      loginStatus: 'unauthenticated',
    };
    return selector(state);
  }),
}));

const renderForm = () =>
  render(
    <MemoryRouter>
      <LandingPageForm />
    </MemoryRouter>
  );

describe('LandingPageForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the login form with email and password fields', () => {
    renderForm();
    expect(screen.getByLabelText(/Email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Password/i)).toBeInTheDocument();
  });

  it('renders the Login submit button', () => {
    renderForm();
    expect(screen.getByRole('button', { name: /^Login$/i })).toBeInTheDocument();
  });

  it('navigates to class-session on successful login', async () => {
    mockLogin.mockResolvedValue({ successful: true, response: null });
    const user = userEvent.setup();
    renderForm();

    await user.type(screen.getByLabelText(/Email/i), 'test@example.com');
    await user.type(screen.getByLabelText(/Password/i), 'password123');
    await user.click(screen.getByRole('button', { name: /Login/i }));

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith({ email: 'test@example.com', password: 'password123' });
    });
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/class-session');
    });
  });

  it('displays error message on failed login', async () => {
    mockLogin.mockResolvedValue({ successful: false, response: 'Invalid credentials' });
    const user = userEvent.setup();
    renderForm();

    await user.type(screen.getByLabelText(/Email/i), 'bad@example.com');
    await user.type(screen.getByLabelText(/Password/i), 'wrongpass');
    await user.click(screen.getByRole('button', { name: /Login/i }));

    await waitFor(() => {
      expect(screen.getByText(/Login failed/i)).toBeInTheDocument();
    });
  });
});

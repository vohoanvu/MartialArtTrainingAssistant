import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import Register from '@/pages/Register';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

const mockNavigate = vi.fn();
const mockRegisterFighter = vi.fn();

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useSearchParams: () => [new URLSearchParams('')],
  };
});

vi.mock('@/store/authStore', () => ({
  default: vi.fn((selector) => {
    const state = {
      registerFighter: mockRegisterFighter,
    };
    return selector(state);
  }),
}));

const renderRegister = () =>
  render(
    <MemoryRouter>
      <Register />
    </MemoryRouter>
  );

describe('Register page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the registration form heading', () => {
    renderRegister();
    expect(screen.getByRole('heading', { name: /Register/i })).toBeInTheDocument();
  });

  it('renders required form fields', () => {
    renderRegister();
    expect(screen.getByLabelText(/^Name:/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Register/i })).toBeInTheDocument();
  });

  it('shows confirmation dialog on form submit', async () => {
    const user = userEvent.setup();
    renderRegister();

    await user.type(screen.getByLabelText(/^Name:/i), 'Test Fighter');
    await user.type(screen.getByLabelText(/^Email/i), 'new@example.com');
    await user.type(screen.getByLabelText(/^Password/i), 'Password123!');

    await user.click(screen.getByRole('button', { name: /Register/i }));

    await waitFor(() => {
      expect(screen.getByText(/Are you sure all of your details are correct/i)).toBeInTheDocument();
    });
  });

  it('navigates to /home after successful registration', async () => {
    mockRegisterFighter.mockResolvedValue({ successful: true, response: null });
    vi.spyOn(window, 'alert').mockImplementation(() => {});
    const user = userEvent.setup();
    renderRegister();

    await user.type(screen.getByLabelText(/^Name:/i), 'Test Fighter');
    await user.type(screen.getByLabelText(/^Email/i), 'new@example.com');
    await user.type(screen.getByLabelText(/^Password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /Register/i }));

    // Confirm the dialog
    await waitFor(() => screen.getByText(/Are you sure all of your details are correct/i));
    await user.click(screen.getByRole('button', { name: /Confirm/i }));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/home');
    });
  });
});

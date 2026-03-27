import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { server } from '../mocks/server';
import { http, HttpResponse } from 'msw';
import TrainingSessionForm from '@/components/ClassSessionManagement/TrainingSessionForm';

// Override the session detail handler to return a proper SessionDetailViewModel shape
const mockSessionDetailFull = {
  id: 1,
  trainingDate: '2026-03-26T09:00:00',
  description: 'Morning session',
  capacity: 20,
  duration: 1.5,
  status: 'Active',
  targetLevel: 'Beginner',
  instructorId: 1,
  instructor: { id: 1, fighterName: 'Instructor Joe', height: 180, weight: 85, gender: 'Male', birthdate: new Date(), fighterRole: 'Instructor', maxWorkoutDuration: 60, beltColor: 'Black', experience: 10 },
  students: [],
  studentIds: [],
  isCurriculumGenerated: false,
};

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const mockHydrate = vi.fn();
const mockAuthState = {
  accessToken: 'test-token',
  refreshToken: 'test-refresh',
  hydrate: mockHydrate,
  user: {
    email: 'instructor@example.com',
    username: 'instructor',
    isAdmin: false,
    fighterInfo: {
      id: 1,
      fighterName: 'Instructor Joe',
      role: 1,
      beltColor: 'Black',
      beltRank: 'Black',
      experience: 10,
      height: 180,
      weight: 85,
      bmi: 26.2,
      birthdate: '1985-01-01',
    },
  },
};

vi.mock('@/store/authStore', () => ({
  default: vi.fn((selector?: (s: Record<string, unknown>) => unknown) => {
    return selector ? selector(mockAuthState) : mockAuthState;
  }),
}));

const renderForm = (sessionId?: string) =>
  render(
    <MemoryRouter initialEntries={[sessionId ? `/edit-session/${sessionId}` : '/create-session']}>
      <Routes>
        <Route path="/create-session" element={<TrainingSessionForm />} />
        <Route path="/edit-session/:sessionId" element={<TrainingSessionForm />} />
      </Routes>
    </MemoryRouter>
  );

describe('TrainingSessionForm - Create mode', () => {
  it('renders Create New Session heading', () => {
    renderForm();
    expect(screen.getByText('Create New Session')).toBeInTheDocument();
  });

  it('renders all required form fields', () => {
    renderForm();
    expect(screen.getByLabelText(/Training Date/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Capacity/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Duration/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Description Notes/i)).toBeInTheDocument();
  });

  it('renders Create submit button in create mode', () => {
    renderForm();
    expect(screen.getByRole('button', { name: /^Create$/i })).toBeInTheDocument();
  });

  it('opens confirmation dialog on form submit', async () => {
    const user = userEvent.setup();
    renderForm();

    const dateInput = screen.getByLabelText(/Training Date/i);
    const capacityInput = screen.getByLabelText(/Capacity/i);
    const durationInput = screen.getByLabelText(/Duration/i);

    await user.type(dateInput, '2026-04-01T10:00');
    await user.clear(capacityInput);
    await user.type(capacityInput, '20');
    await user.clear(durationInput);
    await user.type(durationInput, '60');

    await user.click(screen.getByRole('button', { name: /^Create$/i }));

    await waitFor(() => {
      expect(screen.getByText(/Are you sure you want to create a session/i)).toBeInTheDocument();
    });
  });
});

describe('TrainingSessionForm - Edit mode', () => {
  beforeEach(() => {
    server.use(
      http.get('/api/trainingsession/:sessionId', () => {
        return HttpResponse.json(mockSessionDetailFull);
      })
    );
  });

  it('renders Update Session Details heading in edit mode', async () => {
    renderForm('1');
    await waitFor(() => {
      expect(screen.getByText('Update Session Details')).toBeInTheDocument();
    });
  });

  it('renders Update submit button in edit mode', async () => {
    renderForm('1');
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /^Update$/i })).toBeInTheDocument();
    });
  });
});

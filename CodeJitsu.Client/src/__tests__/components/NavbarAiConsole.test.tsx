import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { http, HttpResponse } from 'msw';
import { server } from '../mocks/server';
import Navbar from '@/components/Navbar';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { changeLanguage: vi.fn() },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

let mockLoginStatus: string = 'authenticated';
let mockAccessToken: string | null = 'jwt-123';

vi.mock('@/store/authStore', () => ({
  default: vi.fn((selector) =>
    selector({
      loginStatus: mockLoginStatus,
      logout: vi.fn(),
      user: { email: 'someone@example.com' },
      accessToken: mockAccessToken,
    })
  ),
}));

const renderNavbar = () =>
  render(
    <MemoryRouter>
      <Navbar />
    </MemoryRouter>
  );

/** Registers an access handler and resolves once the client has called it. */
const stubAccess = (respond: () => Response) => {
  let calls = 0;
  let auth: string | null = null;
  server.use(
    http.get('/vid/api/ai-console/access', ({ request }) => {
      calls += 1;
      auth = request.headers.get('Authorization');
      return respond();
    })
  );
  return {
    calls: () => calls,
    auth: () => auth,
    waitForCall: () => waitFor(() => expect(calls).toBeGreaterThan(0)),
  };
};

describe('Navbar - AI Console link', () => {
  beforeEach(() => {
    mockLoginStatus = 'authenticated';
    mockAccessToken = 'jwt-123';
  });

  it('is shown for an admin and sends the bearer token', async () => {
    const access = stubAccess(() => HttpResponse.json({ isAdmin: true }));
    renderNavbar();

    const link = await screen.findByRole('link', { name: 'aiConsole.nav' });
    expect(link).toHaveAttribute('href', '/console');
    expect(access.auth()).toBe('Bearer jwt-123');
    expect(access.calls()).toBe(1);
  });

  it('is hidden for a non-admin', async () => {
    const access = stubAccess(() => HttpResponse.json({ isAdmin: false }));
    renderNavbar();

    await access.waitForCall();
    // let the resolved promise flush into state
    await new Promise((r) => setTimeout(r, 20));
    expect(screen.queryByText('aiConsole.nav')).not.toBeInTheDocument();
    // regular links remain
    expect(screen.getByText('Video Analysis')).toBeInTheDocument();
  });

  it.each([401, 403, 500])('is hidden when the access check returns %i', async (status) => {
    const access = stubAccess(() => new HttpResponse(null, { status }));
    renderNavbar();

    await access.waitForCall();
    await new Promise((r) => setTimeout(r, 20));
    expect(screen.queryByText('aiConsole.nav')).not.toBeInTheDocument();
  });

  it('is hidden when the request fails at the network level', async () => {
    const access = stubAccess(() => HttpResponse.error());
    renderNavbar();

    await access.waitForCall();
    await new Promise((r) => setTimeout(r, 20));
    expect(screen.queryByText('aiConsole.nav')).not.toBeInTheDocument();
  });

  it('does not call the API when the user is unauthenticated', async () => {
    mockLoginStatus = 'unauthenticated';
    const access = stubAccess(() => HttpResponse.json({ isAdmin: true }));
    renderNavbar();

    await new Promise((r) => setTimeout(r, 30));
    expect(access.calls()).toBe(0);
    expect(screen.queryByText('aiConsole.nav')).not.toBeInTheDocument();
  });
});

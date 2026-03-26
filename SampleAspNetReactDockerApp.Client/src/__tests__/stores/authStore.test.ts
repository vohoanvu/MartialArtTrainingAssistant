import { describe, it, expect, beforeEach, vi } from 'vitest';
import { act } from '@testing-library/react';
import useAuthStore from '@/store/authStore';
import { server } from '../mocks/server';
import { http, HttpResponse } from 'msw';

// Reset the store state between tests
beforeEach(() => {
  useAuthStore.setState({
    accessToken: null,
    refreshToken: null,
    user: null,
    loginStatus: 'unauthenticated',
  });
  // Clear sessionStorage between tests
  sessionStorage.clear();
});

describe('authStore - initial state', () => {
  it('should have null tokens on initialization', () => {
    const { accessToken, refreshToken } = useAuthStore.getState();
    expect(accessToken).toBeNull();
    expect(refreshToken).toBeNull();
  });

  it('should have null user on initialization', () => {
    const { user } = useAuthStore.getState();
    expect(user).toBeNull();
  });

  it('should have unauthenticated loginStatus on initialization', () => {
    const { loginStatus } = useAuthStore.getState();
    expect(loginStatus).toBe('unauthenticated');
  });
});

describe('authStore - setTokens', () => {
  it('should set access and refresh tokens', () => {
    act(() => {
      useAuthStore.getState().setTokens('access-token-123', 'refresh-token-456');
    });
    const { accessToken, refreshToken } = useAuthStore.getState();
    expect(accessToken).toBe('access-token-123');
    expect(refreshToken).toBe('refresh-token-456');
  });
});

describe('authStore - clearTokens', () => {
  it('should clear access and refresh tokens', () => {
    act(() => {
      useAuthStore.getState().setTokens('access-token-123', 'refresh-token-456');
      useAuthStore.getState().clearTokens();
    });
    const { accessToken, refreshToken } = useAuthStore.getState();
    expect(accessToken).toBeNull();
    expect(refreshToken).toBeNull();
  });
});

describe('authStore - login success', () => {
  it('should set tokens and authenticated status on successful login', async () => {
    // MSW handler in handlers.ts already returns mock tokens for /api/fighter/login
    // We also need to handle getUserInfo call that happens post-login
    server.use(
      http.get('/api/auth/v1/manage/info', () => {
        return HttpResponse.json({
          userInfo: { email: 'test@example.com', username: 'testuser', isAdmin: false },
          fighterInfo: { id: 1, fighterName: 'Test Fighter', role: 0, beltColor: 'White', beltRank: 'White', experience: 0, height: 170, weight: 70, bmi: 24.2, birthdate: '2000-01-01' },
        });
      })
    );

    let result: { successful: boolean; response: string | null };
    await act(async () => {
      result = await useAuthStore.getState().login({ email: 'test@example.com', password: 'password123' });
    });

    const { accessToken, refreshToken, loginStatus } = useAuthStore.getState();
    expect(result!.successful).toBe(true);
    expect(accessToken).toBe('mock-access-token');
    expect(refreshToken).toBe('mock-refresh-token');
    expect(loginStatus).toBe('authenticated');
  });
});

describe('authStore - login failure', () => {
  it('should return unsuccessful and set unauthenticated status on failed login', async () => {
    server.use(
      http.post('/api/fighter/login', () => {
        return new HttpResponse('Invalid credentials', { status: 401 });
      })
    );

    let result: { successful: boolean; response: string | null };
    await act(async () => {
      result = await useAuthStore.getState().login({ email: 'bad@example.com', password: 'wrong' });
    });

    const { loginStatus } = useAuthStore.getState();
    expect(result!.successful).toBe(false);
    expect(loginStatus).toBe('unauthenticated');
  });
});

describe('authStore - logout', () => {
  it('should clear tokens, user, and set unauthenticated on logout', () => {
    // Set authenticated state first
    act(() => {
      useAuthStore.setState({
        accessToken: 'some-token',
        refreshToken: 'some-refresh',
        user: { email: 'test@example.com', username: 'test', isAdmin: false },
        loginStatus: 'authenticated',
      });
    });

    act(() => {
      useAuthStore.getState().logout();
    });

    const { accessToken, refreshToken, user, loginStatus } = useAuthStore.getState();
    expect(accessToken).toBeNull();
    expect(refreshToken).toBeNull();
    expect(user).toBeNull();
    expect(loginStatus).toBe('unauthenticated');
  });
});

describe('authStore - hydrate with valid tokens', () => {
  it('should refresh tokens and set authenticated status with valid tokens', async () => {
    server.use(
      http.post('/api/auth/v1/refresh', () => {
        return HttpResponse.json({
          accessToken: 'new-access-token',
          refreshToken: 'new-refresh-token',
        });
      }),
      http.get('/api/auth/v1/manage/info', () => {
        return HttpResponse.json({
          userInfo: { email: 'test@example.com', username: 'testuser', isAdmin: false },
          fighterInfo: null,
        });
      })
    );

    act(() => {
      useAuthStore.setState({
        accessToken: 'old-access-token',
        refreshToken: 'old-refresh-token',
      });
    });

    await act(async () => {
      await useAuthStore.getState().hydrate();
    });

    const { accessToken, loginStatus } = useAuthStore.getState();
    expect(accessToken).toBe('new-access-token');
    expect(loginStatus).toBe('authenticated');
  });
});

describe('authStore - hydrate with expired/missing tokens', () => {
  it('should set unauthenticated when no tokens are present', async () => {
    // loginStatus starts as unauthenticated, tokens are null
    // hydrate redirects to /login - we just check the state
    await act(async () => {
      await useAuthStore.getState().hydrate();
    });

    const { loginStatus } = useAuthStore.getState();
    expect(loginStatus).toBe('unauthenticated');
  });
});

describe('authStore - registerFighter', () => {
  it('should return successful true when registration returns 201', async () => {
    server.use(
      http.post('/api/fighter/register', () => {
        return new HttpResponse(null, { status: 201 });
      })
    );

    let result: { successful: boolean; response: string | null };
    await act(async () => {
      result = await useAuthStore.getState().registerFighter({
        email: 'new@example.com',
        password: 'Password123!',
        fighterRole: 'Student',
        fighterName: 'New Fighter',
        height: 175,
        weight: 75,
        bmi: 24.5,
        gender: 'Male',
        birthdate: '1995-06-15',
        beltColor: 'White',
        maxWorkoutDuration: 5,
        experience: 0,
      });
    });

    expect(result!.successful).toBe(true);
    expect(result!.response).toBeNull();
  });

  it('should return successful false when registration returns non-201', async () => {
    server.use(
      http.post('/api/fighter/register', () => {
        return new HttpResponse('Email already exists', { status: 400 });
      })
    );

    let result: { successful: boolean; response: string | null };
    await act(async () => {
      result = await useAuthStore.getState().registerFighter({
        email: 'existing@example.com',
        password: 'Password123!',
        fighterRole: 'Student',
        fighterName: 'Existing Fighter',
        height: 175,
        weight: 75,
        bmi: 24.5,
        gender: 'Male',
        birthdate: '1995-06-15',
        beltColor: 'White',
        maxWorkoutDuration: 5,
        experience: 0,
      });
    });

    expect(result!.successful).toBe(false);
  });
});

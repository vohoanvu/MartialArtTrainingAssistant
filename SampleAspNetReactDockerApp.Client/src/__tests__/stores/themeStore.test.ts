import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { act } from '@testing-library/react';

// Mock window.matchMedia before any imports
vi.stubGlobal('matchMedia', vi.fn().mockImplementation((query: string) => ({
  matches: false,
  media: query,
  onchange: null,
  addListener: vi.fn(),
  removeListener: vi.fn(),
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
  dispatchEvent: vi.fn(),
})));

import useThemeStore from '@/store/themeStore';

beforeEach(() => {
  // Reset DOM classes
  document.documentElement.classList.remove('dark', 'light', 'system');
  // Reset store theme state
  useThemeStore.setState({ theme: 'light' });
});

afterEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
});

describe('themeStore - store structure', () => {
  it('should have a setTheme action', () => {
    const { setTheme } = useThemeStore.getState();
    expect(typeof setTheme).toBe('function');
  });

  it('should have a storageKey of vite-ui-theme', () => {
    const { storageKey } = useThemeStore.getState();
    expect(storageKey).toBe('vite-ui-theme');
  });
});

describe('themeStore - setTheme', () => {
  it('should update theme state to dark', () => {
    act(() => {
      useThemeStore.getState().setTheme('dark');
    });
    const { theme } = useThemeStore.getState();
    expect(theme).toBe('dark');
  });

  it('should add dark class to document root when theme is dark', () => {
    act(() => {
      useThemeStore.getState().setTheme('dark');
    });
    expect(document.documentElement.classList.contains('dark')).toBe(true);
    expect(document.documentElement.classList.contains('light')).toBe(false);
  });

  it('should persist theme to localStorage', () => {
    act(() => {
      useThemeStore.getState().setTheme('light');
    });
    expect(localStorage.getItem('vite-ui-theme')).toBe('light');
  });

  it('should resolve system theme to light when matchMedia dark returns false', () => {
    act(() => {
      useThemeStore.getState().setTheme('system');
    });
    const { theme } = useThemeStore.getState();
    // matchMedia always returns false for dark query, so system resolves to 'light'
    expect(theme).toBe('light');
  });
});

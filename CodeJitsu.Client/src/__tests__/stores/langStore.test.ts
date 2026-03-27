import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { act } from '@testing-library/react';

// Mock i18next before importing the store
vi.mock('i18next', () => ({
  default: {
    changeLanguage: vi.fn(),
  },
}));

import { useLangStore } from '@/store/langStore';
import i18next from 'i18next';

beforeEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
  document.documentElement.classList.remove('en', 'pl');
  useLangStore.setState({ lang: 'en' });
});

afterEach(() => {
  localStorage.clear();
});

describe('langStore - store structure', () => {
  it('should have a storageKey of i18nextLng', () => {
    const { storageKey } = useLangStore.getState();
    expect(storageKey).toBe('i18nextLng');
  });

  it('should expose a setLang action', () => {
    const { setLang } = useLangStore.getState();
    expect(typeof setLang).toBe('function');
  });
});

describe('langStore - setLang', () => {
  it('should set language to en and persist in localStorage', () => {
    act(() => {
      useLangStore.getState().setLang('en');
    });
    const { lang } = useLangStore.getState();
    expect(lang).toBe('en');
    expect(localStorage.getItem('i18nextLng')).toBe('en');
  });

  it('should set language to pl and call i18next.changeLanguage', () => {
    act(() => {
      useLangStore.getState().setLang('pl');
    });
    const { lang } = useLangStore.getState();
    expect(lang).toBe('pl');
    expect(i18next.changeLanguage).toHaveBeenCalledWith('pl');
  });

  it('should resolve system language to en when navigator.language is en-US', () => {
    // jsdom default navigator.language is typically 'en-US', which doesn't start with 'pl'
    act(() => {
      useLangStore.getState().setLang('system');
    });
    const { lang } = useLangStore.getState();
    expect(lang).toBe('en');
  });
});

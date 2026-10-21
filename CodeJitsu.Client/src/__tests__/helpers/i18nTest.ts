import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import enJSON from '@/locales/en/translation.json';

/**
 * Real English i18n for component tests (instead of echoing keys), so tests also prove that the
 * translation keys used by the components actually exist.
 */
export async function initTestI18n() {
    if (!i18n.isInitialized) {
        await i18n.use(initReactI18next).init({
            lng: 'en',
            fallbackLng: 'en',
            resources: { en: { ...enJSON } },
            interpolation: { escapeValue: false },
            react: { useSuspense: false },
        });
    }
    return i18n;
}

/** jsdom lacks the pointer-capture / scroll APIs that Radix Select relies on. */
export function installRadixPolyfills() {
    const proto = window.HTMLElement.prototype as unknown as Record<string, unknown>;
    proto.hasPointerCapture ??= () => false;
    proto.setPointerCapture ??= () => undefined;
    proto.releasePointerCapture ??= () => undefined;
    proto.scrollIntoView ??= () => undefined;
}

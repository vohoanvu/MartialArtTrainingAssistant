/**
 * Formats the moment the next analysis slot frees up in the user's locale: time only when it is
 * today ("18:40"), otherwise short date + time ("Oct 22, 06:10 PM"). Returns null for a missing or
 * unparsable value.
 */
export function formatQuotaResetTime(iso: string | null | undefined, locale: string, now: Date = new Date()): string | null {
    if (!iso) return null;
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return null;
    const sameDay =
        date.getFullYear() === now.getFullYear() &&
        date.getMonth() === now.getMonth() &&
        date.getDate() === now.getDate();
    return sameDay
        ? date.toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit' })
        : date.toLocaleString(locale, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

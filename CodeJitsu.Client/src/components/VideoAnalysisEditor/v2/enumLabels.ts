// Enum → i18n key helpers (all display text lives in the locale files) plus color/threshold maps.

export const techniqueCategoryKey = (c: string) => `videoReviewV2.enums.techniqueCategory.${c}`;
export const positionKey = (p: string) => `videoReviewV2.enums.position.${p}`;
export const outcomeKey = (o: string) => `videoReviewV2.enums.outcome.${o}`;
export const actorKey = (a: string) => `videoReviewV2.enums.actor.${a}`;
export const severityKey = (s: string) => `videoReviewV2.enums.severity.${s}`;

export const ALL_TECHNIQUE_CATEGORIES = [
    'Takedown', 'Submission', 'Sweep', 'Pass', 'Escape', 'Transition',
    'Control', 'Defense', 'GuardPull', 'Scramble', 'StandUp', 'GripFight',
] as const;

export const ALL_POSITIONS = [
    'Standing', 'OpenGuard', 'ClosedGuard', 'HalfGuard', 'SideControl', 'Mount',
    'BackControl', 'Turtle', 'KneeOnBelly', 'NorthSouth', 'FiftyFifty', 'Crucifix', 'Scramble',
] as const;

export const ALL_OUTCOMES = ['Successful', 'Failed', 'Partial', 'Countered', 'InProgress'] as const;
export const ALL_ACTORS = ['Student', 'Opponent'] as const;
export const ALL_SEVERITIES = ['Critical', 'Major', 'Minor'] as const;

export const outcomeBadgeClass: Record<string, string> = {
    Successful: 'bg-green-100 text-green-700',
    Failed: 'bg-blood-100 text-blood-600',
    Partial: 'bg-amber-100 text-amber-700',
    Countered: 'bg-orange-100 text-orange-700',
    InProgress: 'bg-parchment-200 text-slate-zen400',
};

export const actorBadgeClass: Record<string, string> = {
    Student: 'bg-blue-100 text-blue-700',
    Opponent: 'bg-red-100 text-red-700',
};

export const actorMarkerColor: Record<string, string> = {
    Student: '#3b82f6', // blue
    Opponent: '#ef4444', // red
};

export const severityBadgeClass: Record<string, string> = {
    Critical: 'bg-blood-100 text-blood-700',
    Major: 'bg-amber-100 text-amber-700',
    Minor: 'bg-parchment-200 text-slate-zen400',
};

export const LOW_CONFIDENCE_THRESHOLD = 0.7;

/** Formats absolute milliseconds as mm:ss for compact display. */
export const formatMs = (ms: number): string => {
    if (!Number.isFinite(ms) || ms < 0) ms = 0;
    const totalSeconds = Math.floor(ms / 1000);
    const mins = Math.floor(totalSeconds / 60);
    const secs = totalSeconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
};

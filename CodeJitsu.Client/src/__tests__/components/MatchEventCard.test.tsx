import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import MatchEventCard from '@/components/VideoAnalysisEditor/v2/MatchEventCard';
import { MatchEvent } from '@/types/global';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

const baseEvent: MatchEvent = {
  id: 1,
  startTimestampMs: 1000,
  endTimestampMs: 3000,
  actor: 'Student',
  techniqueCategory: 'Takedown',
  techniqueName: 'Double Leg',
  positionBefore: 'Standing',
  positionAfter: 'SideControl',
  outcome: 'Successful',
  guardType: null,
  submissionType: null,
  actionsDescription: 'Shot a clean double leg',
  confidence: 0.4, // low confidence
};

describe('MatchEventCard', () => {
  it('seeks to the event start when the play button is clicked', () => {
    const onSeekMs = vi.fn();
    render(
      <MatchEventCard event={baseEvent} index={0} onChange={vi.fn()} onDelete={vi.fn()} onSeekMs={onSeekMs} />
    );
    // The seek button shows the formatted start time (mm:ss).
    fireEvent.click(screen.getByText(/00:01/));
    expect(onSeekMs).toHaveBeenCalledWith(1000);
  });

  it('flags low-confidence events', () => {
    render(
      <MatchEventCard event={baseEvent} index={0} onChange={vi.fn()} onDelete={vi.fn()} onSeekMs={vi.fn()} />
    );
    // ConfidenceBadge renders the warning glyph for confidence < 0.7.
    expect(screen.getByText('⚠')).toBeInTheDocument();
  });

  it('emits onChange when the actor is edited', () => {
    const onChange = vi.fn();
    render(
      <MatchEventCard event={baseEvent} index={0} onChange={onChange} onDelete={vi.fn()} onSeekMs={vi.fn()} />
    );
    // First combobox is the actor select.
    const selects = screen.getAllByRole('combobox');
    fireEvent.change(selects[0], { target: { value: 'Opponent' } });
    expect(onChange).toHaveBeenCalledWith(expect.objectContaining({ actor: 'Opponent' }));
  });

  it('emits onDelete when the remove button is clicked', () => {
    const onDelete = vi.fn();
    render(
      <MatchEventCard event={baseEvent} index={0} onChange={vi.fn()} onDelete={onDelete} onSeekMs={vi.fn()} />
    );
    fireEvent.click(screen.getByText('✕'));
    expect(onDelete).toHaveBeenCalled();
  });
});

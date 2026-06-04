import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import AnalysisV2Panel from '@/components/VideoAnalysisEditor/v2/AnalysisV2Panel';
import { AnalysisV2Dto } from '@/types/global';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { changeLanguage: vi.fn() } }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
}));

const analysis: AnalysisV2Dto = {
  id: 1,
  videoId: 1,
  visualDna: 'Athlete in a white gi.',
  pipelineStatus: 'Complete',
  technicalGrade: 78,
  gradeLabel: 'Solid Fundamentals',
  eliteTip: 'Keep your elbows tight.',
  matchSummary: 'A competitive match.',
  matchEvents: [
    {
      id: 1, startTimestampMs: 1000, endTimestampMs: 3000, actor: 'Student',
      techniqueCategory: 'Takedown', techniqueName: 'Double Leg', positionBefore: 'Standing',
      positionAfter: 'SideControl', outcome: 'Successful', guardType: null, submissionType: null,
      actionsDescription: 'Clean entry', confidence: 0.9,
    },
  ],
  coachingReport: {
    id: 1, matchSummary: 'A competitive match.', technicalGrade: 78,
    gradeLabel: 'Solid Fundamentals', eliteTip: 'Keep your elbows tight.',
    keyStrengths: [{ title: 'Strong takedowns', explanation: 'Good entries' }],
    criticalWeaknesses: [{ title: 'Guard retention', explanation: 'Got swept', severity: 'Major', scoringImpact: '2 pts' }],
    prescribedDrills: [{ drillName: 'Guard retention drill', instructions: 'Retain guard', goal: 'Stop sweeps' }],
  },
};

describe('AnalysisV2Panel', () => {
  it('renders the technical grade and switches tabs', () => {
    render(<AnalysisV2Panel analysis={analysis} onSeekMs={vi.fn()} onSave={vi.fn()} />);

    // Grade gauge input shows the grade.
    expect((screen.getByLabelText('technical grade') as HTMLInputElement).value).toBe('78');

    // Switch to the coaching tab.
    fireEvent.click(screen.getByText('videoReviewV2.tabs.coaching'));
    expect(screen.getByText('videoReviewV2.coaching.strengthsTitle')).toBeInTheDocument();

    // Switch to the drills tab.
    fireEvent.click(screen.getByText('videoReviewV2.tabs.drills'));
    expect(screen.getByText('videoReviewV2.drills.title')).toBeInTheDocument();
  });

  it('enables Save after an edit and calls onSave with the draft', async () => {
    const onSave = vi.fn().mockResolvedValue(undefined);
    render(<AnalysisV2Panel analysis={analysis} onSeekMs={vi.fn()} onSave={onSave} />);

    const saveBtn = screen.getByText('videoReviewV2.saveChanges').closest('button')!;
    expect(saveBtn).toBeDisabled();

    // Add a match event → marks the draft dirty.
    fireEvent.click(screen.getByText(/videoReviewV2.timeline.addEvent/));
    await waitFor(() => expect(saveBtn).not.toBeDisabled());

    fireEvent.click(saveBtn);
    expect(onSave).toHaveBeenCalledTimes(1);
    const draft = onSave.mock.calls[0][0] as AnalysisV2Dto;
    expect(draft.matchEvents).toHaveLength(2); // original + added
  });
});

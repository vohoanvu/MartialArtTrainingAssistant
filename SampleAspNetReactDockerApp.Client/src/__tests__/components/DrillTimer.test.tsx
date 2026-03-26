import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act, fireEvent } from '@testing-library/react';
import DrillTimer from '@/components/ClassSessionManagement/DrillTimer';

// Mock HTMLAudioElement since jsdom doesn't support it
const mockAudioPlay = vi.fn().mockResolvedValue(undefined);
const mockAudioPause = vi.fn();
const mockAudioLoad = vi.fn();
const mockAudio = {
  play: mockAudioPlay,
  pause: mockAudioPause,
  load: mockAudioLoad,
  currentTime: 0,
};

vi.stubGlobal('Audio', class { play = mockAudioPlay; pause = mockAudioPause; load = mockAudioLoad; currentTime = 0; });

beforeEach(() => {
  vi.clearAllMocks();
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('DrillTimer', () => {
  it('renders with the correct initial formatted time', () => {
    render(<DrillTimer initialDurationMinutes={3} drillName="Warm Up" />);
    // 3 minutes = 03:00
    expect(screen.getByText('03:00')).toBeInTheDocument();
  });

  it('renders the drill name when provided', () => {
    render(<DrillTimer initialDurationMinutes={2} drillName="Guard Passing Drill" />);
    expect(screen.getByText('Guard Passing Drill')).toBeInTheDocument();
  });

  it('renders a Start button when timer is not running', () => {
    render(<DrillTimer initialDurationMinutes={1} />);
    expect(screen.getByText(/Start/i)).toBeInTheDocument();
  });

  it('shows Pause button after clicking Start', () => {
    render(<DrillTimer initialDurationMinutes={2} />);

    act(() => { fireEvent.click(screen.getByText(/Start/i)); });

    expect(screen.getByText(/Pause/i)).toBeInTheDocument();
  });

  it('shows Resume and Reset buttons after pausing', () => {
    render(<DrillTimer initialDurationMinutes={2} />);

    act(() => { fireEvent.click(screen.getByText(/Start/i)); });
    act(() => { vi.advanceTimersByTime(1000); });
    act(() => { fireEvent.click(screen.getByText(/Pause/i)); });

    expect(screen.getByText(/Resume/i)).toBeInTheDocument();
    expect(screen.getByText(/Reset/i)).toBeInTheDocument();
  });

  it('resets timer to initial value when Reset is clicked', () => {
    render(<DrillTimer initialDurationMinutes={1} />);

    act(() => { fireEvent.click(screen.getByText(/Start/i)); });
    act(() => { vi.advanceTimersByTime(2000); });
    act(() => { fireEvent.click(screen.getByText(/Pause/i)); });
    act(() => { fireEvent.click(screen.getByText(/Reset/i)); });

    expect(screen.getByText('01:00')).toBeInTheDocument();
  });
});

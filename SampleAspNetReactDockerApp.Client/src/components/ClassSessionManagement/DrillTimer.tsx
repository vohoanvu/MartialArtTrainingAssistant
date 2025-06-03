// src/components/DrillTimer.tsx
import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { Button } from '@/components/ui/button';
import { PlayIcon, PauseIcon, ResetIcon } from '@radix-ui/react-icons';

interface DrillTimerProps {
    initialDurationMinutes: number;
    drillName?: string;
    onTimerFinish?: () => void;
    themeColor?: string;
    progressColor?: string;
    timerSoundSrc?: string; // Single sound file for all effects
}

const soundSegments = {
    start: { time: 0, duration: 2 },     // Single bell ring (approx. 2s)
    pauseResume: { time: 9, duration: 2 }, // Double bell ring (approx. 2s)
    finish: { time: 17, duration: 3 },   // Triple bell ring (approx. 3s)
};

const DrillTimer: React.FC<DrillTimerProps> = ({
    initialDurationMinutes,
    drillName,
    onTimerFinish,
    themeColor = 'bg-primary',
    progressColor = 'bg-primary-foreground',
    timerSoundSrc = '/sounds/boxing-bell-signals-6115.mp3', // Your specified audio sprite
}) => {
    const initialTotalSeconds = useMemo(() => initialDurationMinutes * 60, [initialDurationMinutes]);
    const [timeLeft, setTimeLeft] = useState<number | null>(initialTotalSeconds);
    const [isRunning, setIsRunning] = useState<boolean>(false);

    const audioRef = useRef<HTMLAudioElement | null>(null);
    const soundTimeoutRef = useRef<NodeJS.Timeout | null>(null); // To stop playback after segment duration

    useEffect(() => {
        if (timerSoundSrc) {
            audioRef.current = new Audio(timerSoundSrc);
            audioRef.current.load(); // Preload the entire audio sprite
        }
        // Cleanup audio element on component unmount
        return () => {
            if (audioRef.current) {
                audioRef.current.pause();
                audioRef.current = null;
            }
            if (soundTimeoutRef.current) {
                clearTimeout(soundTimeoutRef.current);
            }
        };
    }, [timerSoundSrc]);

    const playSoundSegment = useCallback((segment: { time: number, duration: number }) => {
        if (audioRef.current) {
            const audio = audioRef.current;
            audio.pause(); // Stop any previous playback
            audio.currentTime = segment.time; // Seek to the start of the segment

            // Clear any existing timeout to stop sound
            if (soundTimeoutRef.current) {
                clearTimeout(soundTimeoutRef.current);
            }

            audio.play().then(() => {
                // Set a timeout to stop the sound after its specific duration
                soundTimeoutRef.current = setTimeout(() => {
                    audio.pause();
                }, segment.duration * 1000); // Convert duration to milliseconds
            }).catch(error => console.error("Error playing sound segment:", error));
        }
    }, []);

    const formatTime = (totalSeconds: number | null): string => {
        if (totalSeconds === null) return formatTime(initialTotalSeconds);
        if (totalSeconds < 0) totalSeconds = 0; // Ensure we don't display negative time

        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;
        return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    };

    const startTimerHandler = useCallback(() => {
        if (initialDurationMinutes > 0) {
            if (timeLeft === null || timeLeft === initialTotalSeconds) {
                setTimeLeft(initialTotalSeconds);
            }
            setIsRunning(true);
            playSoundSegment(soundSegments.start);
        }
    }, [initialDurationMinutes, initialTotalSeconds, playSoundSegment, timeLeft]);

    const pauseTimerHandler = () => {
        setIsRunning(false);
        playSoundSegment(soundSegments.pauseResume);
    };

    const resumeTimerHandler = () => {
        setIsRunning(true);
        playSoundSegment(soundSegments.pauseResume);
    };

    useEffect(() => {
        // Handle the case where timer finishes
        if (isRunning && timeLeft === 0) {
            console.log(`${drillName || 'Timer'} finished!`);
            playSoundSegment(soundSegments.finish);
            if (onTimerFinish) {
                onTimerFinish();
            }
            setIsRunning(false); // Stop the timer immediately when it hits zero
            return;
        }

        // If timer is not running, or time is up (or null), don't start/continue interval
        if (!isRunning || timeLeft === null || timeLeft < 0) {
            return;
        }

        // If timer is running and timeLeft > 0, set up the interval
        const intervalId = setInterval(() => {
            setTimeLeft((prevTime) => {
                if (prevTime === null) return 0; // Should not happen if isRunning is true
                if (prevTime <= 1) { // When prevTime is 1, next will be 0.
                    // This is where we'll hit the condition above in the next render.
                    return 0;
                }
                return prevTime - 1;
            });
        }, 1000);

        return () => clearInterval(intervalId);
    }, [isRunning, timeLeft, drillName, onTimerFinish, playSoundSegment]);

    const resetTimer = useCallback(() => {
        setIsRunning(false);
        setTimeLeft(initialTotalSeconds);
        if (audioRef.current) {
            audioRef.current.pause();
            // No need to set currentTime to 0 for the whole sprite on reset,
            // as playSoundSegment will set it correctly.
        }
        if (soundTimeoutRef.current) {
            clearTimeout(soundTimeoutRef.current);
        }
    }, [initialTotalSeconds]);

    const progressPercentage = useMemo(() => {
        if (timeLeft === null || initialTotalSeconds === 0) return 100;
        if(initialTotalSeconds === 0) return 0;
        return Math.max(0, (timeLeft / initialTotalSeconds) * 100);
    }, [timeLeft, initialTotalSeconds]);

    const timeDisplayColor = useMemo(() => {
        if (timeLeft !== null && timeLeft <= 10 && initialTotalSeconds > 10) {
            return 'text-red-500 dark:text-red-400 animate-pulse';
        }
        if (timeLeft !== null && timeLeft <= Math.floor(initialTotalSeconds * 0.25) && initialTotalSeconds > 30) {
            return 'text-yellow-500 dark:text-yellow-400';
        }
        return 'text-foreground';
    }, [timeLeft, initialTotalSeconds]);

    return (
        <div className="mt-3 p-4 border rounded-lg shadow-md bg-card w-full max-w-xs mx-auto sm:mx-0">
            {drillName && <h4 className="text-sm font-medium text-muted-foreground mb-2 text-center">{drillName}</h4>}

            <div className={`relative font-mono text-5xl font-bold p-3 rounded-md text-center mb-3 ${timeDisplayColor} transition-colors duration-300`}>
                {formatTime(timeLeft)}
            </div>

            <div className="w-full bg-muted rounded-full h-2.5 mb-4 dark:bg-background/30">
                <div 
                    className={`${progressColor} h-2.5 rounded-full transition-all duration-300 ease-linear`} 
                    style={{ width: `${progressPercentage}%` }} //progressPercentage handles null
                ></div>
            </div>

            <div className="flex justify-center space-x-2">
                {!isRunning && (timeLeft === initialTotalSeconds || timeLeft === null) && ( 
                    <Button onClick={startTimerHandler} size="sm" className={`w-full ${themeColor} hover:opacity-90`}>
                        <PlayIcon className="mr-2 h-4 w-4" /> Start 
                    </Button>
                )}

                {isRunning && (
                    <Button onClick={pauseTimerHandler} size="sm" variant="outline" className="w-1/2">
                        <PauseIcon className="mr-2 h-4 w-4" /> Pause
                    </Button>
                )}
                {!isRunning && timeLeft !== null && timeLeft > 0 && timeLeft < initialTotalSeconds && (
                     <Button onClick={resumeTimerHandler} size="sm" variant="outline" className="w-1/2">
                        <PlayIcon className="mr-2 h-4 w-4" /> Resume
                    </Button>
                )}

                {(isRunning || (timeLeft !== null && timeLeft < initialTotalSeconds)) && ( 
                     <Button onClick={resetTimer} size="sm" variant="ghost" className="w-1/2">
                        <ResetIcon className="mr-2 h-4 w-4" /> Reset
                    </Button>
                )}
            </div>
        </div>
    );
};

export default DrillTimer;
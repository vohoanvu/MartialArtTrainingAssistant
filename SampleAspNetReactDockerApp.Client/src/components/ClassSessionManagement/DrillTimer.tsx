// src/components/DrillTimer.tsx
import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { Button } from '@/components/ui/button';
import { PlayIcon, PauseIcon, ResetIcon } from '@radix-ui/react-icons'; // Using Radix icons, common with Shadcn

interface DrillTimerProps {
    initialDurationMinutes: number;
    drillName?: string;
    onTimerFinish?: () => void;
    // Add a color prop for theming, e.g., based on section (warm-up, drills, etc.)
    // These would be Tailwind color classes like 'bg-orange-500', 'text-blue-600'
    themeColor?: string; // e.g., 'bg-primary', 'bg-accent' or specific like 'bg-green-500'
    progressColor?: string; // e.g., 'bg-green-600'
}

const DrillTimer: React.FC<DrillTimerProps> = ({
    initialDurationMinutes,
    drillName,
    onTimerFinish,
    themeColor = 'bg-primary', // Default theme color
    progressColor = 'bg-primary-foreground', // Default progress bar color
}) => {
    const initialTotalSeconds = useMemo(() => initialDurationMinutes * 60, [initialDurationMinutes]);
    const [timeLeft, setTimeLeft] = useState<number | null>(null); // in seconds
    const [isRunning, setIsRunning] = useState<boolean>(false);

    const formatTime = (totalSeconds: number | null): string => {
        if (totalSeconds === null) return '--:--';
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;
        return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    };

    const startTimerHandler = useCallback(() => {
        if (initialDurationMinutes > 0) {
            setTimeLeft(initialTotalSeconds);
            setIsRunning(true);
        }
    }, [initialDurationMinutes, initialTotalSeconds]);

    useEffect(() => {
        if (!isRunning || timeLeft === null || timeLeft < 0) { // Changed to < 0 to allow 00:00 to display
            if (isRunning && timeLeft === 0) { // Timer reached exactly zero
                setIsRunning(false);
                // Consider a more subtle notification than alert, e.g., visual change + console log
                console.log(`${drillName || 'Timer'} finished!`);
                if (onTimerFinish) {
                    onTimerFinish();
                }
            }
            return;
        }

        const intervalId = setInterval(() => {
            setTimeLeft((prevTime) => (prevTime !== null ? prevTime - 1 : 0));
        }, 1000);

        return () => clearInterval(intervalId);
    }, [isRunning, timeLeft, drillName, onTimerFinish]);

    const resetTimer = () => {
        setIsRunning(false);
        setTimeLeft(null); // Reset to initial prompt state
    };

    const progressPercentage = useMemo(() => {
        if (timeLeft === null || initialTotalSeconds === 0) return 0;
        return Math.max(0, (timeLeft / initialTotalSeconds) * 100);
    }, [timeLeft, initialTotalSeconds]);

    // Determine dynamic text color for countdown based on time left for a "warning" effect
    const timeDisplayColor = useMemo(() => {
        if (timeLeft !== null && timeLeft <= 10 && initialTotalSeconds > 10) { // Last 10 seconds warning (if timer is longer than 10s)
            return 'text-red-500 dark:text-red-400 animate-pulse';
        }
        if (timeLeft !== null && timeLeft <= Math.floor(initialTotalSeconds * 0.25) && initialTotalSeconds > 30) { // Last 25% warning (if timer is longer than 30s)
            return 'text-yellow-500 dark:text-yellow-400';
        }
        return 'text-foreground';
    }, [timeLeft, initialTotalSeconds]);


    return (
        <div className="mt-3 p-4 border rounded-lg shadow-md bg-card w-full max-w-xs mx-auto sm:mx-0"> {/* Constrain width and center on small screens if alone */}
            {drillName && <h4 className="text-sm font-medium text-muted-foreground mb-2 text-center">{drillName}</h4>}

            <div className={`relative font-mono text-5xl font-bold p-3 rounded-md text-center mb-3 ${timeDisplayColor} transition-colors duration-300`}>
                {formatTime(timeLeft === null ? initialTotalSeconds : timeLeft)}
            </div>

            {/* Progress Bar */}
            <div className="w-full bg-muted rounded-full h-2.5 mb-4 dark:bg-background/30">
                <div
                    className={`${progressColor} h-2.5 rounded-full transition-all duration-300 ease-linear`}
                    style={{ width: `${timeLeft === null ? 100 : progressPercentage}%` }}
                ></div>
            </div>

            <div className="flex justify-center space-x-2">
                {!isRunning && (timeLeft === null || timeLeft === initialTotalSeconds) && ( // Show Start only when pristine or reset
                    <Button onClick={startTimerHandler} size="sm" className={`w-full ${themeColor} hover:opacity-90`}>
                        <PlayIcon className="mr-2 h-4 w-4" /> Start
                    </Button>
                )}

                {isRunning && (
                    <Button onClick={() => setIsRunning(false)} size="sm" variant="outline" className="w-1/2">
                        <PauseIcon className="mr-2 h-4 w-4" /> Pause
                    </Button>
                )}
                {!isRunning && timeLeft !== null && timeLeft > 0 && (
                    <Button onClick={() => setIsRunning(true)} size="sm" variant="outline" className="w-1/2">
                        <PlayIcon className="mr-2 h-4 w-4" /> Resume
                    </Button>
                )}

                {/* Show Reset if timer has started or is running */}
                {timeLeft !== null && (
                    <Button onClick={resetTimer} size="sm" variant="ghost" className="w-1/2">
                        <ResetIcon className="mr-2 h-4 w-4" /> Reset
                    </Button>
                )}
            </div>
        </div>
    );
};

export default DrillTimer;
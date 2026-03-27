import React, { useRef, useState, useEffect, forwardRef, useImperativeHandle } from 'react';
import { Button } from '@/components/ui/button';
import { TechniqueDto } from '@/types/global';

interface VideoPlayerProps {
    videoUrl: string;
    videoId: string;
    identifiedTechniques: TechniqueDto[];
    selectedSegment: { start: string; end: string } | null;
    setSelectedSegment: (segment: { start: string; end: string } | null) => void;
    clearSelection: () => void;
    onSegmentSelect?: (start: string, end: string) => void;
}

export interface VideoPlayerHandle {
    seekTo: (timestamp: string) => void;
}

const parseTimespanToSeconds = (timestamp: string | null): number => {
    if (!timestamp || typeof timestamp !== 'string') return NaN;
    const parts = timestamp.split(':');
    if (parts.length !== 3) return NaN; // Expect HH:mm:ss
    const hours = parseInt(parts[0], 10);
    const minutes = parseInt(parts[1], 10);
    const seconds = parseInt(parts[2], 10);
    if (isNaN(hours) || isNaN(minutes) || isNaN(seconds)) return NaN;
    return hours * 3600 + minutes * 60 + seconds;
};

const parseSecondsToTimespan = (seconds: number): string => {
    const hrs = Math.floor(seconds / 3600);
    const mins = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);
    return `${hrs.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
};

const VideoPlayer = forwardRef<VideoPlayerHandle, VideoPlayerProps>(({
    videoUrl,
    identifiedTechniques,
    selectedSegment,
    setSelectedSegment,
    clearSelection,
    onSegmentSelect,
}, ref) => {
    const videoRef = useRef<HTMLVideoElement>(null);
    const timelineRef = useRef<HTMLDivElement>(null);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration, setDuration] = useState(0);
    const [playing, setPlaying] = useState(false);
    const [dragStart, setDragStart] = useState<number | null>(null);
    const [dragEnd, setDragEnd] = useState<number | null>(null);
    const [isDragging, setIsDragging] = useState(false);
    const [mouseDownPosition, setMouseDownPosition] = useState<number | null>(null);
    const DRAG_THRESHOLD = 5; // Pixels to consider as a drag

    // Expose seekTo function to parent via ref
    useImperativeHandle(ref, () => ({
        seekTo: (timestamp: string) => {
            const seconds = parseTimespanToSeconds(timestamp);
            if (!isNaN(seconds) && videoRef.current) {
                videoRef.current.pause();
                videoRef.current.currentTime = seconds;
            }
        },
    }));

    useEffect(() => {
        const video = videoRef.current!;
        const handleLoadedMetadata = () => setDuration(video.duration);
        const handleTimeUpdate = () => setCurrentTime(video.currentTime);

        video.addEventListener('loadedmetadata', handleLoadedMetadata);
        video.addEventListener('timeupdate', handleTimeUpdate);

        return () => {
            video.removeEventListener('loadedmetadata', handleLoadedMetadata);
            video.removeEventListener('timeupdate', handleTimeUpdate);
        };
    }, []);

    const calculateTimestamp = (e: React.MouseEvent<HTMLDivElement> | MouseEvent) => {
        const timeline = timelineRef.current;
        if (!timeline) return 0;
        const rect = timeline.getBoundingClientRect();
        const clickX = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
        const timelineWidth = rect.width;
        return (clickX / timelineWidth) * duration;
    };

    const handleMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
        if (duration <= 0) return; // Prevent selection if video not loaded
        const startTimestamp = calculateTimestamp(e);
        setDragStart(startTimestamp);
        setDragEnd(startTimestamp);
        setIsDragging(true);
        setMouseDownPosition(e.clientX);
    };

    const handleMouseMove = (e: MouseEvent) => {
        if (isDragging && timelineRef.current) {
            const currentTimestamp = calculateTimestamp(e);
            setDragEnd(currentTimestamp);
            videoRef.current!.currentTime = currentTimestamp; // Real-time seeking
        }
    };

    const handleMouseUp = (e: MouseEvent) => {
        if (isDragging && timelineRef.current && mouseDownPosition !== null) {
            const endTimestamp = calculateTimestamp(e);
            const mouseUpPosition = e.clientX;
            const movement = Math.abs(mouseUpPosition - mouseDownPosition);

            if (movement < DRAG_THRESHOLD) {
                videoRef.current!.currentTime = endTimestamp;
            } else {
                const from = Math.min(dragStart!, endTimestamp);
                const to = Math.max(dragStart!, endTimestamp);
                const startFormatted = parseSecondsToTimespan(from);
                const endFormatted = parseSecondsToTimespan(to);
                setSelectedSegment({ start: startFormatted, end: endFormatted });
                onSegmentSelect?.(startFormatted, endFormatted);
            }

            setIsDragging(false);
            setDragStart(null);
            setDragEnd(null);
            setMouseDownPosition(null);
        }
    };

    useEffect(() => {
        if (isDragging) {
            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', handleMouseUp);
        }

        return () => {
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
        };
    }, [isDragging, dragStart, duration]);

    const handlePlayPause = () => {
        const video = videoRef.current!;
        if (playing) {
            video.pause();
        } else {
            video.play();
        }
        setPlaying(!playing);
    };

    const handleTimelineClick = (e: React.MouseEvent<HTMLDivElement>) => {
        const seekTime = calculateTimestamp(e);
        videoRef.current!.currentTime = seekTime;
    };

    const handleRightClick = (e: React.MouseEvent<HTMLDivElement>) => {
        e.preventDefault();
        const seekTime = calculateTimestamp(e);
        videoRef.current!.currentTime = seekTime;
        setCurrentTime(seekTime);
    };

    const handleClearSelection = () => {
        setSelectedSegment(null);
        clearSelection();
    };

    const getSelectedRangeStyle = () => {
        if (!selectedSegment) return {};
        const from = parseTimespanToSeconds(selectedSegment.start);
        const to = parseTimespanToSeconds(selectedSegment.end);
        if (isNaN(from) || isNaN(to)) return {};
        const leftPercent = (from / duration) * 100;
        const widthPercent = ((to - from) / duration) * 100;
        return {
            left: `${leftPercent}%`,
            width: `${widthPercent}%`,
            backgroundColor: 'rgba(59,130,246,0.35)',
            position: 'absolute',
            height: '100%',
            zIndex: 1,
        } as React.CSSProperties;
    };

    const getFromMarkerStyle = () => {
        if (!selectedSegment) return {};
        const from = parseTimespanToSeconds(selectedSegment.start);
        if (isNaN(from)) return {};
        const leftPercent = (from / duration) * 100;
        return {
            left: `calc(${leftPercent}% - 5px)`,
            position: 'absolute',
            top: '-10px',
            width: '0',
            height: '0',
            borderLeft: '5px solid transparent',
            borderRight: '5px solid transparent',
            borderTop: '10px solid var(--tw-destructive, #ef4444)',
            zIndex: 2,
        } as React.CSSProperties;
    };

    const getToMarkerStyle = () => {
        if (!selectedSegment) return {};
        const to = parseTimespanToSeconds(selectedSegment.end);
        if (isNaN(to)) return {};
        const leftPercent = (to / duration) * 100;
        return {
            left: `calc(${leftPercent}% - 5px)`,
            position: 'absolute',
            top: '-10px',
            width: '0',
            height: '0',
            borderLeft: '5px solid transparent',
            borderRight: '5px solid transparent',
            borderTop: '10px solid var(--tw-primary, #3b82f6)',
            zIndex: 2,
        } as React.CSSProperties;
    };

    const getFromDottedLineStyle = () => {
        if (!selectedSegment) return {};
        const from = parseTimespanToSeconds(selectedSegment.start);
        if (isNaN(from)) return {};
        const leftPercent = (from / duration) * 100;
        return {
            left: `${leftPercent}%`,
            position: 'absolute',
            top: '0',
            width: '1px',
            height: '100%',
            borderLeft: '1px dotted var(--tw-border, #d1d5db)',
            zIndex: 2,
        } as React.CSSProperties;
    };

    const getToDottedLineStyle = () => {
        if (!selectedSegment) return {};
        const to = parseTimespanToSeconds(selectedSegment.end);
        if (isNaN(to)) return {};
        const leftPercent = (to / duration) * 100;
        return {
            left: `${leftPercent}%`,
            position: 'absolute',
            top: '0',
            width: '1px',
            height: '100%',
            borderLeft: '1px dotted black',
            zIndex: 2,
        } as React.CSSProperties;
    };

    const getTemporaryRangeStyle = () => {
        if (!isDragging || dragStart === null || dragEnd === null) return {};
        const leftPercent = (Math.min(dragStart, dragEnd) / duration) * 100;
        const widthPercent = (Math.abs(dragEnd - dragStart) / duration) * 100;
        return {
            left: `${leftPercent}%`,
            width: `${widthPercent}%`,
            backgroundColor: 'var(--tw-bg-accent, rgba(59,130,246,0.1))',
            position: 'absolute',
            height: '100%',
            zIndex: 1,
        } as React.CSSProperties;
    };

    return (
        <div className="video-container">
            <div style={{ overflow: 'hidden' }}>
                <video ref={videoRef} src={videoUrl} controls className='w-full h-full' style={{ objectFit: 'contain' }} />
            </div>
            <div className="timeline-container mt-2 relative">
                <div
                    ref={timelineRef}
                    className="timeline w-full h-10 bg-muted dark:bg-neutral-800 relative rounded-md cursor-pointer border border-border"
                    onClick={handleTimelineClick}
                    onContextMenu={handleRightClick}
                    onMouseDown={handleMouseDown}
                >
                    {isDragging && dragStart !== null && dragEnd !== null && (
                        <div style={getTemporaryRangeStyle()} />
                    )}
                    {selectedSegment && (
                        <>
                            <div style={getFromMarkerStyle()} />
                            <div style={getToMarkerStyle()} />
                            <div style={getFromDottedLineStyle()} />
                            <div style={getToDottedLineStyle()} />
                            <div style={getSelectedRangeStyle()} />
                        </>
                    )}
                    <div
                        className="progress absolute h-full bg-green-500 rounded-md"
                        style={{ width: `${(currentTime / duration) * 100}%` }}
                    />
                    {identifiedTechniques.map((technique) => {
                        const timestampNum = parseTimespanToSeconds(technique.startTimestamp ?? null);
                        if (isNaN(timestampNum) || duration <= 0) {
                            return null;
                        }

                        const leftPercent = duration > 0 ? (timestampNum / duration) * 100 : 0;
                        return (
                            <div
                                key={technique.id}
                                className="marker absolute top-0 h-full cursor-pointer bg-yellow-400 dark:bg-yellow-300"
                                style={{
                                    left: `${leftPercent}%`, // Use calculated percent
                                    width: '5px',
                                }}
                                onClick={(e) => { // Prevent click from bubbling to timeline click
                                    e.stopPropagation();
                                    videoRef.current!.currentTime = timestampNum;
                                }}
                                title={`Technique: ${technique.name} (${technique.startTimestamp})`}
                            />
                        );
                    })}
                </div>
                {selectedSegment && (
                    <div className="mt-2 flex items-center justify-between">
                        <p className="text-sm text-muted-foreground">
                            Selected Segment: {selectedSegment.start} - {selectedSegment.end}
                        </p>
                        <Button
                            onClick={handleClearSelection}
                            variant="outline"
                            size="sm"
                            className="text-destructive"
                        >
                            Clear Selection
                        </Button>
                    </div>
                )}
            </div>
            <div className="controls flex items-center justify-between p-2 mt-2">
                <Button onClick={handlePlayPause}>{playing ? 'Pause' : 'Play'}</Button>
                <Button onClick={() => videoRef.current!.playbackRate = 0.5}>Slow Motion (0.5x)</Button>
                <Button onClick={() => videoRef.current!.playbackRate = 1.0}>Normal Speed (1.0x)</Button>
                <Button onClick={() => videoRef.current!.playbackRate = 2.0}>Fast Forward (2.0x)</Button>
                <Button onClick={() => videoRef.current!.currentTime -= 0.04}>Frame Back</Button>
                <Button onClick={() => videoRef.current!.currentTime += 0.04}>Frame Forward</Button>
            </div>
        </div>
    );
});

export default VideoPlayer;
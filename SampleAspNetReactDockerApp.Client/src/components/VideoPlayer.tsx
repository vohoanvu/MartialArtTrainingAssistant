import React, { useRef, useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { TechniqueIdentified } from '@/types/global';

interface VideoPlayerProps {
    videoUrl: string;
    videoId: string;
    identifiedTechniques: TechniqueIdentified[];
    setFromTimestamp: (timestamp: string) => void;
    setToTimestamp: (timestamp: string) => void;
    selectedSegment: { from: number; to: number } | null;
    setSelectedSegment: (segment: { from: number; to: number } | null) => void;
    clearSelection: () => void;
}

const parseTimestampToSeconds = (timestamp: string): number => {
    if (!timestamp || typeof timestamp !== 'string') return NaN;
    const parts = timestamp.split(':');
    if (parts.length !== 2) return NaN;
    const minutes = parseInt(parts[0], 10);
    const seconds = parseInt(parts[1], 10);
    if (isNaN(minutes) || isNaN(seconds)) return NaN;
    return minutes * 60 + seconds;
};

const VideoPlayer: React.FC<VideoPlayerProps> = ({
    videoUrl,
    identifiedTechniques,
    setFromTimestamp,
    setToTimestamp,
    selectedSegment,
    setSelectedSegment,
}) => {
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
                setSelectedSegment({ from, to }); // Persist the segment
                setFromTimestamp(from.toFixed(2));
                setToTimestamp(to.toFixed(2));
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

    const getSelectedRangeStyle = () => {
        if (!selectedSegment) return {};
        const leftPercent = (selectedSegment.from / duration) * 100;
        const widthPercent = ((selectedSegment.to - selectedSegment.from) / duration) * 100;
        return {
            left: `${leftPercent}%`,
            width: `${widthPercent}%`,
            backgroundColor: 'rgba(0, 0, 255, 0.3)', // Blue selection effect
            position: 'absolute',
            height: '100%',
            zIndex: 1,
        } as React.CSSProperties;
    };

    const getFromMarkerStyle = () => {
        if (!selectedSegment) return {};
        const leftPercent = (selectedSegment.from / duration) * 100;
        return {
            left: `calc(${leftPercent}% - 5px)`, // Center the triangle
            position: 'absolute',
            top: '-10px', // Position so the tip touches the timeline border
            width: '0',
            height: '0',
            borderLeft: '5px solid transparent',
            borderRight: '5px solid transparent',
            borderTop: '10px solid red', // Red triangle pointing down
            zIndex: 2,
        } as React.CSSProperties;
    };

    const getToMarkerStyle = () => {
        if (!selectedSegment) return {};
        const leftPercent = (selectedSegment.to / duration) * 100;
        return {
            left: `calc(${leftPercent}% - 5px)`,
            position: 'absolute',
            top: '-10px',
            width: '0',
            height: '0',
            borderLeft: '5px solid transparent',
            borderRight: '5px solid transparent',
            borderTop: '10px solid blue', // Blue triangle for "To"
            zIndex: 2,
        } as React.CSSProperties;
    };

    const getFromDottedLineStyle = () => {
        if (!selectedSegment) return {};
        const leftPercent = (selectedSegment.from / duration) * 100;
        return {
            left: `${leftPercent}%`,
            position: 'absolute',
            top: '0',
            width: '1px',
            height: '100%',
            borderLeft: '1px dotted black', // Dotted line for "From"
            zIndex: 2,
        } as React.CSSProperties;
    };

    const getToDottedLineStyle = () => {
        if (!selectedSegment) return {};
        const leftPercent = (selectedSegment.to / duration) * 100;
        return {
            left: `${leftPercent}%`,
            position: 'absolute',
            top: '0',
            width: '1px',
            height: '100%',
            borderLeft: '1px dotted black', // Dotted line for "To"
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
            backgroundColor: 'rgba(0, 0, 255, 0.1)', // Temporary highlight
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
                    className="timeline w-full h-6 bg-gray-300 relative rounded-md cursor-pointer"
                    onClick={handleTimelineClick}
                    onContextMenu={handleRightClick}
                    onMouseDown={handleMouseDown}
                >
                    {isDragging && dragStart !== null && dragEnd !== null && (
                        <div style={getTemporaryRangeStyle()} />
                    )}
                    {selectedSegment && (
                        <>
                            <div style={getFromMarkerStyle()} /> {/* "From" marker */}
                            <div style={getToMarkerStyle()} />   {/* "To" marker */}
                            <div style={getFromDottedLineStyle()} /> {/* "From" dotted line */}
                            <div style={getToDottedLineStyle()} />   {/* "To" dotted line */}
                            <div style={getSelectedRangeStyle()} />  {/* Selected range */}
                        </>
                    )}
                    <div
                        className="progress absolute h-full bg-green-500 rounded-md"
                        style={{ width: `${(currentTime / duration) * 100}%` }}
                    />
                    {identifiedTechniques.map((technique) => {
                        const timestampNum = parseTimestampToSeconds(technique.start_timestamp);
                        // Don't render marker if timestamp is invalid or duration is 0
                        if (isNaN(timestampNum) || duration <= 0) {
                            return null;
                        }

                        const leftPercent = duration > 0 ? (timestampNum / duration) * 100 : 0;
                        return (
                            <div
                                key={technique.technique_type}
                                className={`marker absolute top-0 h-full cursor-pointer bg-blue-500`}
                                style={{
                                    left: `${leftPercent}%`, // Use calculated percent
                                    width: '5px',
                                }}
                                onClick={(e) => { // Prevent click from bubbling to timeline click
                                    e.stopPropagation();
                                    videoRef.current!.currentTime = timestampNum;
                                }}
                                title={`Technique: ${technique.technique_name} (${technique.start_timestamp})`}
                            />
                        );
                    })}
                </div>
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
};

export default VideoPlayer;
import React, { useRef, useState, useEffect } from 'react';
import axios from 'axios';
import useAuthStore from '@/store/authStore';

interface Feedback {
    id: string;
    timestamp: number;
    feedback: string;
    aiInsights?: string;
}

interface VideoPlayerProps {
    videoUrl: string;
    videoId: string;
    feedbackList: Feedback[];
    onAddFeedback: (feedback: Feedback) => void;
}

const VideoPlayer: React.FC<VideoPlayerProps> = ({ videoUrl, videoId, feedbackList, onAddFeedback }) => {
    const videoRef = useRef<HTMLVideoElement>(null);
    const [currentTime, setCurrentTime] = useState(0);
    const [duration, setDuration] = useState(0);
    const [playing, setPlaying] = useState(false);
    const [showFeedbackForm, setShowFeedbackForm] = useState(false);
    const [feedbackText, setFeedbackText] = useState('');
    const [aiInsights, setAiInsights] = useState('');
    const { accessToken } = useAuthStore();
    const headers = {
        Authorization: `Bearer ${accessToken}`
    };

    // Initialize video metadata and time updates
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

    // Play/Pause toggle
    const handlePlayPause = () => {
        const video = videoRef.current!;
        if (playing) {
            video.pause();
        } else {
            video.play();
        }
        setPlaying(!playing);
    };

    // Seek on timeline click
    const handleTimelineClick = (e: React.MouseEvent<HTMLDivElement>) => {
        const rect = e.currentTarget.getBoundingClientRect();
        const clickX = e.clientX - rect.left;
        const timelineWidth = rect.width;
        const seekTime = (clickX / timelineWidth) * duration;
        videoRef.current!.currentTime = seekTime;
    };

    // Open feedback form and fetch AI analysis
    const handleGiveFeedback = async () => {
        setShowFeedbackForm(true);
        try {
            const response = await axios.post(
                `/vid/api/video/${videoId}/feedback`,
                { timestamp: currentTime },
                { headers }
            );
            setAiInsights(response.data.insights || 'No insights available');
        } catch (error) {
            console.error('Error fetching AI analysis:', error);
            setAiInsights('Error fetching AI insights');
        }
    };

    // Submit feedback
    const handleSubmitFeedback = () => {
        if (feedbackText.trim()) {
            onAddFeedback({
                id: Date.now().toString(), // Temporary ID; backend should assign a real one
                timestamp: currentTime,
                feedback: feedbackText,
                aiInsights,
            });
            setFeedbackText('');
            setShowFeedbackForm(false);
        }
    };

    return (
        <div className="video-container">
            <video ref={videoRef} src={videoUrl} style={{ width: '100%' }} />
            <div className="controls">
                <button onClick={handlePlayPause}>{playing ? 'Pause' : 'Play'}</button>
                <div
                    className="timeline"
                    onClick={handleTimelineClick}
                    style={{ height: '20px', background: '#ddd', position: 'relative' }}
                >
                    <div
                        className="progress"
                        style={{ width: `${(currentTime / duration) * 100}%`, height: '100%', background: '#4CAF50' }}
                    />
                    {feedbackList.map((feedback) => (
                        <div
                            key={feedback.id}
                            className="marker"
                            style={{
                                position: 'absolute',
                                left: `${(feedback.timestamp / duration) * 100}%`,
                                width: '5px',
                                height: '20px',
                                background: '#f00',
                                cursor: 'pointer',
                            }}
                            onClick={() => (videoRef.current!.currentTime = feedback.timestamp)}
                            title={feedback.feedback}
                        />
                    ))}
                </div>
                <button onClick={handleGiveFeedback}>Give Feedback</button>
            </div>
            {showFeedbackForm && (
                <div className="feedback-form">
                    <h3>Give Feedback</h3>
                    <p>Timestamp: {currentTime.toFixed(2)}s</p>
                    <p>AI Insights: {aiInsights}</p>
                    <textarea
                        placeholder="Enter feedback..."
                        value={feedbackText}
                        onChange={(e) => setFeedbackText(e.target.value)}
                    />
                    <button onClick={handleSubmitFeedback}>Save</button>
                    <button onClick={() => setShowFeedbackForm(false)}>Cancel</button>
                </div>
            )}
        </div>
    );
};

export default VideoPlayer;
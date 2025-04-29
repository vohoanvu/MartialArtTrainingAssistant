import React, { useRef, useState, useEffect } from 'react';
import axios from 'axios';
import useAuthStore from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Feedback } from '@/pages/VideoReview';

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
    const [feedbackType, setFeedbackType] = useState('Posture');
    const { accessToken } = useAuthStore();
    const headers = {
        Authorization: `Bearer ${accessToken}`
    };

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
        const rect = e.currentTarget.getBoundingClientRect();
        const clickX = e.clientX - rect.left;
        const timelineWidth = rect.width;
        const seekTime = (clickX / timelineWidth) * duration;
        videoRef.current!.currentTime = seekTime;
    };

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

    const handleSubmitFeedback = () => {
        if (feedbackText.trim()) {
            onAddFeedback({
                id: Date.now().toString(),
                timestamp: currentTime,
                feedback: feedbackText,
                feedbackType: 'human',
                aiInsights,
            });
            setFeedbackText('');
            setShowFeedbackForm(false);
        }
    };

    const handleRightClick = (e: React.MouseEvent<HTMLDivElement>) => {
        e.preventDefault();
        const rect = e.currentTarget.getBoundingClientRect();
        const clickX = e.clientX - rect.left;
        const timelineWidth = rect.width;
        const seekTime = (clickX / timelineWidth) * duration;
        videoRef.current!.currentTime = seekTime;
        setCurrentTime(seekTime);
        handleGiveFeedback();
    };

    return (
        <div className="video-container">
            <div style={{ overflow: 'hidden' }}>
                <video ref={videoRef} src={videoUrl} controls className='w-full h-full' style={{ objectFit: 'contain' }}/> 
            </div>
            <div className="timeline-container mt-2">
                <div
                    className="timeline w-full h-6 bg-gray-300 relative rounded-md cursor-pointer"
                    onClick={handleTimelineClick}
                    onContextMenu={handleRightClick}
                >
                    <div
                        className="progress absolute h-full bg-green-500 rounded-md"
                        style={{ width: `${(currentTime / duration) * 100}%` }}
                    />
                    {feedbackList.map((feedback) => (
                        <div
                            key={feedback.id}
                            className={`marker absolute top-0 h-full cursor-pointer ${feedback.feedbackType === 'human' ? 'bg-blue-500' : 'bg-green-500'}`}
                            style={{
                                left: `${(feedback.timestamp / duration) * 100}%`,
                                width: '5px',
                            }}
                            onClick={() => (videoRef.current!.currentTime = feedback.timestamp)}
                            title={`${feedback.feedbackType === 'human' ? 'Human' : 'AI'} Feedback: ${feedback.feedback.substring(0, 50)}...`}
                        />
                    ))}
                </div>
            </div>
            <div className="controls flex items-center justify-between p-4 mt-2">
                <Button onClick={handlePlayPause}>{playing ? 'Pause' : 'Play'}</Button>
                <Button onClick={() => videoRef.current!.playbackRate = 0.5}>Slow Motion (0.5x)</Button>
                <Button onClick={() => videoRef.current!.playbackRate = 1.0}>Normal Speed (1.0x)</Button>
                <Button onClick={() => videoRef.current!.playbackRate = 2.0}>Fast Forward (2.0x)</Button>
                <Button onClick={() => videoRef.current!.currentTime -= 0.04}>Frame Back</Button>
                <Button onClick={() => videoRef.current!.currentTime += 0.04}>Frame Forward</Button>
                <Button onClick={handleGiveFeedback}>Give Feedback</Button>
            </div>
            {showFeedbackForm && (
                <div className="feedback-form p-4 border rounded-md shadow-md mt-4">
                    <h3 className="text-lg font-semibold mb-2">Give Feedback</h3>
                    <p className="text-sm text-gray-600">Timestamp: {currentTime.toFixed(2)}s</p>
                    <p className="text-sm text-gray-600">AI Insights: {aiInsights || 'No AI insights available'}</p>
                    <select
                        value={feedbackType}
                        onChange={(e) => setFeedbackType(e.target.value)}
                        className="mb-2 p-2 border rounded-md"
                    >
                        <option value="Posture">Posture</option>
                        <option value="Technique Execution">Technique Execution</option>
                        <option value="Defense">Defense</option>
                        <option value="Movement Efficiency">Movement Efficiency</option>
                    </select>
                    <textarea
                        className="w-full h-24 p-2 border rounded-md mb-2"
                        placeholder="Enter feedback..."
                        value={feedbackText}
                        onChange={(e) => setFeedbackText(e.target.value)}
                    />
                    <div className="flex justify-end">
                        <Button onClick={() => {
                            handleSubmitFeedback();
                            setFeedbackText('');
                            setFeedbackType('Posture');
                        }}>Save and Add Another</Button>
                        <Button variant="secondary" onClick={handleSubmitFeedback}>Save</Button>
                        <Button variant="ghost" onClick={() => setShowFeedbackForm(false)}>Cancel</Button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default VideoPlayer;
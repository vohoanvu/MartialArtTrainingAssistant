import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import axios from 'axios';
import VideoPlayer from '../components/VideoPlayer';
import FeedbackList from '../components/FeedbackList';
import useAuthStore from '@/store/authStore';

export interface Feedback {
    id: string;
    timestamp: number;
    feedback: string;
    aiInsights?: string;
}

const VideoReview: React.FC = () => {
    const { videoId } = useParams<{ videoId: string }>();
    const [feedbackList, setFeedbackList] = useState<Feedback[]>([]);
    const [videoUrl, setVideoUrl] = useState('');
    const { accessToken } = useAuthStore();
    const headers = {
        Authorization: `Bearer ${accessToken}`
    };

    // Fetch video details and feedback on mount
    useEffect(() => {
        if (!videoId) return;
        const fetchData = async () => {
            try {
                const videoResponse = await axios.get(`/vid/api/video/${videoId}`, { headers });
                setVideoUrl(videoResponse.data.url);
                const feedbackResponse = await axios.get(`/vid/api/video/${videoId}/feedback`, { headers });
                // If feedbackResponse.data has the structure { HumanFeedback, AiFeedback },
                // you can combine them as needed. For this example we assume HumanFeedback feedback.
                const humanFeedback = Array.isArray(feedbackResponse.data.HumanFeedback)
                    ? feedbackResponse.data.HumanFeedback
                    : [];
                setFeedbackList(humanFeedback);
            } catch (error) {
                console.error('Error fetching data:', error);
            }
        };
        fetchData();
    }, [videoId]);

    // Handle new feedback submission
    const handleAddFeedback = async (newFeedback: Feedback) => {
        try {
            const response = await axios.post(`/vid/api/video/${videoId}/feedback`, newFeedback, { headers });
            setFeedbackList([...feedbackList, response.data]);
        } catch (error) {
            console.error('Error saving feedback:', error);
        }
    };

    // Handle seeking to a timestamp
    const handleSeek = (timestamp: number) => {
        // TODO: This requires communication with VideoPlayer, possibly via ref or context
        console.log(`Seeking to ${timestamp}`);
    };

    return (
        <div style={{ display: 'flex' }}>
            <VideoPlayer
                videoUrl={videoUrl}
                videoId={videoId || ""}
                feedbackList={feedbackList}
                onAddFeedback={handleAddFeedback}
            />
            <div className="feedback-panel" style={{ flex: 1, marginLeft: '20px' }}>
                <FeedbackList feedbackList={feedbackList} onSeek={handleSeek} />
            </div>
        </div>
    );
};

export default VideoReview;
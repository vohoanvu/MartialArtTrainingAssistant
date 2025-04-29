import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getVideoDetails, getVideoFeedback, addVideoFeedback } from '@/services/api';
import VideoPlayer from '../components/VideoPlayer';
import FeedbackList from '../components/FeedbackList';
import useAuthStore from '@/store/authStore';
import FeedbackForm from '@/components/FeedbackForm';

export interface Feedback {
    id: string;
    timestamp: number;
    feedback: string;
    feedbackType: string;
    aiInsights?: string;
}

const VideoReview: React.FC = () => {
    const { videoId } = useParams<{ videoId: string }>();
    const [feedbackList, setFeedbackList] = useState<Feedback[]>([]);
    const [videoUrl, setVideoUrl] = useState('');
    const { accessToken, refreshToken, hydrate } = useAuthStore();
    const [aiFeedbackList, setAiFeedbackList] = useState<Feedback[]>([]);
    const [fromTimestamp, setFromTimestamp] = useState('');
    const [toTimestamp, setToTimestamp] = useState('');
    const [feedbackText, setFeedbackText] = useState('');
    const [category, setCategory] = useState('');

    useEffect(() => {
        if (!videoId) return;

        const fetchData = async () => {
            try {
                const videoDetails = await getVideoDetails({
                    videoId,
                    jwtToken: accessToken,
                    refreshToken,
                    hydrate,
                });
                setVideoUrl(videoDetails.signedUrl);

                const feedbackData = await getVideoFeedback({
                    videoId,
                    jwtToken: accessToken,
                    refreshToken,
                    hydrate,
                });
                setFeedbackList(feedbackData.humanFeedback || []);
                setAiFeedbackList(feedbackData.aiFeedback || []);
            } catch (error) {
                console.error('Error fetching data:', error);
            }
        };

        fetchData();
    }, [videoId, accessToken, refreshToken, hydrate]);

    const handleAddFeedback = async () => {
        if (!fromTimestamp || !toTimestamp || !feedbackText) return;

        const newFeedback: Feedback = {
            id: Date.now().toString(),
            timestamp: parseFloat(fromTimestamp),
            feedback: feedbackText,
            feedbackType: 'human',
        };

        try {
            const addedFeedback = await addVideoFeedback({
                videoId: videoId || '',
                feedback: newFeedback,
                jwtToken: accessToken,
                refreshToken,
                hydrate,
            });
            setFeedbackList([...feedbackList, addedFeedback]);
            setFromTimestamp('');
            setToTimestamp('');
            setFeedbackText('');
            setCategory('');
        } catch (error) {
            console.error('Error saving feedback:', error);
        }
    };

    const handleSeek = (timestamp: number) => {
        console.log(`Seeking to ${timestamp}`);
    };

    return (
        <div className="flex flex-col md:flex-row gap-4">
            <div className="flex-1">
                <VideoPlayer
                    videoUrl={videoUrl}
                    videoId={videoId || ""}
                    feedbackList={feedbackList}
                    onAddFeedback={(feedback) => setFeedbackList([...feedbackList, feedback])}
                />
            </div>
            <div className="w-full md:w-1/3">
                <FeedbackForm
                    videoId={parseInt(videoId || "0", 10)}
                    timestamp={parseFloat(fromTimestamp) || 0}
                    fromTimestamp={fromTimestamp}
                    toTimestamp={toTimestamp}
                    feedbackText={feedbackText}
                    category={category}
                    onSave={handleAddFeedback}
                    onCancel={() => {
                        setFromTimestamp('');
                        setToTimestamp('');
                        setFeedbackText('');
                        setCategory('');
                    }}
                    setFromTimestamp={setFromTimestamp}
                    setToTimestamp={setToTimestamp}
                    setFeedbackText={setFeedbackText}
                    setCategory={setCategory}
                />
                <FeedbackList feedbackList={feedbackList || aiFeedbackList} onSeek={handleSeek} />
            </div>
        </div>
    );
};

export default VideoReview;
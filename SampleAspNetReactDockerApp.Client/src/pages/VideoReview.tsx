import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getVideoDetails, getVideoFeedback, addVideoFeedback } from '@/services/api';
import VideoPlayer from '../components/VideoPlayer';
import FeedbackList from '../components/FeedbackList';
import useAuthStore from '@/store/authStore';
import FeedbackForm from '@/components/FeedbackForm';
import { Button } from '@/components/ui/button';

export interface Feedback {
    id: string;
    fromTimestamp: number;
    toTimestamp: number;
    feedback: string;
    feedbackType: string;
    aiInsights?: string;
    isPending?: boolean;
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
    const [selectedSegment, setSelectedSegment] = useState<{ from: number; to: number } | null>(null);

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
                setFeedbackList(feedbackData.humanFeedback.map(fb => ({ ...fb, isPending: false })) || []);
                setAiFeedbackList(feedbackData.aiFeedback.map(fb => ({ ...fb, isPending: false })) || []);
            } catch (error) {
                console.error('Error fetching data:', error);
            }
        };

        fetchData();
    }, [videoId, accessToken, refreshToken, hydrate]);

    const handleAddFeedback = () => {
        if (!fromTimestamp || !toTimestamp || !feedbackText) return;

        const newFeedback: Feedback = {
            id: Date.now().toString(),
            fromTimestamp: parseFloat(fromTimestamp),
            toTimestamp: parseFloat(toTimestamp),
            feedback: feedbackText,
            feedbackType: 'human',
            isPending: true,
        };

        setFeedbackList([newFeedback, ...feedbackList]);
        setFromTimestamp('');
        setToTimestamp('');
        setFeedbackText('');
        setCategory('');
        setSelectedSegment(null); // Clear selection after saving
    };

    const handleSubmitFeedback = async () => {
        const pendingFeedback = feedbackList.filter(fb => fb.isPending);
        try {
            const savedFeedbackPromises = pendingFeedback.map(fb => addVideoFeedback({
                videoId: videoId || '',
                feedback: { ...fb, id: undefined },
                jwtToken: accessToken,
                refreshToken,
                hydrate,
            }));
            const savedFeedback = await Promise.all(savedFeedbackPromises);
            setFeedbackList(
                [
                    ...feedbackList.filter((fb: Feedback) => !fb.isPending),
                    ...savedFeedback.map((fb: Feedback) => ({ ...fb, isPending: false }))
                ]
            );
        } catch (error) {
            console.error('Error submitting feedback:', error);
        }
    };

    const handleSeek = (timestamp: number) => {
        console.log(`Seeking to ${timestamp}`);
    };

    const clearSelection = () => {
        setSelectedSegment(null);
        setFromTimestamp('');
        setToTimestamp('');
        setFeedbackText('');
        setCategory('');
    };

    return (
        <div className="flex flex-col md:flex-row gap-4">
            <div className="flex-1">
                <VideoPlayer
                    videoUrl={videoUrl}
                    videoId={parseInt(videoId || "0", 10)}
                    feedbackList={feedbackList}
                    onAddFeedback={(feedback) => setFeedbackList([feedback, ...feedbackList])}
                    setFromTimestamp={setFromTimestamp}
                    setToTimestamp={setToTimestamp}
                    selectedSegment={selectedSegment}
                    setSelectedSegment={setSelectedSegment}
                    clearSelection={clearSelection}
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
                    onCancel={clearSelection}
                    setFromTimestamp={setFromTimestamp}
                    setToTimestamp={setToTimestamp}
                    setFeedbackText={setFeedbackText}
                    setCategory={setCategory}
                />
                <FeedbackList feedbackList={feedbackList || aiFeedbackList} onSeek={handleSeek} />
                <Button onClick={handleSubmitFeedback} className="mt-2">Submit Feedback</Button>
            </div>
        </div>
    );
};

export default VideoReview;
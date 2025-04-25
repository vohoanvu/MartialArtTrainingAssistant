import { useState, useEffect, useCallback } from 'react';
import axios, { AxiosError } from 'axios';
import VideoPlayer from '../components/VideoPlayer';
import FeedbackForm from '../components/FeedbackForm';
import FeedbackList from '../components/FeedbackList';
import AIInsights, { Feedback } from '../components/AiInsights';
import { useParams } from 'react-router-dom';
import useAuthStore from '@/store/authStore';
import { UploadedVideoDto } from './VideoStorageListing';
import AiAnalysisResults from '@/components/AiAnalysisResults';

interface FeedbackResponse {
    humanFeedback: Feedback[];
    aiFeedback: Feedback[];
}

interface TechniqueReviewProps {
    videoId: number;
}

const VideoReviewWrapper = () => {
    const { videoId } = useParams<{ videoId: string }>();
    const id = Number(videoId);
    return <VideoReview videoId={id} />;
};

export default VideoReviewWrapper;

const VideoReview: React.FC<TechniqueReviewProps> = ({ videoId }) => {
    const [uploadedVideo, setLoadedVideo] = useState<UploadedVideoDto>();
    const [videoUri, setVideoUri] = useState<string>('');
    const [feedback, setFeedback] = useState<Feedback[]>([]);
    const [aiFeedback, setAiFeedback] = useState<Feedback[]>([]);
    const [showForm, setShowForm] = useState<boolean>(false);
    const [currentTime, setCurrentTime] = useState<number>(0);
    const [isPaused, setIsPaused] = useState<boolean>(false);
    const [seekTo, setSeekTo] = useState<number | null>(null);
    const [error, setError] = useState<string | null>(null);
    const { accessToken } = useAuthStore();

    const authHeaders = {
        headers: {
            Authorization: `Bearer ${accessToken}`,
        },
    };

    // Fetch video and feedback data using UploadedVideoDto as the response interface.
    useEffect(() => {
        const fetchData = async () => {
            try {
                // The response will be of type UploadedVideoDto.
                const videoResponse = await axios.get<UploadedVideoDto>(`/vid/api/video/${videoId}`, 
                    authHeaders);
                if (videoResponse.status !== 200) {
                    setError('Failed to fetch video');
                }
                // Use the filePath property as the video URL for the player.
                console.log("Get UploadedVideo Response ", videoResponse);
                setLoadedVideo(videoResponse.data);
                setVideoUri(videoResponse.data.signedUrl);
                const feedbackResponse = await axios.get<FeedbackResponse>(
                    `/vid/api/video/${videoId}/feedback`, 
                    authHeaders
                );
                setFeedback(feedbackResponse.data.humanFeedback);
                setAiFeedback(feedbackResponse.data.aiFeedback);
                setError(null);
                console.log("Video Uri ", videoUri);
            } catch (err) {
                const errorMessage =
                    err instanceof AxiosError
                        ? err.response?.data?.message || 'Failed to fetch data'
                        : 'An unexpected error occurred';
                setError(errorMessage);
            }
        };

        fetchData();
    }, [videoId, accessToken]);

    useEffect(() => {
        console.log("Updated videoUri:", videoUri);
    }, [videoUri]);

    // Handle saving new feedback
    const handleSaveFeedback = useCallback(
        async (newFeedback: Omit<Feedback, 'id'>) => {
            try {
                const response = await axios.post<Feedback>(
                    `/vid/api/video/${videoId}/feedback`,
                    newFeedback,
                    authHeaders,
                );
                setFeedback([...feedback, response.data]);
                setShowForm(false);
                setError(null);
            } catch (err) {
                const errorMessage =
                    err instanceof AxiosError
                        ? err.response?.data?.message || 'Failed to save feedback'
                        : 'An unexpected error occurred';
                setError(errorMessage);
            }
        },
        [videoId, feedback, accessToken],
    );

    // Handle feedback selection (seek to timestamp)
    const handleSelectFeedback = useCallback((time: number) => {
        setSeekTo(time);
    }, []);

    // Handle video time updates
    const handleTimeUpdate = useCallback((time: number) => {
        setCurrentTime(time);
    }, []);

    // Handle video pause (enable feedback form)
    const handlePause = useCallback(() => {
        setIsPaused(true);
        setShowForm(true);
    }, []);

    return (
        <div className="p-5">
            {error && <div className="text-red-500 mb-4">{error}</div>}
            {
                videoUri && (
                    <VideoPlayer
                        videoUri={videoUri}
                        feedback={[...feedback, ...aiFeedback]}
                        onTimeUpdate={handleTimeUpdate}
                        onPause={handlePause}
                        seekTo={seekTo}
                    />)
            }
            <div className="mt-5">
                <button
                    onClick={() => setShowForm(true)}
                    disabled={!isPaused}
                    className={`px-4 py-2 rounded text-white ${
                        isPaused ? 'bg-blue-600 hover:bg-blue-700' : 'bg-gray-400 cursor-not-allowed'
                    }`}
                >
                    Give Feedback
                </button>
            </div>
            {showForm && (
                <FeedbackForm
                    videoId={videoId}
                    timestamp={currentTime}
                    onSave={handleSaveFeedback}
                    onCancel={() => setShowForm(false)}
                />
            )}
            <div className="mt-5">
                <FeedbackList feedback={feedback} onSelect={handleSelectFeedback} />
                <AIInsights
                    aiFeedback={aiFeedback}
                    onIncorporate={(f) => setFeedback([...feedback, f])}
                />
            </div>
            <div className="mt-5">
                <AiAnalysisResults analysisJson={uploadedVideo?.aiAnalysisResult || ""} />
            </div>
        </div>
    );
};
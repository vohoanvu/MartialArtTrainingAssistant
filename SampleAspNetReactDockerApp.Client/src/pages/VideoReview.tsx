import { useState, useEffect, useCallback } from 'react';
import axios, { AxiosError } from 'axios';
import VideoContainer from '../components/VideoPlayer';
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
    const [error, setError] = useState<string | null>(null);
    const { accessToken } = useAuthStore();

    const authHeaders = {
        headers: {
            Authorization: `Bearer ${accessToken}`,
        },
    };

    useEffect(() => {
        const fetchData = async () => {
            try {
                const videoResponse = await axios.get<UploadedVideoDto>(
                    `/vid/api/video/${videoId}`,
                    authHeaders
                );
                if (videoResponse.status !== 200) {
                    setError('Failed to fetch video');
                    return;
                }
                setLoadedVideo(videoResponse.data);
                setVideoUri(videoResponse.data.signedUrl);

                const feedbackResponse = await axios.get<FeedbackResponse>(
                    `/vid/api/video/${videoId}/feedback`,
                    authHeaders
                );
                setFeedback(feedbackResponse.data.humanFeedback);
                setAiFeedback(feedbackResponse.data.aiFeedback);
                setError(null);
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

    const handleSaveFeedback = useCallback(
        async (newFeedback: Omit<Feedback, 'id'>) => {
            try {
                const response = await axios.post<Feedback>(
                    `/vid/api/video/${videoId}/feedback`,
                    newFeedback,
                    authHeaders
                );
                setFeedback([...feedback, response.data]);
                setShowForm(false);
            } catch (err) {
                const errorMessage =
                    err instanceof AxiosError
                        ? err.response?.data?.message || 'Failed to save feedback'
                        : 'An unexpected error occurred';
                console.error(errorMessage);
            }
        },
        [videoId, feedback, accessToken]
    );

    const handleSelectFeedback = useCallback((time: number) => {
        setCurrentTime(time);
    }, []);

    return (
        <div className="p-5 flex flex-col md:flex-row space-y-5 md:space-y-0 md:space-x-5">
            {error && <div className="text-red-500 mb-4">{error}</div>}
            <div className="flex-2 w-full min-h-[500px]">
                {videoUri ? (
                    <VideoContainer
                        videoUri={videoUri}
                        feedback={[...feedback, ...aiFeedback]}
                        key={videoId} // Stable key to prevent unmounting
                    />
                ) : (
                    <p>Loading video...</p>
                )}
            </div>
            {videoUri && (
                <>
                    <div className="mt-5">
                        <button
                            onClick={() => setShowForm(true)}
                            disabled={!isPaused}
                            className={`px-4 py-2 rounded text-white ${isPaused ? 'bg-blue-600 hover:bg-blue-700' : 'bg-gray-400 cursor-not-allowed'
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
                    <div className="flex-1 w-full">
                        <FeedbackList feedback={feedback} onSelect={handleSelectFeedback} />
                        <AIInsights
                            aiFeedback={aiFeedback}
                            onIncorporate={(f) => setFeedback([...feedback, f])}
                        />
                        <div className="mt-5">
                            <AiAnalysisResults analysisJson={uploadedVideo?.aiAnalysisResult || ''} />
                        </div>
                    </div>
                </>
            )}
        </div>
    );
};
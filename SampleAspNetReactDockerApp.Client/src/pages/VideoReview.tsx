import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getVideoDetails, getVideoFeedback, getFighterDetails } from '@/services/api';
import VideoPlayer from '../components/VideoPlayer';
import useAuthStore from '@/store/authStore';
import { AiAnalysisResultDto, Fighter } from '@/types/global';
import { StudentDetails } from '@/components/StudentFighterDetails';
import TechniqueFeedback from '@/components/TechniqueFeedback';
//import TimeSegmentSelection from '@/components/TimeSegmentSelection';

const VideoReview: React.FC = () => {
    const { videoId } = useParams<{ videoId: string }>();
    const [feedbackList, setFeedbackList] = useState<AiAnalysisResultDto | null>(null);
    const [videoUrl, setVideoUrl] = useState('');
    const { accessToken, refreshToken, hydrate } = useAuthStore();

    // const [fromTimestamp, setFromTimestamp] = useState('');
    // const [toTimestamp, setToTimestamp] = useState('');

    const [selectedSegment, setSelectedSegment] = useState<{ from: number; to: number } | null>(null);
    const [fighterDetails, setFighterDetails] = useState<Fighter | null>(null);

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
                console.log('Video Details:', videoDetails);
                setVideoUrl(videoDetails.signedUrl);

                const feedbackData : AiAnalysisResultDto = await getVideoFeedback({
                    videoId,
                    jwtToken: accessToken,
                    refreshToken,
                    hydrate,
                });
                setFeedbackList(feedbackData);
                console.log('AiAnalysisResultDto data:', feedbackData);

                const fighterDetails = await getFighterDetails({
                    fighterId: videoDetails.fighterId,
                    jwtToken: accessToken,
                    refreshToken,
                    hydrate,
                });
                setFighterDetails(fighterDetails);
            } catch (error) {
                console.error('Error fetching data:', error);
            }
        };

        fetchData();
    }, [videoId, accessToken, refreshToken, hydrate]);

    // const handleAddFeedback = () => {
    //     if (!fromTimestamp || !toTimestamp) return;

    //     const newFeedback: Feedback = {
    //         id: Date.now().toString(),
    //         fromTimestamp: parseFloat(fromTimestamp),
    //         toTimestamp: parseFloat(toTimestamp),
    //     };

    //     setFeedbackList([newFeedback, ...feedbackList]);
    //     setFromTimestamp('');
    //     setToTimestamp('');
    //     setSelectedSegment(null); // Clear selection after saving
    // };

    // const handleSubmitFeedback = async () => {
    //     const pendingFeedback = feedbackList.filter(fb => fb.isPending);
    //     try {
    //         const savedFeedbackPromises = pendingFeedback.map(fb => addVideoFeedback({
    //             videoId: videoId || '',
    //             feedback: { ...fb, id: undefined },
    //             jwtToken: accessToken,
    //             refreshToken,
    //             hydrate,
    //         }));
    //         const savedFeedback = await Promise.all(savedFeedbackPromises);
    //         setFeedbackList(
    //             [
    //                 ...feedbackList.filter((fb: Feedback) => !fb.isPending),
    //                 ...savedFeedback.map((fb: Feedback) => ({ ...fb, isPending: false }))
    //             ]
    //         );
    //     } catch (error) {
    //         console.error('Error submitting feedback:', error);
    //     }
    // };

    const handleSeek = (timestamp: number) => {
        console.log(`Seeking to ${timestamp}`);
    };

    const clearSelection = () => {
        setSelectedSegment(null);
        // setFromTimestamp('');
        // setToTimestamp('');
    };

    return (
        <div className="flex flex-col md:flex-row gap-4">
            <div className="flex-2">
                {/* <TimeSegmentSelection
                    videoId={parseInt(videoId || "0", 10)}
                    fromTimestamp={fromTimestamp}
                    toTimestamp={toTimestamp}
                    onSave={() => console.log('Save clicked')}
                    onCancel={clearSelection}
                    setFromTimestamp={setFromTimestamp}
                    setToTimestamp={setToTimestamp}
                /> */}
                <VideoPlayer
                    videoUrl={videoUrl}
                    videoId={videoId || "0"}
                    identifiedTechniques={feedbackList?.techniques_identified  || []}
                    setFromTimestamp={() => console.log('Set START timestamp')}
                    setToTimestamp={() => console.log('Set END timestamp')}
                    selectedSegment={selectedSegment}
                    setSelectedSegment={setSelectedSegment}
                    clearSelection={clearSelection}
                />
                <StudentDetails fighterDetails={fighterDetails}/>
            </div>
            <div className="w-full md:w-2/3">
                <TechniqueFeedback
                    feedbackData={feedbackList}
                    onSeek={handleSeek}
                />
            </div>
        </div>
    );
};

export default VideoReview;
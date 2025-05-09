import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getVideoDetails, getVideoFeedback, getFighterDetails, saveVideoAnalysisResult } from '@/services/api';
import VideoPlayer from '../components/VideoAnalysisEditor/VideoPlayer';
import useAuthStore from '@/store/authStore';
import { AnalysisResultDto, Fighter } from '@/types/global';
import { StudentDetails } from '@/components/VideoAnalysisEditor/StudentFighterDetails';
import TechniqueFeedback from '@/components/VideoAnalysisEditor/TechniqueFeedback';

const VideoReview: React.FC = () => {
    const { videoId } = useParams<{ videoId: string }>();
    const [feedbackList, setFeedbackList] = useState<AnalysisResultDto | null>(null);
    const [videoUrl, setVideoUrl] = useState('');
    const { accessToken, refreshToken, hydrate } = useAuthStore();

    const [selectedSegment, setSelectedSegment] = useState<{ start: string; end: string } | null>(null);
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

                const feedbackData: AnalysisResultDto = await getVideoFeedback({
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

    const handleInputChange = (
        section: string,
        index: string | number,
        field: string | number,
        value: any
    ) => {
        const updatedAnalysis = { ...feedbackList } as any;
        if (section === 'overallAnalysis') {
            updatedAnalysis[section][field] = value;
        } else {
            updatedAnalysis[section][index][field] = value;
        }
        setFeedbackList(updatedAnalysis);
    };

    const handleSaveAnalysisResult = async (updatedFeedbackData: AnalysisResultDto) => {
        if (!updatedFeedbackData || !videoId) {
            console.error('Missing videoId or AnalysisResultDto for saving.');
            return;
        }

        try {
            const updatedAnalysisResult = await saveVideoAnalysisResult({
                videoId,
                analysisResultBody: updatedFeedbackData,
                jwtToken: accessToken,
                refreshToken,
                hydrate,
            });

            setFeedbackList(updatedAnalysisResult);
            alert('Changes saved successfully!');
        } catch (error: any) {
            console.error('Save operation failed:', error);
            alert(`Error saving changes: ${error.message || 'Unknown error'}`);
        }
    };

    const handleSeek = (timestamp: number) => {
        console.log(`Seeking to ${timestamp}`);
    };

    const clearSelection = () => {
        setSelectedSegment(null);
    };

    return (
        <div className="flex flex-col md:flex-row gap-4">
            <div className="flex-1">
                <VideoPlayer
                    videoUrl={videoUrl}
                    videoId={videoId || '0'}
                    identifiedTechniques={feedbackList?.techniques || []}
                    selectedSegment={selectedSegment}
                    setSelectedSegment={setSelectedSegment}
                    clearSelection={clearSelection}
                    onSegmentSelect={(start, end) => setSelectedSegment({ start, end })}
                />
                <StudentDetails fighterDetails={fighterDetails} />
            </div>
            <div className="flex-2">
                <TechniqueFeedback
                    feedbackData={feedbackList}
                    onSeek={handleSeek}
                    saveChanges={handleSaveAnalysisResult}
                    onInputChange={handleInputChange}
                    selectedSegment={selectedSegment}
                />
            </div>
        </div>
    );
};

export default VideoReview;
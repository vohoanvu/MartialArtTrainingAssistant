import React, { useState, useEffect, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { getFighterDetails, getVideoDetails, getVideoFeedback, saveVideoAnalysisResult } from '@/services/api';
import VideoPlayer, { VideoPlayerHandle } from '../components/VideoAnalysisEditor/VideoPlayer';
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
    const videoPlayerRef = useRef<VideoPlayerHandle>(null);
    const [studentIdentifier, setStudentIdentifier] = useState<string | null>(null);
    const [isAnalysisSaving , setIsAnalysisSaving] = useState(false);

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
                setStudentIdentifier(videoDetails.studentIdentifier);

                const feedbackData: AnalysisResultDto = await getVideoFeedback({
                    videoId,
                    jwtToken: accessToken,
                    refreshToken,
                    hydrate,
                });
                setFeedbackList(feedbackData);
                //console.log('AiAnalysisResultDto data:', feedbackData);

                const fighterDetails = await getFighterDetails({
                    fighterId: videoDetails.fighterId,
                    jwtToken: accessToken,
                    refreshToken,
                    hydrate,
                })
                setFighterDetails(fighterDetails ?? null);
                console.log("Video details: ", videoDetails);
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

    const handleSaveAnalysisResult = async (partialFeedbackData: AnalysisResultDto) => {
        if (!partialFeedbackData || !videoId) {
            console.error('Missing videoId or PartialAnalysisResultDto for saving.');
            return;
        }

        try {
            setIsAnalysisSaving(true);
            const updatedAnalysisResult = await saveVideoAnalysisResult({
                videoId,
                analysisResultBody: partialFeedbackData,
                jwtToken: accessToken,
                refreshToken,
                hydrate,
            });

            setFeedbackList(updatedAnalysisResult);
            alert('Changes saved successfully!');
        } catch (error: any) {
            console.error('Save operation failed:', error);
            alert(`Error saving changes: ${error.message || 'Unknown error'}`);
        } finally {
            setIsAnalysisSaving(false);
        }
    };

    const handleSeek = (timestamp: string) => {
        if (videoPlayerRef.current) {
            videoPlayerRef.current.seekTo(timestamp);
        }
    };

    const clearSelection = () => {
        setSelectedSegment(null);
    };

    return (
        <div className="w-full">
            <h1 className="text-3xl font-bold text-center mt-3">Review and edit this tape</h1>
            <div className="flex flex-col md:flex-row gap-4 md:p-4 m-0 md:m-2">
                <div className="w-full md:w-1/2 flex flex-col gap-4">
                    <div className="rounded-lg shadow bg-background border border-border p-2 md:p-4">
                        <VideoPlayer
                            ref={videoPlayerRef}
                            videoUrl={videoUrl}
                            videoId={videoId || '0'}
                            identifiedTechniques={feedbackList?.techniques || []}
                            selectedSegment={selectedSegment}
                            setSelectedSegment={setSelectedSegment}
                            clearSelection={clearSelection}
                            onSegmentSelect={(start, end) => setSelectedSegment({ start, end })}
                        />
                    </div>
                    <div className="rounded-lg shadow bg-background border border-border p-2 md:p-4">
                        <StudentDetails fighterDetails={fighterDetails} studentIdentifier={studentIdentifier} />
                    </div>
                </div>
                <div className="w-full md:w-1/2 mt-4 md:mt-0">
                    <div className="rounded-lg shadow bg-background border border-border p-2 md:p-4 h-full">
                        <TechniqueFeedback
                            feedbackData={feedbackList}
                            onSeek={handleSeek}
                            saveChanges={handleSaveAnalysisResult}
                            onInputChange={handleInputChange}
                            selectedSegment={selectedSegment}
                            isAnalysisSaving={isAnalysisSaving}
                        />
                    </div>
                </div>
            </div>
        </div>
    );
};

export default VideoReview;
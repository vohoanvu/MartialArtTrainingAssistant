import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
    getFighterDetails, getVideoDetails, getVideoFeedback, saveVideoAnalysisResult,
    getVideoAnalysisV2, saveVideoAnalysisV2, requestTranscode,
} from '@/services/api';
import VideoPlayer, { VideoPlayerHandle, EventMarker } from '../components/VideoAnalysisEditor/VideoPlayer';
import useAuthStore from '@/store/authStore';
import { AnalysisResultDto, AnalysisV2Dto, Fighter } from '@/types/global';
import { StudentDetails } from '@/components/VideoAnalysisEditor/StudentFighterDetails';
import TechniqueFeedback from '@/components/VideoAnalysisEditor/TechniqueFeedback';
import AnalysisV2Panel from '@/components/VideoAnalysisEditor/v2/AnalysisV2Panel';
import { actorMarkerColor, techniqueCategoryKey } from '@/components/VideoAnalysisEditor/v2/enumLabels';
import { analysisConnection } from '@/services/SignalRService';
import { useToast } from '@/hooks/use-toast';

const V2_ENABLED = import.meta.env.VITE_ANALYSIS_V2_ENABLED !== 'false';

const VideoReview: React.FC = () => {
    const { videoId } = useParams<{ videoId: string }>();
    const { t } = useTranslation();
    const { toast } = useToast();
    const [feedbackList, setFeedbackList] = useState<AnalysisResultDto | null>(null);
    const [analysisV2, setAnalysisV2] = useState<AnalysisV2Dto | null>(null);
    const [videoUrl, setVideoUrl] = useState('');
    const [transcodeStatus, setTranscodeStatus] = useState<string>('None');
    const [analysisPhase, setAnalysisPhase] = useState<string | null>(null);
    const { accessToken, refreshToken, hydrate } = useAuthStore();

    const [selectedSegment, setSelectedSegment] = useState<{ start: string; end: string } | null>(null);
    const [selectedSegmentMs, setSelectedSegmentMs] = useState<{ startMs: number; endMs: number } | null>(null);
    const [fighterDetails, setFighterDetails] = useState<Fighter | null>(null);
    const videoPlayerRef = useRef<VideoPlayerHandle>(null);
    const [studentIdentifier, setStudentIdentifier] = useState<string | null>(null);
    const [isAnalysisSaving, setIsAnalysisSaving] = useState(false);

    const loadAnalysis = useCallback(async () => {
        if (!videoId) return;
        try {
            const videoDetails = await getVideoDetails({ videoId, jwtToken: accessToken, refreshToken, hydrate });
            setVideoUrl(videoDetails.signedUrl);
            setTranscodeStatus(videoDetails.transcodeStatus ?? 'None');
            setStudentIdentifier(videoDetails.studentIdentifier);

            // Prefer the richer v2 analysis; fall back to the legacy editor when absent.
            let usedV2 = false;
            if (V2_ENABLED) {
                try {
                    const v2 = await getVideoAnalysisV2({ videoId, jwtToken: accessToken, refreshToken, hydrate });
                    if (v2) { setAnalysisV2(v2); usedV2 = true; }
                } catch (e) {
                    console.warn('v2 analysis fetch failed, falling back to legacy:', e);
                }
            }
            if (!usedV2) {
                const feedbackData: AnalysisResultDto = await getVideoFeedback({ videoId, jwtToken: accessToken, refreshToken, hydrate });
                setFeedbackList(feedbackData);
            }

            const fd = await getFighterDetails({ fighterId: videoDetails.fighterId, jwtToken: accessToken, refreshToken, hydrate });
            setFighterDetails(fd ?? null);
        } catch (error) {
            console.error('Error fetching data:', error);
        }
    }, [videoId, accessToken, refreshToken, hydrate]);

    useEffect(() => { loadAnalysis(); }, [loadAnalysis]);

    // Live analysis progress. The agentic pipeline broadcasts per-phase status + completion/failure on
    // the shared videoAnalysisHub (the connection is owned/started globally by NotificationsListener).
    // Here we only register handlers scoped to THIS video and detach them on unmount — we deliberately
    // do NOT start/stop the shared connection, to avoid racing the global owner.
    useEffect(() => {
        if (!videoId) return;
        const id = Number(videoId);

        const onStatus = (vId: number, phase: string) => {
            if (vId === id) setAnalysisPhase(phase);
        };
        const onCompleted = (vId: number) => {
            if (vId !== id) return;
            setAnalysisPhase(null);
            toast({
                title: t('videoReviewV2.progress.completeTitle'),
                description: t('videoReviewV2.progress.complete'),
                variant: 'default',
            });
            void loadAnalysis(); // swap in the fresh results without a manual reload
        };
        const onFailed = (vId: number) => {
            if (vId !== id) return;
            setAnalysisPhase(null);
            toast({
                title: t('videoReviewV2.progress.failedTitle'),
                description: t('videoReviewV2.progress.failedBody'),
                variant: 'destructive',
            });
        };

        analysisConnection.on('AnalysisStatusChanged', onStatus);
        analysisConnection.on('AnalysisV2Completed', onCompleted);
        analysisConnection.on('AnalysisV2Failed', onFailed);

        return () => {
            analysisConnection.off('AnalysisStatusChanged', onStatus);
            analysisConnection.off('AnalysisV2Completed', onCompleted);
            analysisConnection.off('AnalysisV2Failed', onFailed);
        };
    }, [videoId, t, toast, loadAnalysis]);

    // While a playback transcode is running, poll for completion and swap in the playable URL.
    useEffect(() => {
        if (!videoId || transcodeStatus !== 'Processing') return;
        const interval = setInterval(async () => {
            try {
                const details = await getVideoDetails({ videoId, jwtToken: accessToken, refreshToken, hydrate });
                const next = details.transcodeStatus ?? 'None';
                if (next !== 'Processing') {
                    setTranscodeStatus(next);
                    // On Ready the signed URL now points at the H.264 playback copy; refresh the player.
                    if (next === 'Ready' && details.signedUrl) setVideoUrl(details.signedUrl);
                }
            } catch (e) {
                console.warn('Transcode status poll failed:', e);
            }
        }, 10000);
        return () => clearInterval(interval);
    }, [videoId, transcodeStatus, accessToken, refreshToken, hydrate]);

    const handleRequestTranscode = async () => {
        if (!videoId) return;
        try {
            const res = await requestTranscode({ videoId, jwtToken: accessToken, hydrate });
            setTranscodeStatus(res.transcodeStatus ?? 'Processing');
        } catch (error) {
            // Surface failures (e.g. backend 400 when transcoding is disabled) — otherwise the click
            // silently no-ops and the Convert button keeps inviting a retry that always fails.
            console.error('Transcode request failed:', error);
            alert(t('videoReviewV2.player.convertError'));
        }
    };

    const handleInputChange = (section: string, index: string | number, field: string | number, value: any) => {
        const updatedAnalysis = { ...feedbackList } as any;
        if (section === 'overallAnalysis') {
            updatedAnalysis[section][field] = value;
        } else {
            updatedAnalysis[section][index][field] = value;
        }
        setFeedbackList(updatedAnalysis);
    };

    const handleSaveAnalysisResult = async (partialFeedbackData: AnalysisResultDto) => {
        if (!partialFeedbackData || !videoId) return;
        try {
            setIsAnalysisSaving(true);
            const updated = await saveVideoAnalysisResult({
                videoId, analysisResultBody: partialFeedbackData, jwtToken: accessToken, refreshToken, hydrate,
            });
            setFeedbackList(updated);
            alert('Changes saved successfully!');
        } catch (error: any) {
            console.error('Save operation failed:', error);
            alert(`Error saving changes: ${error.message || 'Unknown error'}`);
        } finally {
            setIsAnalysisSaving(false);
        }
    };

    const handleSaveV2 = async (draft: AnalysisV2Dto) => {
        if (!videoId) return;
        try {
            setIsAnalysisSaving(true);
            const updated = await saveVideoAnalysisV2({ videoId, analysisBody: draft, jwtToken: accessToken, hydrate });
            setAnalysisV2(updated);
        } catch (error: any) {
            console.error('Save (v2) failed:', error);
            alert(`Error saving changes: ${error.message || 'Unknown error'}`);
        } finally {
            setIsAnalysisSaving(false);
        }
    };

    const handleSeek = (timestamp: string) => videoPlayerRef.current?.seekTo(timestamp);
    const handleSeekMs = (ms: number) => videoPlayerRef.current?.seekToMs(ms);
    const clearSelection = () => { setSelectedSegment(null); setSelectedSegmentMs(null); };

    const eventMarkers: EventMarker[] = (analysisV2?.matchEvents ?? []).map((e, i) => ({
        id: e.id ?? i,
        startMs: e.startTimestampMs,
        label: e.techniqueName || t(techniqueCategoryKey(e.techniqueCategory)),
        color: actorMarkerColor[e.actor],
    }));

    return (
        <div className="w-full">
            <h1 className="font-serif text-3xl font-bold text-center mt-3 text-ink-400">{t('videoReviewV2.page.title')}</h1>
            {analysisPhase && (
                <div className="mx-auto mt-2 flex max-w-2xl items-center justify-center space-x-2 rounded-md border border-[rgba(60,50,40,0.10)] bg-parchment-50 px-4 py-2 shadow-zen-sm">
                    <div className="animate-spin rounded-full h-5 w-5 border-t-2 border-b-2 border-samurai-400"></div>
                    <p className="text-samurai-400">{t(`videoReviewV2.progress.${analysisPhase.toLowerCase()}`)}</p>
                </div>
            )}
            <div className="flex flex-col md:flex-row gap-4 md:p-4 m-0 md:m-2">
                <div className="w-full md:w-1/2 flex flex-col gap-4">
                    <div className="rounded-lg shadow-zen-sm bg-parchment-50 border border-[rgba(60,50,40,0.10)] p-2 md:p-4">
                        <VideoPlayer
                            ref={videoPlayerRef}
                            videoUrl={videoUrl}
                            videoId={videoId || '0'}
                            identifiedTechniques={analysisV2 ? [] : (feedbackList?.techniques || [])}
                            selectedSegment={selectedSegment}
                            setSelectedSegment={setSelectedSegment}
                            clearSelection={clearSelection}
                            onSegmentSelect={(start, end) => setSelectedSegment({ start, end })}
                            eventMarkers={analysisV2 ? eventMarkers : undefined}
                            onSegmentSelectMs={(startMs, endMs) => setSelectedSegmentMs({ startMs, endMs })}
                            transcodeStatus={transcodeStatus}
                            onRequestTranscode={handleRequestTranscode}
                        />
                    </div>
                    <div className="rounded-lg shadow-zen-sm bg-parchment-50 border border-[rgba(60,50,40,0.10)] p-2 md:p-4">
                        <StudentDetails fighterDetails={fighterDetails} studentIdentifier={studentIdentifier} />
                    </div>
                </div>
                <div className="w-full md:w-1/2 mt-4 md:mt-0">
                    <div className="rounded-lg shadow-zen-sm bg-parchment-50 border border-[rgba(60,50,40,0.10)] p-2 md:p-4 h-full">
                        {analysisV2 ? (
                            <AnalysisV2Panel
                                analysis={analysisV2}
                                onSeekMs={handleSeekMs}
                                onSave={handleSaveV2}
                                isSaving={isAnalysisSaving}
                                selectedSegmentMs={selectedSegmentMs}
                            />
                        ) : (
                            <TechniqueFeedback
                                feedbackData={feedbackList}
                                onSeek={handleSeek}
                                saveChanges={handleSaveAnalysisResult}
                                onInputChange={handleInputChange}
                                selectedSegment={selectedSegment}
                                isAnalysisSaving={isAnalysisSaving}
                            />
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default VideoReview;

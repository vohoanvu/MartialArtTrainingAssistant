import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { connection, analysisConnection } from '../services/SignalRService';
import * as signalR from '@microsoft/signalr';
import NotificationPopup from './NotificationPopup';

const NotificationListener: React.FC = () => {
    const { t } = useTranslation();
    const [notification, setNotification] = useState({ bannerTitle: '', videoTitle: '', userName: '' });
    const [showNotification, setShowNotification] = useState(false);

    // Mounted globally in Layout: this component OWNS the SignalR connection lifecycle and renders the
    // top banner for every notification (video shares + analysis completion/failure). Pages register
    // their own page-local handlers; they do NOT start/stop these shared connections.
    useEffect(() => {
        const showBanner = (bannerTitle: string, videoTitle: string, userName = '') => {
            setNotification({ bannerTitle, videoTitle, userName });
            setShowNotification(true);
        };

        const onShared = (bannerTitle: string, videoTitle: string, userName: string) =>
            showBanner(bannerTitle, videoTitle, userName);
        // The agentic v2 pipeline (default) emits AnalysisV2Completed; legacy single-shot emits
        // AnalysisCompleted. Both surface the same completion banner.
        const onAnalysisDone = (videoId: number) =>
            showBanner(t('videoReviewV2.progress.completeTitle'), t('videoReviewV2.progress.completeBanner', { videoId }));
        const onAnalysisFailed = (_videoId: number) =>
            showBanner(t('videoReviewV2.progress.failedTitle'), t('videoReviewV2.progress.failedBody'));

        connection.on('ReceiveVideoSharedNotification', onShared);
        analysisConnection.on('AnalysisCompleted', onAnalysisDone);
        analysisConnection.on('AnalysisV2Completed', onAnalysisDone);
        analysisConnection.on('AnalysisV2Failed', onAnalysisFailed);

        if (connection.state === signalR.HubConnectionState.Disconnected) {
            connection.start().catch((e) => console.error('videoShareHub start failed:', e));
        }
        if (analysisConnection.state === signalR.HubConnectionState.Disconnected) {
            analysisConnection.start().catch((e) => console.error('videoAnalysisHub start failed:', e));
        }

        return () => {
            connection.off('ReceiveVideoSharedNotification', onShared);
            analysisConnection.off('AnalysisCompleted', onAnalysisDone);
            analysisConnection.off('AnalysisV2Completed', onAnalysisDone);
            analysisConnection.off('AnalysisV2Failed', onAnalysisFailed);
        };
    }, [t]);

    const handleCloseNotification = () => setShowNotification(false);

    return (
        <NotificationPopup
            bannerTitle={notification.bannerTitle}
            videoTitle={notification.videoTitle}
            userName={notification.userName}
            isVisible={showNotification}
            onClose={handleCloseNotification}
        />
    );
};

export default NotificationListener;

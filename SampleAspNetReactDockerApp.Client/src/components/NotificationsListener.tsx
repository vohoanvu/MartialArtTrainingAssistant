import React, { useEffect, useState } from 'react';
import { connection } from '../services/SignalRService';
import * as signalR from '@microsoft/signalr';
import NotificationPopup from './NotificationPopup';

const NotificationListener: React.FC = () => {
    const [isConnected, setIsConnected] = useState(false);
    const [notification, setNotification] = useState({ bannerTitle: '', videoTitle: '', userName: '' });
    const [showNotification, setShowNotification] = useState(false);
    useEffect(() => {
        if (!isConnected && connection.state === signalR.HubConnectionState.Disconnected) {
            console.log('Starting SignalR connection...');
            connection.start().then(() => {
                console.log('Connected to SignalR hub');
                setIsConnected(true);
                connection.on('ReceiveVideoSharedNotification', (bannerTitle: string, videoTitle: string, userName: string) => {
                    console.log('Client reveived notification... from SignalR hub', bannerTitle, videoTitle, userName);
                    setNotification({ bannerTitle, videoTitle, userName });
                    setShowNotification(true);
                });
            }).catch(error => {
                console.error('Error starting SignalR connection:', error);
            });
        }

        return () => {
            if (isConnected) {
                console.log('Stopping SignalR connection...');
                connection.stop();
                setIsConnected(false);
            }
        };
    }, [isConnected]);

    const handleCloseNotification = () => {
        setShowNotification(false);
    };

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

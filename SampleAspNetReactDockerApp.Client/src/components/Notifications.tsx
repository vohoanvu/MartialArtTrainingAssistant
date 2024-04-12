import React, { useEffect, useState } from 'react';
import { connection } from '../services/SignalRService';
import * as signalR from '@microsoft/signalr';

const NotificationComponent: React.FC = () => {
    const [isConnected, setIsConnected] = useState(false);

    useEffect(() => {
        if (!isConnected && connection.state === signalR.HubConnectionState.Disconnected) {
            console.log('Starting SignalR connection...');
            connection.start().then(() => {
                console.log('Connected to SignalR hub');
                setIsConnected(true);
                connection.on('ReceiveVideoSharedNotification', (videoTitle: string, userName: string) => {
                    alert(`New video shared: ${videoTitle} by ${userName}`);
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

    return null;
};

export default NotificationComponent;

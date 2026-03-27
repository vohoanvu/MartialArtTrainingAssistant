import * as signalR from '@microsoft/signalr';

export const connection = new signalR.HubConnectionBuilder()
    .withUrl(`/videoShareHub`, {
        withCredentials: true,
        transport: signalR.HttpTransportType.WebSockets
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

export const analysisConnection = new signalR.HubConnectionBuilder()
    .withUrl(`/videoAnalysisHub`, {
        withCredentials: true,
        transport: signalR.HttpTransportType.WebSockets
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();
import * as signalR from '@microsoft/signalr';

export const connection = new signalR.HubConnectionBuilder()
    .withUrl(`/videoShareHub`)
    .configureLogging(signalR.LogLevel.Information)
    .build();
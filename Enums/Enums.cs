﻿namespace TwitchBots.NET.Enums
{
    public enum ConnectionEventType
    {
        ConnectedToTwitch,
        DisconnectedFromTwitch
    }

    public enum ConnectionServerEventType
    {
        ConnectedToServer,
        DisconnectedFromServer
    }

    public enum MessageType
    {
        Received,
        Sent
    }

    public enum MessageWhisperEventType
    {
        Queued,
        Sent,
        SentImmediate,
        Received
    }

    public enum ErrorConnectionEventType
    {
        ConnectBot,
        ConnectToServer,
        DisconnectFromServer,
        DisconnectBot
    }

    public enum ErrorMessageEventType
    {
        Sending,
        Receiving
    }

    public enum ErrorMessageSendType
    {
        Queued,
        Immediate,
        QueuedSent,
        Received
    }

    public enum ErrorDataEventType
    {
        Get,
        Create,
        Update,
        Delete
    }

    public enum ServerChatColorChangeEventType
    {
        Initiated,
        Confirmed
    }

    public enum ErrorBotServerConnectEventType
    {
        JoinChannel,
        LeaveChannel
    }
}

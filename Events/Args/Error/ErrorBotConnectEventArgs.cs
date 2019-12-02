using TwitchBots.NET.Enums;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorBotConnectEventArgs : ErrorBotEventArgs
    {
        public ErrorConnectionEventType ErrorConnectionEventType { get; set; }
    }
}

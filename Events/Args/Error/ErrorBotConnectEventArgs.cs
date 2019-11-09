using Twitch.NET.Enums;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorBotConnectEventArgs : ErrorBotEventArgs
    {
        public ErrorConnectionEventType ErrorConnectionEventType { get; set; }
    }
}

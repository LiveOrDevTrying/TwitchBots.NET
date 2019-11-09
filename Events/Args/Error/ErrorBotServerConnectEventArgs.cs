using Twitch.NET.Enums;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorBotServerConnectEventArgs : ErrorBotServerEventArgs 
    {
        public ErrorBotServerConnectEventType ErrorBotServerConnectEventType { get; set; }
    }
}

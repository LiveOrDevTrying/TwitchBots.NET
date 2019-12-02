using TwitchBots.NET.Enums;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorBotServerConnectEventArgs : ErrorBotServerEventArgs 
    {
        public ErrorBotServerConnectEventType ErrorBotServerConnectEventType { get; set; }
    }
}

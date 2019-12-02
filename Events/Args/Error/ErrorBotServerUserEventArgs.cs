using TwitchBots.NET.Models.DTOs.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorBotServerUserEventArgs : ErrorBotServerEventArgs 
    {
        public string Username { get; set; }
    }
}

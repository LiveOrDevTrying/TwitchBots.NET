using Twitch.NET.Models.DTOs.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorBotServerUserEventArgs : ErrorBotServerEventArgs 
    {
        public string Username { get; set; }
    }
}

using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public abstract class ErrorBotServerEventArgs : ErrorBotEventArgs
    {
        public IServer Server { get; set; }
    }
}

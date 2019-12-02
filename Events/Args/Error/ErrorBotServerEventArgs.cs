using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public abstract class ErrorBotServerEventArgs : ErrorBotEventArgs
    {
        public IServer Server { get; set; }
    }
}

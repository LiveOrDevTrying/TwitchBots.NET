using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public abstract class ErrorBotEventArgs : ErrorEventArgs
    {
        public IBot Bot { get; set; }
    }

}

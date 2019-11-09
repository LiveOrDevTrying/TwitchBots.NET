using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public abstract class ErrorBotEventArgs : ErrorEventArgs
    {
        public IBot Bot { get; set; }
    }

}

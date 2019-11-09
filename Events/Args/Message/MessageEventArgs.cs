using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Message
{
    public abstract class MessageEventArgs : BaseEventArgs
    {
        public IMessage Message { get; set; }
    }
}

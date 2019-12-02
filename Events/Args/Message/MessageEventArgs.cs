using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Message
{
    public abstract class MessageEventArgs : BaseEventArgs
    {
        public IMessage Message { get; set; }
    }
}

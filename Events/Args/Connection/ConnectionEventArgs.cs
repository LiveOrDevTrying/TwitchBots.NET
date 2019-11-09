using Twitch.NET.Enums;

namespace Twitch.NET.Events.Args.Connection
{
    public abstract class ConnectionEventArgs : BaseEventArgs
    {
        public ConnectionEventType ConnectionEventType { get; set; }
    }
}

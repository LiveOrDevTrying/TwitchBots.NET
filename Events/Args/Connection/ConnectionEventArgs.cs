using TwitchBots.NET.Enums;

namespace TwitchBots.NET.Events.Args.Connection
{
    public abstract class ConnectionEventArgs : BaseEventArgs
    {
        public ConnectionEventType ConnectionEventType { get; set; }
    }
}

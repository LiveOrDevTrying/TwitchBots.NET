using TwitchBots.NET.Enums;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Connection
{
    public abstract class ConnectionServerEventArgs : ConnectionEventArgs
    {
        public IServer Server { get; set; }
        public ConnectionServerEventType ConnectionServerEventType { get; set; }
    }
}

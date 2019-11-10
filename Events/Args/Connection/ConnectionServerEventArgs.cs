using Twitch.NET.Enums;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Connection
{
    public abstract class ConnectionServerEventArgs : ConnectionEventArgs
    {
        public IServer Server { get; set; }
        public ConnectionServerEventType ConnectionServerEventType { get; set; }
    }
}

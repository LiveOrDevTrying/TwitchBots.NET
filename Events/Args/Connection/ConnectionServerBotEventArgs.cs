using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Connection
{
    public class ConnectionServerBotEventArgs  : ConnectionServerEventArgs
    {
        public IBot Bot { get; set; }
    }
}

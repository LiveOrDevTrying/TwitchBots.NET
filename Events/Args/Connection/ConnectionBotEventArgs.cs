using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Connection
{
    public class ConnectionBotEventArgs  : ConnectionEventArgs
    {
        public IBot Bot { get; set; }
    }
}

using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Connection
{
    public class ConnectionServerBotEventArgs  : ConnectionServerEventArgs
    {
        public IBot Bot { get; set; }
    }
}

using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Connection
{
    public class ConnectionBotEventArgs  : ConnectionEventArgs
    {
        public IBot Bot { get; set; }
    }
}

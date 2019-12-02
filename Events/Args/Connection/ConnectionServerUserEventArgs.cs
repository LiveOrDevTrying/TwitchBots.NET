using TwitchBots.NET.Models.DTOs.Interfaces;

namespace TwitchBots.NET.Events.Args.Connection
{
    public class ConnectionServerUserEventArgs  : ConnectionServerEventArgs
    {
        public IUserDTO User { get; set; }
    }
}

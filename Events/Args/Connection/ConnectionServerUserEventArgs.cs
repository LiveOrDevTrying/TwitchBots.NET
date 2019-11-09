using Twitch.NET.Models.DTOs.Interfaces;

namespace Twitch.NET.Events.Args.Connection
{
    public class ConnectionServerUserEventArgs  : ConnectionServerEventArgs
    {
        public IUserDTO User { get; set; }
    }
}

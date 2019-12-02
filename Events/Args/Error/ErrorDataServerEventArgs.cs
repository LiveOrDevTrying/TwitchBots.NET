using TwitchBots.NET.Models.DTOs.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorDataServerEventArgs : ErrorDataEventArgs
    {
        public IServerDTO Server { get; set; }
    }
}

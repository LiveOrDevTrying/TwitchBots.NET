using Twitch.NET.Models.DTOs.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorDataServerEventArgs : ErrorDataEventArgs
    {
        public IServerDTO Server { get; set; }
    }
}

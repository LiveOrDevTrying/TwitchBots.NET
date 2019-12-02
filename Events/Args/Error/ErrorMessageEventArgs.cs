using TwitchBots.NET.Enums;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public abstract class ErrorMessageEventArgs : ErrorEventArgs
    {
        public IBot Bot { get; set; }
        public string Message { get; set; }
        public ErrorMessageEventType ErrorMessageEventType { get; set; }
        public ErrorMessageSendType ErrorMessageSendType { get; set; }
    }

}

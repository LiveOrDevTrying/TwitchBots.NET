using Twitch.NET.Enums;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Message
{
    public class MessageWhisperEventArgs : MessageEventArgs
    {
        public MessageWhisperEventType MessageWhisperEventType { get; set; }
    }
}

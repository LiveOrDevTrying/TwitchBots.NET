using TwitchBots.NET.Enums;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Message
{
    public class MessageWhisperEventArgs : MessageEventArgs
    {
        public MessageWhisperEventType MessageWhisperEventType { get; set; }
    }
}

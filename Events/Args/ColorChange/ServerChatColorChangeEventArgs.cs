using TwitchBots.NET.Enums;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.ColorChange
{
    public class ServerChatColorChangeEventArgs : BaseEventArgs
    {
        public IBot Bot { get; set; }
        public IServer Server { get; set; }
        public string HexColorCode { get; set; }
        public ServerChatColorChangeEventType ServerChatColorChangeEventType { get; set; }
    }
}

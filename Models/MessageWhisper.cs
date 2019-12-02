using System;
using TwitchBots.NET.Enums;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Models
{
    public struct MessageWhisper : IMessageWhisper
    {
        public IBot Bot { get; set; }
        public Guid Id { get; set; }
        public string MessageText { get; set; }
        public MessageType MessageType { get; set; }
        public DateTime Timestamp { get; set; }
        public IUserDTO User { get; set; }
    }
}

using System;
using TwitchBots.NET.Enums;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Models
{
    public struct MessageServerChat : IMessageServerChat
    {
        public Guid Id { get; set; }
        public IServer Server { get; set; }
        public string MessageText { get; set; }
        public IUserDTO User { get; set; }
        public IBot Bot { get; set; }
        public string ChatColor { get; set; }
        public DateTime Timestamp { get; set; }
        public MessageType MessageType { get; set; }
    }
}

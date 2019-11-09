using System;
using Twitch.NET.Enums;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Models
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

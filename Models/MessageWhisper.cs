using System;
using Twitch.NET.Enums;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Models
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

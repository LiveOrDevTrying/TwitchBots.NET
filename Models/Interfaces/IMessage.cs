using System;
using TwitchBots.NET.Enums;
using TwitchBots.NET.Models.DTOs.Interfaces;

namespace TwitchBots.NET.Models.Interfaces
{
    public interface IMessage
    {
        IBot Bot { get; set; }
        Guid Id { get; set; }
        string MessageText { get; set; }
        MessageType MessageType { get; set; }
        DateTime Timestamp { get; set; }
        IUserDTO User { get; set; }
    }
}
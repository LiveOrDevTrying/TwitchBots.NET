using System;
using Twitch.NET.Enums;
using Twitch.NET.Models.DTOs.Interfaces;

namespace Twitch.NET.Models.Interfaces
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
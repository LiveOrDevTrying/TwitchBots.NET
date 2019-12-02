using System;
using TwitchBots.NET.Models.DTOs.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorDataUserEventArgs : ErrorDataEventArgs
    {
        public IUserDTO User { get; set; }
        public Guid UserId { get; set; }
    }
}

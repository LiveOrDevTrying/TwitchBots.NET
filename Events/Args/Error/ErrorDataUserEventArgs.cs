using System;
using Twitch.NET.Models.DTOs.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorDataUserEventArgs : ErrorDataEventArgs
    {
        public IUserDTO User { get; set; }
        public Guid UserId { get; set; }
    }
}

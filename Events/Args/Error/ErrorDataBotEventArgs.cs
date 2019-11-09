using System;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorDataBotEventArgs : ErrorDataEventArgs
    {
        public IBot Bot { get; set; }
        public Guid BotId { get; set; }
        public Guid UserId { get; set; }
    }
}

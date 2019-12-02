using System;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorDataBotEventArgs : ErrorDataEventArgs
    {
        public IBot Bot { get; set; }
        public Guid BotId { get; set; }
        public Guid UserId { get; set; }
    }
}

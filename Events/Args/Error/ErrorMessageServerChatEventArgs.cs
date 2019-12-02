using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorMessageServerChatEventArgs : ErrorMessageEventArgs
    {
        public string ChatColor { get; set; }
    }

}

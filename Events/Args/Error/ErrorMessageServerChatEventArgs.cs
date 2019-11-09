using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorMessageServerChatEventArgs : ErrorMessageEventArgs
    {
        public string ChatColor { get; set; }
    }

}

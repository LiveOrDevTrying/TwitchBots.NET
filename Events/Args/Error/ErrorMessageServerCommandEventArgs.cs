using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorMessageServerCommandEventArgs : ErrorMessageEventArgs
    {
        public string ChatColor { get; set; }
    }
}

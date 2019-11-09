using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorMessageServerCommandEventArgs : ErrorMessageEventArgs
    {
        public string ChatColor { get; set; }
    }
}

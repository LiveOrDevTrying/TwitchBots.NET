using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorFollowEventArgs : ErrorEventArgs
    {
        public string[] UserIdsFollowed { get; set; }
    }

}

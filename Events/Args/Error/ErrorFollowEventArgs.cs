using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorFollowEventArgs : ErrorEventArgs
    {
        public string[] UserIdsFollowed { get; set; }
    }

}

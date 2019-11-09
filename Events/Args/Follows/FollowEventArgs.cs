using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Follows
{
    public class FollowEventArgs : BaseEventArgs
    { 
        public IServer Server { get; set; }
        public IUserDTO[] NewFollows { get; set; }
    }
}

using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Follows
{
    public class FollowEventArgs : BaseEventArgs
    { 
        public IServer Server { get; set; }
        public IUserDTO[] NewFollows { get; set; }
    }
}

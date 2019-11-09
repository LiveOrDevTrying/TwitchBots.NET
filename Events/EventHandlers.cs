using System.Threading.Tasks;
using Twitch.NET.Events.Args;

namespace Twitch.NET.Events
{
    public delegate Task TwitchNETEventHandler<T>(object sender, T args) where T : BaseEventArgs;
}

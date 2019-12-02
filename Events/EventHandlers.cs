using System.Threading.Tasks;
using TwitchBots.NET.Events.Args;

namespace TwitchBots.NET.Events
{
    public delegate Task TwitchNETEventHandler<T>(object sender, T args) where T : BaseEventArgs;
}

using TwitchBots.NET.Enums;

namespace TwitchBots.NET.Events.Args.Error
{
    public abstract class ErrorDataEventArgs : ErrorEventArgs
    {
        public ErrorDataEventType ErrorDataEventType { get; set; }
    }
}

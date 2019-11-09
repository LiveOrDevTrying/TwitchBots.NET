using Twitch.NET.Enums;

namespace Twitch.NET.Events.Args.Error
{
    public abstract class ErrorDataEventArgs : ErrorEventArgs
    {
        public ErrorDataEventType ErrorDataEventType { get; set; }
    }
}

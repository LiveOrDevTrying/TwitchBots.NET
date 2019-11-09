using System;

namespace Twitch.NET.Events.Args.Error
{
    public abstract class ErrorEventArgs : BaseEventArgs
    {
        public Exception Exception { get; set; }
    }
}

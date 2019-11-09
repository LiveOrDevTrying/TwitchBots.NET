using System;

namespace Twitch.NET.Events.Args
{
    public abstract class BaseEventArgs : EventArgs
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

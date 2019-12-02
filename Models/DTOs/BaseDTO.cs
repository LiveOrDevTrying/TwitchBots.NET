using System;

namespace TwitchBots.NET.Models.DTOs
{
    public abstract class BaseDTO : IDisposable
    {
        public Guid Id { get; set; }

        public virtual void Dispose()
        { }
    }
}

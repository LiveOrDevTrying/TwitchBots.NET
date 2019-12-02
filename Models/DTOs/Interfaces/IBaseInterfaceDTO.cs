using System;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Models.DTOs.Interfaces
{
    public interface IBaseInterfaceDTO : IBaseInterface
    {
        Guid Id { get; set; }
    }
}

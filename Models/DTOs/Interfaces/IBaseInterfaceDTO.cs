using System;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Models.DTOs.Interfaces
{
    public interface IBaseInterfaceDTO : IBaseInterface
    {
        Guid Id { get; set; }
    }
}

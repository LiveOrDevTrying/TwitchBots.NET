using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Models.DTOs.Interfaces
{
    public interface IServerDTO : IBaseInterfaceDTO
    {
        string Username { get; set; }
    }
}
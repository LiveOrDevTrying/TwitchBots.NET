using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Models.DTOs.Interfaces
{
    public interface IUserDTO : IBaseInterfaceDTO
    {
        string DisplayName { get; set; }
        string TwitchId { get; set; }
        string Username { get; set; }
    }
}
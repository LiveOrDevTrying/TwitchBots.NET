using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Models.DTOs.Interfaces
{
    public interface IUserDTO : IBaseInterfaceDTO
    {
        string DisplayName { get; set; }
        string TwitchId { get; set; }
        string Username { get; set; }
    }
}
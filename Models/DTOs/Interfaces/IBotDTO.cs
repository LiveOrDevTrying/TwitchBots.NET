using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Models.DTOs.Interfaces
{
    public interface IBotDTO : IBaseInterfaceDTO
    {
        IUserDTO UserDTO { get; set; }
    }
}
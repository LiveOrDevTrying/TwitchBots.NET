using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Models.DTOs.Interfaces
{
    public interface IBotDTO : IBaseInterfaceDTO
    {
        IUserDTO UserDTO { get; set; }
    }
}
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Models.DTOs.Interfaces
{
    public interface IServerDTO : IBaseInterfaceDTO
    {
        string Username { get; set; }
    }
}
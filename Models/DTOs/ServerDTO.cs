using Twitch.NET.Models.DTOs.Interfaces;

namespace Twitch.NET.Models.DTOs
{
    public class ServerDTO : BaseDTO, IServerDTO
    {
        public string Username { get; set; }
    }
}

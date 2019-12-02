using TwitchBots.NET.Models.DTOs.Interfaces;

namespace TwitchBots.NET.Models.DTOs
{
    public class ServerDTO : BaseDTO, IServerDTO
    {
        public string Username { get; set; }
    }
}

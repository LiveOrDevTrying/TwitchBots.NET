using TwitchBots.NET.Models.DTOs.Interfaces;

namespace TwitchBots.NET.Models.DTOs
{
    public class BotDTO : BaseDTO, IBotDTO
    {
        public IUserDTO UserDTO { get; set; }
    }
}

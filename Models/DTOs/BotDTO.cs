using Twitch.NET.Models.DTOs.Interfaces;

namespace Twitch.NET.Models.DTOs
{
    public class BotDTO : BaseDTO, IBotDTO
    {
        public IUserDTO User { get; set; }
    }
}

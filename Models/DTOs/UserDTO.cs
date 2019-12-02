using TwitchBots.NET.Models.DTOs.Interfaces;

namespace TwitchBots.NET.Models.DTOs
{
    public class UserDTO : BaseDTO, IUserDTO
    {
        public string DisplayName { get; set; }
        public string TwitchId { get; set; }
        public string Username { get; set; }
    }
}

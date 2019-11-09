using Twitch.NET.Models.DTOs.Interfaces;

namespace Twitch.NET.Models.DTOs
{
    public class UserDTO : BaseDTO, IUserDTO
    {
        public string DisplayName { get; set; }
        public string TwitchId { get; set; }
        public string Username { get; set; }
    }
}

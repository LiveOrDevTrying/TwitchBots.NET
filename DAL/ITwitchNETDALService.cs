using System;
using System.Threading.Tasks;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.DAL
{
    public interface ITwitchNETDALService
    {
        Task<IUserDTO> GetUserAsync(Guid id);
        Task<IUserDTO> GetUserByTwitchIdAsync(string twitchUserId);
        Task<IUserDTO> GetUserByTwitchUsernameAsync(string twitchUsername);
        Task<IUserDTO> CreateUserAsync(IUserDTO user);
        Task<IUserDTO> UpdateUserAsync(IUserDTO user);

        Task<IServerDTO> GetServerAsync(Guid id);
        Task<IServerDTO> GetServerByUsernameAsync(string twitchUsername);

        Task<IBotDTO> GetBotAsync(Guid id);

        Task<IUserDTO[]> GetUsersOnlineAsync(IServer server);
        Task<bool> CreateUsersOnlineAsync(IServer server, IUserDTO[] users);
    }
}

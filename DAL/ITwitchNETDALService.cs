using System;
using System.Threading.Tasks;
using Twitch.NET.Models.DTOs;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.DAL
{
    public interface ITwitchNETDALService
    {
        Task<IUserDTO> GetUserAsync(Guid id);
        Task<IUserDTO> GetUserByTwitchIdAsync(string twitchUserId);
        Task<IUserDTO> GetUserByTwitchUsernameAsync(string twitchUsername);
        Task<IUserDTO> CreateUserAsync(IUserDTO user);
        Task<IUserDTO> UpdateUserAsync(IUserDTO user);
        Task<bool> DeleteUserAsync(IUserDTO user);

        Task<IServerDTO> GetServerAsync(Guid id);
        Task<IServerDTO> GetServerByUsernameAsync(string twitchUsername);
        Task<IServerDTO> CreateServerAsync(IServerDTO server);
        Task<IServerDTO> UpdateServerAsync(IServerDTO server);
        Task<bool> DeleteServerAsync(IServerDTO server);

        Task<IBotDTO> GetBotAsync(Guid id);
        Task<IBotDTO> GetBotAsync(IUserDTO user);
        Task<IBotDTO> GetBotAsync(string username);
        Task<IBotDTO> CreateBotAsync(IBotDTO bot);
        Task<IBotDTO> UpdateBotAsync(IBotDTO bot);
        Task<bool> DeleteBotAsync(IBotDTO bot);

        Task<IUserDTO[]> GetUsersOnlineAsync(IServer server);
        Task<bool> CreateUsersOnlineAsync(IServer server, IUserDTO[] users);
    }
}

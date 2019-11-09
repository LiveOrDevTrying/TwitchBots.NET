using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.NET.Models.DTOs;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.DAL
{
    public class TwitchNETDALInMemoryService : ITwitchNETDALService
    {
        private ConcurrentDictionary<Guid, IBaseInterfaceDTO> _objects =
            new ConcurrentDictionary<Guid, IBaseInterfaceDTO>();
        private ConcurrentDictionary<Guid, KeyValuePair<IServer, IUserDTO[]>> _usersOnline =
            new ConcurrentDictionary<Guid, KeyValuePair<IServer, IUserDTO[]>>();

        public Task<IBotDTO> GetBotAsync(Guid id)
        {
            return _objects.TryGetValue(id, out var instance) &&
                instance is IBotDTO c
                ? Task.FromResult(c)
                : Task.FromResult<IBotDTO>(null);
        }
        public Task<IBotDTO> GetBotAsync(IUserDTO user)
        {
            var bot = _objects.Values
                .OfType<IBotDTO>()
                .FirstOrDefault(s => s.User.Id == user.Id);

            return bot != null ? Task.FromResult(bot) : Task.FromResult<IBotDTO>(null);
        }
        public Task<IBotDTO> GetBotAsync(string username)
        {
            var bot = _objects.Values
                .OfType<IBotDTO>()
                .FirstOrDefault(s => s.User.Username.Trim().ToLower() == username.Trim().ToLower());

            return bot != null ? Task.FromResult(bot) : Task.FromResult<IBotDTO>(null);
        }
        public Task<IBotDTO> CreateBotAsync(IBotDTO bot)
        {
            IBotDTO instance = new BotDTO
            {
                Id = Guid.NewGuid(),
                User = bot.User
            };

            return _objects.TryAdd(instance.Id, instance) ? Task.FromResult(instance) : Task.FromResult<IBotDTO>(null);
        }
        public Task<IBotDTO> UpdateBotAsync(IBotDTO bot)
        {
            if (_objects.TryGetValue(bot.Id, out var instance) &&
                instance is IBotDTO c)
            {
                c.User = bot.User;
                return Task.FromResult(c);
            }

            return Task.FromResult<IBotDTO>(null);
        }
        public Task<bool> DeleteBotAsync(IBotDTO bot)
        {
            return Task.FromResult(_objects.TryRemove(bot.Id, out var result));
        }

        public Task<IServerDTO> GetServerAsync(Guid id)
        {
            return _objects.TryGetValue(id, out var instance) &&
                instance is IServerDTO c
                ? Task.FromResult(c)
                : Task.FromResult<IServerDTO>(null);
        }
        public Task<IServerDTO> GetServerByUsernameAsync(string username)
        {
            var instance = _objects.Values
                .OfType<IServerDTO>()
                .FirstOrDefault(s => s.Username.Trim().ToLower() == username.Trim().ToLower());

            if (instance != null)
            {
                return Task.FromResult(instance);
            }

            return Task.FromResult<IServerDTO>(null);
        }
        public Task<IServerDTO> CreateServerAsync(IServerDTO server)
        {
            IServerDTO instance = new ServerDTO
            {
                Id = Guid.NewGuid(),
                Username = server.Username
            };

            return _objects.TryAdd(instance.Id, instance) ? Task.FromResult(instance) : Task.FromResult<IServerDTO>(null);
        }
        public Task<IServerDTO> UpdateServerAsync(IServerDTO server)
        {
            if (_objects.TryGetValue(server.Id, out var instance) &&
                instance is IServerDTO c)
            {
                c.Username = server.Username;
                return Task.FromResult(c);
            }

            return Task.FromResult<IServerDTO>(null);
        }
        public Task<bool> DeleteServerAsync(IServerDTO server)
        {
            return Task.FromResult(_objects.TryRemove(server.Id, out var result));
        }

        public Task<IUserDTO> GetUserAsync(Guid id)
        {
            return _objects.TryGetValue(id, out var instance) &&
               instance is IUserDTO c
               ? Task.FromResult(c)
               : Task.FromResult<IUserDTO>(null);
        }
        public Task<IUserDTO> GetUserByTwitchIdAsync(string twitchUserId)
        {
            var instance = _objects.Values
                .OfType<IUserDTO>()
                .FirstOrDefault(s => s.TwitchId == twitchUserId);

            return instance != null ? Task.FromResult(instance) : Task.FromResult<IUserDTO>(null);
        }
        public Task<IUserDTO> GetUserByTwitchUsernameAsync(string twitchUsername)
        {
            var instance = _objects.Values
                .OfType<IUserDTO>()
                .FirstOrDefault(s => s.Username.Trim().ToLower() == twitchUsername.Trim().ToLower());

            return instance != null ? Task.FromResult(instance) : Task.FromResult<IUserDTO>(null);
        }
        public Task<IUserDTO> CreateUserAsync(IUserDTO user)
        {
            IUserDTO instance = new UserDTO
            {
                Id = Guid.NewGuid(),
                Username = user.Username,
                DisplayName = user.DisplayName,
                TwitchId = user.TwitchId
            };

            return _objects.TryAdd(instance.Id, instance) ? Task.FromResult(instance) : Task.FromResult<IUserDTO>(null);
        }
        public Task<IUserDTO> UpdateUserAsync(IUserDTO user)
        {
            if (_objects.TryGetValue(user.Id, out var instance) &&
               instance is IUserDTO c)
            {
                c.Username = user.Username;
                c.TwitchId = user.TwitchId;
                c.DisplayName = user.DisplayName;
                return Task.FromResult(c);
            }

            return Task.FromResult<IUserDTO>(null);
        }
        public Task<bool> DeleteUserAsync(IUserDTO user)
        {
            return Task.FromResult(_objects.TryRemove(user.Id, out var result));
        }

        public Task<IUserDTO[]> GetUsersOnlineAsync(IServer server)
        {
            return _usersOnline.TryGetValue(server.ServerDTO.Id, out var kvp) ? Task.FromResult(kvp.Value) : Task.FromResult(new IUserDTO[0]);
        }
        public Task<bool> CreateUsersOnlineAsync(IServer server, IUserDTO[] users)
        {
            _usersOnline.TryRemove(server.ServerDTO.Id, out var kvp);
            kvp = new KeyValuePair<IServer, IUserDTO[]>(server, users);
            return Task.FromResult(_usersOnline.TryAdd(server.ServerDTO.Id, kvp));
        }
    }
}

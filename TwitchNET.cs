using System;
using System.Threading.Tasks;
using Twitch.NET.DAL;
using Twitch.NET.Enums;
using Twitch.NET.Events;
using Twitch.NET.Events.Args.ColorChange;
using Twitch.NET.Events.Args.Connection;
using Twitch.NET.Events.Args.Error;
using Twitch.NET.Events.Args.Follows;
using Twitch.NET.Events.Args.Message;
using Twitch.NET.Managers;
using Twitch.NET.Models;
using Twitch.NET.Models.DTOs;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;
using TwitchLib.Client.Enums;

namespace Twitch.NET
{
    public class TwitchNET : ITwitchNET
    {
        protected readonly ITwitchNETDALService _twitchNETService;
        protected readonly BotManager _twitchNETBotManager;

        public event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        public event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        public event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        public event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        public event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        public event TwitchNETEventHandler<MessageWhisperEventArgs> MessageWhisperEvent;
        public event TwitchNETEventHandler<FollowEventArgs> FollowEvent;
        public event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        public event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        public TwitchNET(ITwitchNETDALService twitchNETService)
        {
            _twitchNETService = twitchNETService;

            _twitchNETBotManager = new BotManager(_twitchNETService);
            _twitchNETBotManager.ConnectionBotEvent += OnConnectionBotEvent;
            _twitchNETBotManager.ConnectionServerBotEvent += OnConnectionServerBotEvent;
            _twitchNETBotManager.ConnectionServerUserEvent += OnConnectionServerUserEvent;
            _twitchNETBotManager.MessageServerChatEvent += OnMessageServerChatEvent;
            _twitchNETBotManager.MessageServerCommandEvent += OnMessageServerCommandEvent;
            _twitchNETBotManager.MessageWhisperEvent += OnMessageWhisperEvent;
            _twitchNETBotManager.FollowEvent += OnFollowEvent;
            _twitchNETBotManager.ColorChangeEvent += OnColorChangeEvent;
            _twitchNETBotManager.ErrorEvent += OnErrorEvent;
        }

        public virtual async Task<IBot> ConnectBotAsync(BotCredentials credentials, int reconnectIntervalSec)
        {
            var user = await _twitchNETService.GetUserByTwitchUsernameAsync(credentials.Username);

            if (user == null)
            {
                user = await _twitchNETService.CreateUserAsync(new UserDTO
                {
                    DisplayName = credentials.Username,
                    Username = credentials.Username,
                });
            }

            var bot = await _twitchNETService.GetBotAsync(user);

            if (bot == null)
            {
                bot = await _twitchNETService.CreateBotAsync(new BotDTO
                {
                    User = user
                });
            }

            return _twitchNETBotManager.AddBot(credentials, bot, reconnectIntervalSec * 1000);
        }
        public virtual async Task<bool> DisconnectBotAsync(IBot bot)
        {
            await Task.FromResult<object>(null);
            return _twitchNETBotManager.RemoveBot(bot);
        }

        public virtual async Task<IServer> ConnectBotToServerAsync(IBot bot, string serverName)
        {
            return await bot.JoinServerAsync(serverName);
        }
        public virtual async Task<bool> DisconnectBotFromServerAsync(IServer server)
        {
            server.Bot.LeaveServer(server);
            server.Dispose();

            return await Task.FromResult(true);
        }

        public virtual void SendMessageToServer(IBot bot, IServer server, string message, ChatColorPresets chatColor)
        {
            bot.SendMessage(server, message, chatColor);
        }
        public virtual void SendMessageToServer(IBot bot, IServer server, string message, string colorHexCode)
        {
            bot.SendMessage(server, message, colorHexCode);
        }
        public virtual void SendMessageToServerImmediate(IBot bot, IServer server, string message)
        {
            bot.SendMessageImmediate(server, message);
        }
        public virtual void SendCommandToServer(IBot bot, IServer server, string message, ChatColorPresets chatColor)
        {
                bot.SendCommand(server, message, chatColor);
        }
        public virtual void SendCommandToServer(IBot bot, IServer server, string message, string hexCodeColor)
        {
            bot.SendCommand(server, message, hexCodeColor);
        }
        public virtual void SendCommandToServerImmediate(IBot bot, IServer server, string message)
        {
            bot.SendCommandImmediate(server, message);
        }

        public virtual async Task<IUserDTO> GetUserAsync(Guid id)
        {
            try
            {
                return await _twitchNETService.GetUserAsync(id);
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorDataUserEventArgs
                {
                    Exception = ex,
                    UserId = id,
                    ErrorDataEventType = ErrorDataEventType.Get
                });
            }

            return null;
        }
        public virtual async Task<IUserDTO> CreateUserAsync(IUserDTO user)
        {
            try
            {
                return await _twitchNETService.CreateUserAsync(user);
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorDataUserEventArgs
                {
                    Exception = ex,
                    UserId = user.Id,
                    User = user,
                    ErrorDataEventType = ErrorDataEventType.Create,
                });
            }

            return null;
        }
        public virtual async Task<IBotDTO> GetBotAsync(Guid id)
        {
            try
            {
                return await _twitchNETService.GetBotAsync(id);
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorDataBotEventArgs
                {
                    Exception = ex,
                    ErrorDataEventType = ErrorDataEventType.Get,
                    BotId = id,
                });
            }

            return null;
        }
        public virtual async Task<IBotDTO> CreateBotAsync(Guid userId)
        {
            try
            {
                var user = await GetUserAsync(userId);

                if (user != null)
                {
                    return await _twitchNETService.CreateBotAsync(new BotDTO
                    {
                        User = user
                    });
                }
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorDataBotEventArgs
                {
                    ErrorDataEventType = ErrorDataEventType.Create,
                    Exception = ex,
                    UserId = userId
                });
            }
            return null;
        }

        protected virtual Task OnMessageWhisperEvent(object sender, MessageWhisperEventArgs args)
        {
            FireMessageWhisperEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnMessageServerCommandEvent(object sender, MessageServerCommandEventArgs args)
        {
            FireMessageServerCommandEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnMessageServerChatEvent(object sender, MessageServerChatEventArgs args)
        {
            FireMessageServerChatEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnConnectionServerUserEvent(object sender, ConnectionServerUserEventArgs args)
        {
            FireConnectionServerUserEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnConnectionServerBotEvent(object sender, ConnectionServerBotEventArgs args)
        {
            FireConnectionServerBotEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnConnectionBotEvent(object sender, ConnectionBotEventArgs args)
        {
            FireConnectionBotEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnFollowEvent(object sender, FollowEventArgs args)
        {
            FireFollowEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            FireColorChangeEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnErrorEvent(object sender, ErrorEventArgs args)
        {
            FireErrorEvent(sender, args);
            return Task.CompletedTask;
        }

        protected virtual void FireConnectionBotEvent(object sender, ConnectionBotEventArgs args)
        {
            ConnectionBotEvent?.Invoke(sender, args);
        }
        protected virtual void FireConnectionServerBotEvent(object sender, ConnectionServerBotEventArgs args)
        {
            ConnectionServerBotEvent?.Invoke(sender, args);
        }
        protected virtual void FireConnectionServerUserEvent(object sender, ConnectionServerUserEventArgs args)
        {
            ConnectionServerUserEvent?.Invoke(sender, args);
        }
        protected virtual void FireMessageServerChatEvent(object sender, MessageServerChatEventArgs args)
        {
            MessageServerChatEvent?.Invoke(sender, args);
        }
        protected virtual void FireMessageServerCommandEvent(object sender, MessageServerCommandEventArgs args)
        {
            MessageServerCommandEvent?.Invoke(sender, args);
        }
        protected virtual void FireMessageWhisperEvent(object sender, MessageWhisperEventArgs args)
        {
            MessageWhisperEvent?.Invoke(sender, args);
        }
        protected virtual void FireFollowEvent(object sender, FollowEventArgs args)
        {
            FollowEvent?.Invoke(sender, args);
        }
        protected virtual void FireColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            ColorChangeEvent?.Invoke(sender, args);
        }
        protected virtual void FireErrorEvent(object sender, ErrorEventArgs args)
        {
            ErrorEvent?.Invoke(sender, args);
        }

        public virtual void Dispose()
        {
            if (_twitchNETBotManager != null)
            {
                _twitchNETBotManager.Dispose();
                _twitchNETBotManager.ConnectionBotEvent -= OnConnectionBotEvent;
                _twitchNETBotManager.ConnectionServerBotEvent -= OnConnectionServerBotEvent;
                _twitchNETBotManager.ConnectionServerUserEvent -= OnConnectionServerUserEvent;
                _twitchNETBotManager.MessageServerChatEvent -= OnMessageServerChatEvent;
                _twitchNETBotManager.MessageServerCommandEvent -= OnMessageServerCommandEvent;
                _twitchNETBotManager.MessageWhisperEvent -= OnMessageWhisperEvent;
                _twitchNETBotManager.ColorChangeEvent -= OnColorChangeEvent;
                _twitchNETBotManager.ErrorEvent -= OnErrorEvent;
            }
        }

        public IBot[] Bots
        {
            get
            {
                return _twitchNETBotManager.GetBots;
            }
        }
    }
}
using System;
using System.Threading.Tasks;
using TwitchBots.NET.DAL;
using TwitchBots.NET.Enums;
using TwitchBots.NET.Events;
using TwitchBots.NET.Events.Args.ColorChange;
using TwitchBots.NET.Events.Args.Connection;
using TwitchBots.NET.Events.Args.Error;
using TwitchBots.NET.Events.Args.Follows;
using TwitchBots.NET.Events.Args.Message;
using TwitchBots.NET.Managers;
using TwitchBots.NET.Models;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;
using TwitchLib.Client.Enums;

namespace TwitchBots.NET
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
            var bot = await _twitchNETService.GetBotAsync(credentials.BotId);

            if (bot == null)
            {
                return null;
            }

            var instance = await _twitchNETBotManager.AddBotAsync(credentials, bot, reconnectIntervalSec * 1000);
            instance.ConnectionBotEvent += OnConnectionBotEvent;
            instance.ConnectionServerBotEvent += OnConnectionServerBotEvent;
            instance.ConnectionServerUserEvent += OnConnectionServerUserEvent;
            instance.MessageServerChatEvent += OnMessageServerChatEvent;
            instance.MessageServerCommandEvent += OnMessageServerCommandEvent;
            instance.MessageWhisperEvent += OnMessageWhisperEvent;
            instance.FollowEvent += OnFollowEvent;
            instance.ErrorEvent += OnErrorEvent;

            return instance;
        }
        public virtual async Task<bool> DisconnectBotAsync(IBot bot)
        {
            await Task.FromResult<object>(null);

            if (bot != null &&
                _twitchNETBotManager.RemoveBot(bot))
            {
                bot.ConnectionBotEvent -= OnConnectionBotEvent;
                bot.ConnectionServerBotEvent -= OnConnectionServerBotEvent;
                bot.ConnectionServerUserEvent -= OnConnectionServerUserEvent;
                bot.MessageServerChatEvent -= OnMessageServerChatEvent;
                bot.MessageServerCommandEvent -= OnMessageServerCommandEvent;
                bot.MessageWhisperEvent -= OnMessageWhisperEvent;
                bot.FollowEvent -= OnFollowEvent;
                bot.ErrorEvent -= OnErrorEvent;
            }
            return _twitchNETBotManager.RemoveBot(bot);
        }

        public virtual async Task<IServer> ConnectBotToServerAsync(IBot bot, string serverName)
        {
            return await bot.JoinServerAsync(serverName);
        }
        public virtual async Task<bool> DisconnectBotFromServerAsync(IServer server)
        {
            await server.Bot.LeaveServerAsync(server);
            server.Dispose();
            return true;
        }

        public virtual void SendMessageToServer(IBot bot, IServer server, string message, ChatColorPresets chatColor)
        {
            bot.SendMessage(server, message, chatColor);
        }
        public virtual void SendMessageToServer(IBot bot, IServer server, string message, string colorHexCode)
        {
            bot.SendMessage(server, message, colorHexCode);
        }
        public virtual async Task SendMessageToServerImmediateAsync(IBot bot, IServer server, string message)
        {
            await bot.SendMessageImmediateAsync(server, message);
        }
        public virtual void SendCommandToServer(IBot bot, IServer server, string message, ChatColorPresets chatColor)
        {
            bot.SendCommand(server, message, chatColor);
        }
        public virtual void SendCommandToServer(IBot bot, IServer server, string message, string hexCodeColor)
        {
            bot.SendCommand(server, message, hexCodeColor);
        }
        public virtual async Task SendCommandToServerImmediateAsync(IBot bot, IServer server, string message)
        {
            await bot.SendCommandImmediateAsync(server, message);
        }

        public virtual async Task<IUserDTO> GetUserAsync(Guid id)
        {
            try
            {
                return await _twitchNETService.GetUserAsync(id);
            }
            catch (Exception ex)
            {
                await FireErrorEventAsync(this, new ErrorDataUserEventArgs
                {
                    Exception = ex,
                    UserId = id,
                    ErrorDataEventType = ErrorDataEventType.Get
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
                await FireErrorEventAsync(this, new ErrorDataBotEventArgs
                {
                    Exception = ex,
                    ErrorDataEventType = ErrorDataEventType.Get,
                    BotId = id,
                });
            }

            return null;
        }

        protected virtual async Task OnConnectionBotEvent(object sender, ConnectionBotEventArgs args)
        {
            await FireConnectionBotEventAsync(sender, args);
        }
        protected virtual async Task OnConnectionServerBotEvent(object sender, ConnectionServerBotEventArgs args)
        {
            await FireConnectionServerBotEventAsync(sender, args);
        }
        protected virtual async Task OnMessageServerChatEvent(object sender, MessageServerChatEventArgs args)
        {
            await FireMessageServerChatEventAsync(sender, args);
        }
        protected virtual async Task OnMessageServerCommandEvent(object sender, MessageServerCommandEventArgs args)
        {
            await FireMessageServerCommandEventAsync(sender, args);
        }
        protected virtual async Task OnMessageWhisperEvent(object sender, MessageWhisperEventArgs args)
        {
            await FireMessageWhisperEventAsync(sender, args);
        }
        protected virtual async Task OnConnectionServerUserEvent(object sender, ConnectionServerUserEventArgs args)
        {
            await FireConnectionServerUserEventAsync(sender, args);
        }
        protected virtual async Task OnFollowEvent(object sender, FollowEventArgs args)
        {
            await FireFollowEventAsync(sender, args);
        }
        protected virtual async Task OnColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            await FireColorChangeEventAsync(sender, args);
        }
        protected virtual async Task OnErrorEvent(object sender, ErrorEventArgs args)
        {
            await FireErrorEventAsync(sender, args);
        }
        protected virtual async Task FireConnectionBotEventAsync(object sender, ConnectionBotEventArgs args)
        {
            await ConnectionBotEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireConnectionServerBotEventAsync(object sender, ConnectionServerBotEventArgs args)
        {
            await ConnectionServerBotEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireConnectionServerUserEventAsync(object sender, ConnectionServerUserEventArgs args)
        {
            await ConnectionServerUserEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireMessageServerChatEventAsync(object sender, MessageServerChatEventArgs args)
        {
            await MessageServerChatEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireMessageServerCommandEventAsync(object sender, MessageServerCommandEventArgs args)
        {
            await MessageServerCommandEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireMessageWhisperEventAsync(object sender, MessageWhisperEventArgs args)
        {
            await MessageWhisperEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireFollowEventAsync(object sender, FollowEventArgs args)
        {
            await FollowEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireColorChangeEventAsync(object sender, ServerChatColorChangeEventArgs args)
        {
            await ColorChangeEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireErrorEventAsync(object sender, ErrorEventArgs args)
        {
            await ErrorEvent?.Invoke(sender, args);
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
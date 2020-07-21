using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TwitchBots.NET.DAL;
using TwitchBots.NET.Events;
using TwitchBots.NET.Events.Args.ColorChange;
using TwitchBots.NET.Events.Args.Connection;
using TwitchBots.NET.Events.Args.Error;
using TwitchBots.NET.Events.Args.Follows;
using TwitchBots.NET.Events.Args.Message;
using TwitchBots.NET.Models;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Managers
{
    public sealed class BotManager : IDisposable
    {
        private readonly ITwitchNETDALService _twitchNETService;

        private ConcurrentDictionary<Guid, IBot> _bots =
            new ConcurrentDictionary<Guid, IBot>();

        public event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        public event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        public event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        public event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        public event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        public event TwitchNETEventHandler<MessageWhisperEventArgs> MessageWhisperEvent;
        public event TwitchNETEventHandler<FollowEventArgs> FollowEvent;
        public event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        public event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        public BotManager(ITwitchNETDALService twitchNETService)
        {
            _twitchNETService = twitchNETService;
        }

        public async Task<IBot> AddBotAsync(BotCredentials credentials, IBotDTO botDTO, int reconnectIntervalMS)
        {
            IBot bot = new Bot(_twitchNETService, botDTO, reconnectIntervalMS);
            bot.ConnectionBotEvent += OnConnectionBotEvent;
            bot.ConnectionServerBotEvent += OnConnectionServerBotEvent;
            bot.ConnectionServerUserEvent += OnConnectionServerUserEvent;
            bot.MessageServerChatEvent += OnMessageServerChatEvent;
            bot.MessageServerCommandEvent += OnMessageServerCommandEvent;
            bot.MessageWhisperEvent += OnMessageWhisperEvent;
            bot.ColorChangeEvent += OnColorChangeEvent;
            bot.ErrorEvent += OnErrorEvent;
            await bot.ConnectAsync(credentials);

            _bots.TryAdd(botDTO.Id, bot);

            return bot;
        }
        public bool RemoveBot(IBot bot)
        {
            if (_bots.TryRemove(bot.BotDTO.Id, out bot))
            {
                bot.Dispose();

                bot.ConnectionBotEvent -= OnConnectionBotEvent;
                bot.ConnectionServerBotEvent -= OnConnectionServerBotEvent;
                bot.ConnectionServerUserEvent -= OnConnectionServerUserEvent;
                bot.MessageServerChatEvent -= OnMessageServerChatEvent;
                bot.MessageServerCommandEvent -= OnMessageServerCommandEvent;
                bot.MessageWhisperEvent -= OnMessageWhisperEvent;
                bot.ColorChangeEvent -= OnColorChangeEvent;
                bot.ErrorEvent -= OnErrorEvent;
                return true;
            }

            return false;
        }

        private async Task OnMessageServerCommandEvent(object sender, MessageServerCommandEventArgs args)
        {
            await FireMessageServerCommandEventAsync(sender, args);
        }
        private async Task OnMessageWhisperEvent(object sender, MessageWhisperEventArgs args)
        {
            await FireMessageWhisperEventAsync(sender, args);
        }
        private async Task OnMessageServerChatEvent(object sender, MessageServerChatEventArgs args)
        {
            await FireMessageServerChatEventAsync(sender, args);
        }
        private async Task OnConnectionServerUserEvent(object sender, ConnectionServerUserEventArgs args)
        {
            await FireConnectionServerUserEventAsync(sender, args);
        }
        private async Task OnConnectionServerBotEvent(object sender, ConnectionServerBotEventArgs args)
        {
            await FireConnectionServerBotEventAsync(sender, args);
        }
        private async Task OnConnectionBotEvent(object sender, ConnectionBotEventArgs args)
        {
            await FireConnectionBotEventAsync(sender, args);
        }
        private async Task OnColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            await FireColorChangeEventAsync(sender, args);
        }
        private async Task OnErrorEvent(object sender, ErrorEventArgs args)
        {
            await FireErrorEventAsync(sender, args);
        }

        private async Task FireConnectionBotEventAsync(object sender, ConnectionBotEventArgs args)
        {
            await ConnectionBotEvent?.Invoke(sender, args);
        }
        private async Task FireConnectionServerBotEventAsync(object sender, ConnectionServerBotEventArgs args)
        {
            await ConnectionServerBotEvent?.Invoke(sender, args);
        }
        private async Task FireConnectionServerUserEventAsync(object sender, ConnectionServerUserEventArgs args)
        {
            await ConnectionServerUserEvent?.Invoke(sender, args);
        }
        private async Task FireMessageServerChatEventAsync(object sender, MessageServerChatEventArgs args)
        {
            await MessageServerChatEvent?.Invoke(sender, args);
        }
        private async Task FireMessageServerCommandEventAsync(object sender, MessageServerCommandEventArgs args)
        {
            await MessageServerCommandEvent?.Invoke(sender, args);
        }
        private async Task FireMessageWhisperEventAsync(object sender, MessageWhisperEventArgs args)
        {
            await MessageWhisperEvent?.Invoke(sender, args);
        }
        private async Task FireFollowEventAsync(object sender, FollowEventArgs args)
        {
            await FollowEvent?.Invoke(sender, args);
        }
        private async Task FireColorChangeEventAsync(object sender, ServerChatColorChangeEventArgs args)
        {
            await ColorChangeEvent?.Invoke(sender, args);
        }
        private async Task FireErrorEventAsync(object sender, ErrorEventArgs args)
        {
            await ErrorEvent?.Invoke(sender, args);
        }

        public void Dispose()
        {
            foreach (var bot in _bots.Values)
            {
                RemoveBot(bot);
            }
        }

        public IBot[] GetBots
        {
            get
            {
                return _bots.Values.ToArray();
            }
        }
    }
}
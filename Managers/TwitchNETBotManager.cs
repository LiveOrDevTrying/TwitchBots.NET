using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Twitch.NET.DAL;
using Twitch.NET.Events;
using Twitch.NET.Events.Args.ColorChange;
using Twitch.NET.Events.Args.Connection;
using Twitch.NET.Events.Args.Error;
using Twitch.NET.Events.Args.Message;
using Twitch.NET.Models;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Managers
{
    public sealed class TwitchNETBotManager : IDisposable
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
        public event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        public event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        public TwitchNETBotManager(ITwitchNETDALService twitchNETService)
        {
            _twitchNETService = twitchNETService;
        }

        public IBot AddBot(BotCredentials credentials, IBotDTO botDTO, int reconnectIntervalMS)
        {
            var bot = new Bot(_twitchNETService, botDTO, reconnectIntervalMS);
            bot.ConnectionBotEvent += OnConnectionBotEvent;
            bot.ConnectionServerBotEvent += OnConnectionServerBotEvent;
            bot.ConnectionServerUserEvent += OnConnectionServerUserEvent;
            bot.MessageServerChatEvent += OnMessageServerChatEvent;
            bot.MessageServerCommandEvent += OnMessageServerCommandEvent;
            bot.MessageWhisperEvent += OnMessageWhisperEvent;
            bot.ColorChangeEvent += OnColorChangeEvent;
            bot.ErrorEvent += OnErrorEvent;
            bot.Connect(credentials);

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

        private Task OnMessageServerCommandEvent(object sender, MessageServerCommandEventArgs args)
        {
            FireMessageServerCommandEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnMessageWhisperEvent(object sender, MessageWhisperEventArgs args)
        {
            FireMessageWhisperEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnMessageServerChatEvent(object sender, MessageServerChatEventArgs args)
        {
            FireMessageServerChatEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnConnectionServerUserEvent(object sender, ConnectionServerUserEventArgs args)
        {
            FireConnectionServerUserEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnConnectionServerBotEvent(object sender, ConnectionServerBotEventArgs args)
        {
            FireConnectionServerBotEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnConnectionBotEvent(object sender, ConnectionBotEventArgs args)
        {
            FireConnectionBotEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            FireColorChangeEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnErrorEvent(object sender, ErrorEventArgs args)
        {
            FireErrorEvent(sender, args);
            return Task.CompletedTask;
        }

        private void FireConnectionBotEvent(object sender, ConnectionBotEventArgs args)
        {
            ConnectionBotEvent?.Invoke(sender, args);
        }
        private void FireConnectionServerBotEvent(object sender, ConnectionServerBotEventArgs args)
        {
            ConnectionServerBotEvent?.Invoke(sender, args);
        }
        private void FireConnectionServerUserEvent(object sender, ConnectionServerUserEventArgs args)
        {
            ConnectionServerUserEvent?.Invoke(sender, args);
        }
        private void FireMessageServerChatEvent(object sender, MessageServerChatEventArgs args)
        {
            MessageServerChatEvent?.Invoke(sender, args);
        }
        private void FireMessageServerCommandEvent(object sender, MessageServerCommandEventArgs args)
        {
            MessageServerCommandEvent?.Invoke(sender, args);
        }
        private void FireMessageWhisperEvent(object sender, MessageWhisperEventArgs args)
        {
            MessageWhisperEvent?.Invoke(sender, args);
        }
        private void FireColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            ColorChangeEvent?.Invoke(sender, args);
        }
        private void FireErrorEvent(object sender, ErrorEventArgs args)
        {
            ErrorEvent?.Invoke(sender, args);
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
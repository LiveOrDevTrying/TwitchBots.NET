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

        public IBot AddBot(BotCredentials credentials, IBotDTO botDTO, int reconnectIntervalMS)
        {
            IBot bot = new Bot(_twitchNETService, botDTO, reconnectIntervalMS);
            bot.ConnectionBotEvent += OnConnectionBotEvent;
            bot.ConnectionServerBotEvent += OnConnectionServerBotEvent;
            bot.ConnectionServerUserEvent += OnConnectionServerUserEvent;
            bot.MessageServerChatEvent += OnMessageServerChatEvent;
            bot.MessageServerCommandEvent += OnMessageServerCommandEvent;
            bot.MessageWhisperEvent += OnMessageWhisperEvent;
            bot.FollowEvent += OnFollowEvent;
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
                bot.FollowEvent -= OnFollowEvent;
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

        private Task OnFollowEvent(object sender, FollowEventArgs args)
        {
            throw new NotImplementedException();
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
        private void FireFollowEvent(object sender, FollowEventArgs args)
        {
            FollowEvent?.Invoke(sender, args);
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
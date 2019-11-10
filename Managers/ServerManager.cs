using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.NET.DAL;
using Twitch.NET.Events;
using Twitch.NET.Events.Args.ColorChange;
using Twitch.NET.Events.Args.Connection;
using Twitch.NET.Events.Args.Error;
using Twitch.NET.Events.Args.Follows;
using Twitch.NET.Events.Args.Message;
using Twitch.NET.Models;
using Twitch.NET.Models.DTOs;
using Twitch.NET.Models.Interfaces;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;

namespace Twitch.NET.Managers
{
    public sealed class ServerManager : IDisposable
    {
        private readonly ITwitchNETDALService _twitchNetService;
        private readonly TwitchClient _client;
        private readonly ITwitchAPI _twitchAPI;
        private readonly IBot _bot;
        
        private ConcurrentQueue<IServer> _servers =
            new ConcurrentQueue<IServer>();
        private bool _isTimerRunning;
        private int _maxNumberMessagesInQueue;

        public event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        public event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        public event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        public event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        public event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        public event TwitchNETEventHandler<FollowEventArgs> FollowEvent;
        public event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        public event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        public ServerManager(ITwitchNETDALService twitchNETService,
            TwitchClient client,
            ITwitchAPI twitchAPI,
            IBot bot,
            int maxNumberMessagesInQueue)
        {
            _twitchNetService = twitchNETService;
            _client = client;
            _bot = bot;
            _twitchAPI = twitchAPI;
            _maxNumberMessagesInQueue = maxNumberMessagesInQueue;
        }

        public async Task<IServer> JoinServerAsync(string serverName)
        { 
            var server = await _twitchNetService.GetServerByUsernameAsync(serverName);

            if (!_servers.Any(s => s.ServerDTO.Username.Trim().ToLower() == serverName.Trim().ToLower()))
            {
                var instance = new Server(_twitchNetService, _client, _bot, server, _twitchAPI, _maxNumberMessagesInQueue);
                instance.ConnectionBotEvent += OnConnectionBotEvent;
                instance.ConnectionServerBotEvent += OnConnectionServerBotEvent;
                instance.ConnectionServerUserEvent += OnConnectionServerUserEvent;
                instance.MessageServerChatEvent += OnMessageServerChat;
                instance.MessageServerCommandEvent += OnMessageServerCommand;
                instance.FollowEvent += OnFollowEvent;
                instance.ColorChangeEvent += OnColorChangeEvent;
                instance.ErrorEvent += OnErrorEvent;
                _servers.Enqueue(instance);
                return instance;
            }
            else
            {
                return _servers.FirstOrDefault(s => s.ServerDTO.Username.Trim().ToLower() == serverName.Trim().ToLower());
            }
        }
        public bool LeaveServer(IServer server)
        {
            var servers = new Queue<IServer>(_servers);
            var newQueue = new Queue<IServer>();
            var isSuccess = false;

            foreach (var item in servers)
            {
                if (item.ServerDTO.Id != server.ServerDTO.Id)
                {
                    newQueue.Enqueue(item);
                }
                else
                {
                    isSuccess = true;

                    item.ConnectionBotEvent -= OnConnectionBotEvent;
                    item.ConnectionServerBotEvent -= OnConnectionServerBotEvent;
                    item.ConnectionServerUserEvent -= OnConnectionServerUserEvent;
                    item.MessageServerChatEvent -= OnMessageServerChat;
                    item.MessageServerCommandEvent -= OnMessageServerCommand;
                    item.FollowEvent -= OnFollowEvent;
                    item.ColorChangeEvent -= OnColorChangeEvent;
                    item.ErrorEvent -= OnErrorEvent;
                    item.Dispose();
                }
            }

            _servers = new ConcurrentQueue<IServer>(newQueue);

            return isSuccess;
        }

        public void OnTimerTick()
        {
            if (!_isTimerRunning)
            {
                _isTimerRunning = true;

                try
                {
                    if (_servers.Any(s => s.NumberMessagesQueued > 0))
                    {
                        var isBreak = false;

                        do
                        {
                            if (_servers.TryDequeue(out var server))
                            {
                                _servers.Enqueue(server);

                                if (server.NumberMessagesQueued > 0)
                                {
                                    server.OnTimerTick();
                                    isBreak = true;
                                }
                            }
                        }
                        while (!isBreak);
                    }
                }
                catch
                { }

                _isTimerRunning = false;
            }
        }

        private Task OnMessageServerCommand(object sender, MessageServerCommandEventArgs args)
        {
            FireMessageServerCommandEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnMessageServerChat(object sender, MessageServerChatEventArgs args)
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
            throw new NotImplementedException();
        }
        private Task OnConnectionBotEvent(object sender, ConnectionBotEventArgs args)
        {
            FireConnectionBotEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnFollowEvent(object sender, FollowEventArgs args)
        {
            FireFollowEvent(sender, args);
            return Task.CompletedTask;
        }
        private Task OnColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            FireServerChatColorChangeEvent(sender, args);
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
        private void FireFollowEvent(object sender, FollowEventArgs args)
        {
            FollowEvent?.Invoke(sender, args);
        }
        private void FireServerChatColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            ColorChangeEvent?.Invoke(sender, args);
        }
        private void FireErrorEvent(object sender, ErrorEventArgs args)
        {
            ErrorEvent?.Invoke(sender, args);
        }

        public void Dispose()
        {
            while (_servers.Any())
            {
                if (!_servers.TryDequeue(out var server))
                {
                    break;
                }
                else
                {
                    LeaveServer(server);
                }
            }

            _servers.Clear();
        }

        public bool IsConnected
        {
            get
            {
                return _client.IsConnected;
            }
        }
        public ICollection<IServer> Servers
        {
            get
            {
                return _servers.ToList();
            }
        }
    }
}

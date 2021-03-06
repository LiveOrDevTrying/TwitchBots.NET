﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using TwitchBots.NET.Models.DTOs;
using TwitchBots.NET.Models.Interfaces;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;

namespace TwitchBots.NET.Managers
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
            var serverDTO = await _twitchNetService.GetServerByUsernameAsync(serverName);

            if (!_servers.Any(s => s.ServerDTO.Username.Trim().ToLower() == serverName.Trim().ToLower()))
            {
                var instance = new Server(_twitchNetService, _client, _bot, serverDTO, _twitchAPI, _maxNumberMessagesInQueue);
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
                var server = _servers.FirstOrDefault(s => s.ServerDTO.Username.Trim().ToLower() == serverName.Trim().ToLower());

                if (!server.IsConnected)
                {
                    server.RejoinChannel();
                }

                return server;
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

        private async Task OnMessageServerCommand(object sender, MessageServerCommandEventArgs args)
        {
            await FireMessageServerCommandEventAsync(sender, args);
        }
        private async Task OnMessageServerChat(object sender, MessageServerChatEventArgs args)
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
        private async Task OnFollowEvent(object sender, FollowEventArgs args)
        {
            await FireFollowEventAsync(sender, args);
        }
        private async Task OnColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            await FireServerChatColorChangeEventAsync(sender, args);
        }
        private async Task OnErrorEvent(object sender, ErrorEventArgs args)
        {
            await FireErrorEventAsync(sender, args);
        }

        private async Task FireConnectionBotEventAsync(object sender, ConnectionBotEventArgs args)
        {
            if (ConnectionBotEvent != null)
            {
                await ConnectionBotEvent?.Invoke(sender, args);
            }
        }
        private async Task FireConnectionServerBotEventAsync(object sender, ConnectionServerBotEventArgs args)
        {
            if (ConnectionServerBotEvent != null)
            {
                await ConnectionServerBotEvent?.Invoke(sender, args);
            }
        }
        private async Task FireConnectionServerUserEventAsync(object sender, ConnectionServerUserEventArgs args)
        {
            if (ConnectionServerUserEvent != null)
            {
                await ConnectionServerUserEvent?.Invoke(sender, args);
            }
        }
        private async Task FireMessageServerChatEventAsync(object sender, MessageServerChatEventArgs args)
        {
            if (MessageServerChatEvent != null)
            {
                await MessageServerChatEvent?.Invoke(sender, args);
            }
        }
        private async Task FireMessageServerCommandEventAsync(object sender, MessageServerCommandEventArgs args)
        {
            if (MessageServerCommandEvent != null)
            {
                await MessageServerCommandEvent?.Invoke(sender, args);
            }
        }
        private async Task FireFollowEventAsync(object sender, FollowEventArgs args)
        {
            if (FollowEvent != null)
            {
                await FollowEvent?.Invoke(sender, args);
            }
        }
        private async Task FireServerChatColorChangeEventAsync(object sender, ServerChatColorChangeEventArgs args)
        {
            if (ColorChangeEvent != null)
            {
                await ColorChangeEvent?.Invoke(sender, args);
            }
        }
        private async Task FireErrorEventAsync(object sender, ErrorEventArgs args)
        {
            if (ErrorEvent != null)
            {
                await ErrorEvent?.Invoke(sender, args);
            }
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
                return _servers.Where(x => x.Client.IsConnected && x.Client.JoinedChannels.Any(s => s.Channel.Trim().ToLower() == x.ServerDTO.Username.Trim().ToLower())).ToList();
            }
        }
    }
}

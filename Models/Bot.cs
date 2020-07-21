using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using TwitchBots.NET.Models.DTOs;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;
using TwitchBots.NET.Utils;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace TwitchBots.NET.Models
{
    public class Bot : IBot
    {
        protected readonly ITwitchNETDALService _twitchNetService;
        protected readonly IBotDTO _bot;

        protected TwitchClient _client;
        protected ServerManager _serverManager;
        protected BotCredentials _botCredentials;
        protected int _intervalReconnectMS;
        protected bool _isRunning = true;
        protected Timer _timer;
        protected TwitchAPI _twitchAPI;
        protected bool _wasServerMessage;

        protected ConcurrentQueue<IMessageWhisper> _whispersQueued =
            new ConcurrentQueue<IMessageWhisper>();

        public event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        public event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        public event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        public event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        public event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        public event TwitchNETEventHandler<MessageWhisperEventArgs> MessageWhisperEvent;
        public event TwitchNETEventHandler<FollowEventArgs> FollowEvent;
        public event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        public event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        public Bot(ITwitchNETDALService twitchNETService,
            IBotDTO bot,
            int intervalReconnectMS)
        {
            _twitchNetService = twitchNETService;
            _bot = bot;
            _intervalReconnectMS = intervalReconnectMS;
        }

        public virtual async Task ConnectAsync(BotCredentials botCredentials)
        {
            try
            {
                _botCredentials = botCredentials;

                _twitchAPI = new TwitchAPI();
                _twitchAPI.Settings.ClientId = _botCredentials.ClientId;
                _twitchAPI.Settings.AccessToken = $"oauth:{_botCredentials.OAuthToken}";

                _botCredentials = botCredentials;
                var credentials = new ConnectionCredentials(_botCredentials.Username, _botCredentials.OAuthToken);

                await DisconnectAsync();

                _isRunning = true;

                _client = new TwitchClient();
                _client.Initialize(credentials);

                _client.OnWhisperReceived += OnWhisperReceived;
                _client.OnNewSubscriber += OnNewSubscriber;
                _client.OnConnected += OnConnected;
                _client.OnDisconnected += OnDisconnected;

                _serverManager = new ServerManager(_twitchNetService, _client, _twitchAPI, this, botCredentials.MaxMessagesInQueue);
                _serverManager.ConnectionBotEvent += OnConnectionBotEventAsync;
                _serverManager.ConnectionServerBotEvent += OnConnectionServerBotEventAsync;
                _serverManager.ConnectionServerUserEvent += OnConnectionServerUserEventAsync;
                _serverManager.MessageServerChatEvent += OnMessageServerChatEventAsync;
                _serverManager.MessageServerCommandEvent += OnMessageServerCommandEventAsync;
                _serverManager.FollowEvent += OnFollowEventAsync;
                _serverManager.ColorChangeEvent += OnColorChangeEventAsync;
                _serverManager.ErrorEvent += OnErrorEventAsync;

                _client.Connect();
            }
            catch (Exception ex)
            {
                await FireErrorEventAsync(this, new ErrorBotConnectEventArgs
                {
                    Bot = this,
                    ErrorConnectionEventType = ErrorConnectionEventType.ConnectBot,
                    Exception = ex
                });
            }
        }
        public virtual async Task DisconnectAsync()
        {
            try
            {
                _isRunning = false;

                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

                if (_serverManager != null)
                {
                    _serverManager.Dispose();
                    _serverManager.ConnectionBotEvent -= OnConnectionBotEventAsync;
                    _serverManager.ConnectionServerBotEvent -= OnConnectionServerBotEventAsync;
                    _serverManager.ConnectionServerUserEvent -= OnConnectionServerUserEventAsync;
                    _serverManager.MessageServerChatEvent -= OnMessageServerChatEventAsync;
                    _serverManager.MessageServerCommandEvent -= OnMessageServerCommandEventAsync;
                    _serverManager.FollowEvent -= OnFollowEventAsync;
                    _serverManager.ColorChangeEvent -= OnColorChangeEventAsync;
                    _serverManager.ErrorEvent -= OnErrorEventAsync;
                    _serverManager = null;
                }

                if (_client != null)
                {
                    _client.Disconnect();
                    _client.OnWhisperReceived -= OnWhisperReceived;
                    _client.OnNewSubscriber -= OnNewSubscriber;
                    _client.OnConnected -= OnConnected;
                    _client.OnDisconnected -= OnDisconnected;
                }
            }
            catch (Exception ex)
            {
                await FireErrorEventAsync(this, new ErrorBotConnectEventArgs
                {
                    Bot = this,
                    ErrorConnectionEventType = ErrorConnectionEventType.DisconnectBot,
                    Exception = ex
                });
            }
        }

        public virtual async Task<IServer> JoinServerAsync(string userOrServerName)
        {
            try
            {
                return await _serverManager.JoinServerAsync(userOrServerName);
            }
            catch (Exception ex)
            {
                await FireErrorEventAsync(this, new ErrorBotConnectServerEventArgs
                {
                    Bot = this,
                    Exception = ex,
                    ServerName = userOrServerName,
                    ErrorConnectionEventType = ErrorConnectionEventType.ConnectToServer
                });
            }

            return null;
        }
        public virtual async Task LeaveServerAsync(IServer server)
        {
            try
            {
                _serverManager.LeaveServer(server);
            }
            catch (Exception ex)
            {
                await FireErrorEventAsync(this, new ErrorBotConnectServerEventArgs
                {
                    Bot = this,
                    ErrorConnectionEventType = ErrorConnectionEventType.DisconnectFromServer,
                    Exception = ex,
                    Server = server,
                    ServerName = server.ServerDTO.Username
                });
            }
        }

        public virtual void SendMessage(IServer server, string message, ChatColorPresets chatColor)
        {
            SendMessage(server, message, TwitchNETUtils.GetHexCode(chatColor));
        }
        public virtual void SendMessage(IServer server, string message, string hexCodeColor)
        {
            server.SendMessage(new MessageServerChat
            {
                Bot = this,
                ChatColor = hexCodeColor,
                MessageText = message,
                MessageType = MessageType.Sent,
                Server = server,
                Timestamp = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                User = _bot.UserDTO
            });
        }
        public virtual void SendCommand(IServer server, string message, ChatColorPresets chatColor)
        {
            SendCommand(server, message, TwitchNETUtils.GetHexCode(chatColor));
        }
        public virtual void SendCommand(IServer server, string message, string hexCodeColor)
        {
            server.SendCommand(new MessageServerCommand
            {
                Bot = this,
                ChatColor = hexCodeColor,
                MessageText = message,
                MessageType = Enums.MessageType.Sent,
                Server = server,
                Timestamp = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                User = _bot.UserDTO
            });
        }
        public virtual void SendWhisper(IUserDTO user, string message)
        {
            if (_whispersQueued.Count() > _botCredentials.MaxMessagesInQueue)
            {
                _whispersQueued.TryDequeue(out _);
            }

            _whispersQueued.Enqueue(new MessageWhisper
            {
                Bot = this,
                MessageText = message,
                MessageType = Enums.MessageType.Sent,
                Timestamp = DateTime.UtcNow,
                Id = Guid.NewGuid(),
                User = _bot.UserDTO,
            });
        }
        public virtual async Task<bool> SendMessageImmediateAsync(IServer server, string message)
        {
            if (_client != null &&
                _client.IsConnected &&
                _client.JoinedChannels.Any(s => s.Channel.Trim().ToLower() == server.ServerDTO.Username.Trim().ToLower()))
            {
                await server.SendMessageImmediateAsync(message);
                return true;
            }

            return false;
        }
        public virtual async Task<bool> SendCommandImmediateAsync(IServer server, string message)
        {
            if (_client != null &&
                _client.IsConnected &&
                _client.JoinedChannels.Any(s => s.Channel.Trim().ToLower() == server.ServerDTO.Username.Trim().ToLower()))
            {
                await server.SendCommandImmediateAsync(message);
                return true;
            }

            return false;
        }
        public virtual async Task<bool> SendWhisperImmediateAsync(IUserDTO user, string message)
        {
            try
            {
                if (_client != null &&
                    _client.IsConnected)
                {
                    _client.SendWhisper(user.Username.Trim().ToLower(), message);

                    await FireMessageWhisperEventAsync(this, new MessageWhisperEventArgs
                    {
                        Message = new MessageWhisper
                        {
                            Bot = this,
                            MessageText = message,
                            MessageType = MessageType.Sent,
                            User = user
                        },
                        MessageWhisperEventType = MessageWhisperEventType.SentImmediate
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                await FireErrorEventAsync(this, new ErrorMessageWhisperEventArgs
                {
                    Bot = this,
                    ErrorMessageEventType = ErrorMessageEventType.Sending,
                    ErrorMessageSendType = ErrorMessageSendType.Immediate,
                    Exception = ex,
                    Message = message
                });
            }

            return false;
        }

        protected virtual void OnConnected(object sender, OnConnectedArgs args)
        {
            Task.Run(async () =>
            {
                try
                {
                    _isRunning = true;

                    if (_timer != null)
                    {
                        _timer.Dispose();
                    }

                    _timer = new Timer(OnTimerTick, null, 0, TwitchNETUtils.GetRateLimit());

                    await FireConnectionBotEventAsync(sender, new ConnectionBotEventArgs
                    {
                        Bot = this,
                        ConnectionEventType = ConnectionEventType.ConnectedToTwitch,
                    });
                }
                catch (Exception ex)
                {
                    await FireErrorEventAsync(sender, new ErrorBotConnectEventArgs
                    {
                        Bot = this,
                        ErrorConnectionEventType = ErrorConnectionEventType.ConnectBot,
                        Exception = ex
                    });

                    await DisconnectAsync();
                }
            });
        }
        protected virtual void OnDisconnected(object sender, OnDisconnectedEventArgs args)
        {
            Task.Run(async () =>
            {
                try
                {
                    _isRunning = false;

                    if (_timer != null)
                    {
                        _timer.Dispose();
                        _timer = null;
                    }

                    if (_serverManager != null)
                    {
                        _serverManager.Dispose();
                    }

                    await FireConnectionBotEventAsync(sender, new ConnectionBotEventArgs
                    {
                        Bot = this,
                        ConnectionEventType = Enums.ConnectionEventType.DisconnectedFromTwitch,
                    });

                    if (_intervalReconnectMS > 0)
                    {
                        Thread.Sleep(_intervalReconnectMS);
                        await ConnectAsync(_botCredentials);
                    }
                }
                catch (Exception ex)
                {
                    await FireErrorEventAsync(sender, new ErrorBotConnectEventArgs
                    {
                        Bot = this,
                        ErrorConnectionEventType = ErrorConnectionEventType.DisconnectBot,
                        Exception = ex
                    });

                }
            });
        }
        protected virtual void OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    var user = await _twitchNetService.GetUserByTwitchUsernameAsync(e.WhisperMessage.Username.Trim().ToLower());

                    if (user == null)
                    {
                        user = await _twitchNetService.CreateUserAsync(new UserDTO
                        {
                            DisplayName = e.WhisperMessage.DisplayName,
                            TwitchId = e.WhisperMessage.ThreadId,
                            Username = e.WhisperMessage.Username,
                        });
                    }
                    else if (user.DisplayName != e.WhisperMessage.DisplayName ||
                        user.TwitchId != e.WhisperMessage.UserId ||
                        user.Username != e.WhisperMessage.Username)
                    {
                        user = await _twitchNetService.UpdateUserAsync(new UserDTO
                        {
                            DisplayName = e.WhisperMessage.DisplayName,
                            TwitchId = e.WhisperMessage.UserId,
                            Username = e.WhisperMessage.Username,
                            Id = user.Id
                        });
                    }

                    await FireMessageWhisperEventAsync(sender, new MessageWhisperEventArgs
                    {
                        Message = new MessageWhisper
                        {
                            Bot = this,
                            MessageText = e.WhisperMessage.Message,
                            User = user,
                            MessageType = Enums.MessageType.Received,
                            Id = Guid.NewGuid(),
                            Timestamp = DateTime.UtcNow,
                        },
                        MessageWhisperEventType = MessageWhisperEventType.Received
                    });
                }
                catch (Exception ex)
                {
                    await FireErrorEventAsync(sender, new ErrorMessageWhisperEventArgs
                    {
                        Bot = this,
                        ErrorMessageEventType = ErrorMessageEventType.Receiving,
                        Exception = ex,
                        ErrorMessageSendType = ErrorMessageSendType.Received,
                        Message = e.WhisperMessage.Message
                    });
                }
            });
        }
        protected virtual void OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            //if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            //    client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            //else
            //    client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
        }

        protected virtual void OnTimerTick(object state)
        {
            if (_wasServerMessage && _whispersQueued.Any())
            {
                _wasServerMessage = false;
                if (_whispersQueued.TryDequeue(out var message))
                {
                    Task.Run(async () =>
                    {
                        _client.SendWhisper(message.User.Username.Trim().ToLower(), message.MessageText);
                        await FireMessageWhisperEventAsync(this, new MessageWhisperEventArgs
                        {
                            Message = new MessageWhisper
                            {
                                Bot = this,
                                MessageText = message.MessageText,
                                MessageType = MessageType.Sent,
                                User = message.User,
                            },
                            MessageWhisperEventType = MessageWhisperEventType.Sent
                        });
                    });
                    return;
                }
            }
            _wasServerMessage = true;
            _serverManager.OnTimerTick();
        }

        protected virtual async Task OnMessageServerCommandEventAsync(object sender, MessageServerCommandEventArgs args)
        {
            await FireMessageServerCommandEventAsync(sender, args);
        }
        protected virtual async Task OnMessageServerChatEventAsync(object sender, MessageServerChatEventArgs args)
        {
            await FireMessageServerChatEventAsync(sender, args);
        }
        protected virtual async Task OnConnectionServerUserEventAsync(object sender, ConnectionServerUserEventArgs args)
        {
            await FireConnectionServerUserEventAsync(sender, args);
        }
        protected virtual async Task OnConnectionServerBotEventAsync(object sender, ConnectionServerBotEventArgs args)
        {
            await FireConnectionServerBotEventAsync(sender, args);
        }
        protected virtual async Task OnConnectionBotEventAsync(object sender, ConnectionBotEventArgs args)
        {
            await FireConnectionBotEventAsync(sender, args);
        }
        protected virtual async Task OnFollowEventAsync(object sender, FollowEventArgs args)
        {
            await FireFollowEventAsync(sender, args);
        }
        protected virtual async Task OnErrorEventAsync(object sender, ErrorEventArgs args)
        {
            await FireErrorEventAsync(sender, args);
        }
        protected virtual async Task OnColorChangeEventAsync(object sender, ServerChatColorChangeEventArgs args)
        {
            await FireColorChangeEventAsync(sender, args);
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

        public ICollection<IServer> GetServersConnected()
        {
            return _serverManager.Servers;
        }

        public virtual void Dispose()
        {
            DisconnectAsync().Wait();
        }

        public IBotDTO BotDTO
        {
            get
            {
                return _bot;
            }
        }
        public TwitchClient TwitchClient
        {
            get
            {
                return _client;
            }
        }
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
        }
    }
}

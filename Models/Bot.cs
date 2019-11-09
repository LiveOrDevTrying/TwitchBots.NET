using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twitch.NET.DAL;
using Twitch.NET.Enums;
using Twitch.NET.Events;
using Twitch.NET.Events.Args.ColorChange;
using Twitch.NET.Events.Args.Connection;
using Twitch.NET.Events.Args.Error;
using Twitch.NET.Events.Args.Message;
using Twitch.NET.Managers;
using Twitch.NET.Models.DTOs;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;
using Twitch.NET.Utils;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace Twitch.NET.Models
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
        protected FollowerService _followerService;
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

        public virtual void Connect(BotCredentials botCredentials)
        {
            try
            {
                _botCredentials = botCredentials;

                _twitchAPI = new TwitchAPI();
                _twitchAPI.Settings.ClientId = _botCredentials.ClientId;
                _twitchAPI.Settings.AccessToken = $"oauth:{_botCredentials.OAuthToken}";

                _botCredentials = botCredentials;
                var credentials = new ConnectionCredentials(_botCredentials.Username, _botCredentials.OAuthToken);

                Disconnect();

                _isRunning = true;

                _client = new TwitchClient();
                _client.Initialize(credentials);

                _client.OnWhisperReceived += OnWhisperReceived;
                _client.OnNewSubscriber += OnNewSubscriber;
                _client.OnConnected += OnConnected;
                _client.OnDisconnected += OnDisconnected;

                _serverManager = new ServerManager(_twitchNetService, _client, this, botCredentials.MaxMessagesInQueue);
                _serverManager.ConnectionBotEvent += OnConnectionBotEvent;
                _serverManager.ConnectionServerBotEvent += OnConnectionServerBotEvent;
                _serverManager.ConnectionServerUserEvent += OnConnectionServerUserEvent;
                _serverManager.MessageServerChatEvent += OnMessageServerChatEvent;
                _serverManager.MessageServerCommandEvent += OnMessageServerCommandEvent;
                _serverManager.ColorChangeEvent += OnColorChangeEvent;
                _serverManager.ErrorEvent += OnErrorEvent;

                _client.Connect();
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorBotConnectEventArgs
                {
                    Bot = this,
                    ErrorConnectionEventType = ErrorConnectionEventType.ConnectBot,
                    Exception = ex
                });
            }
        }
        public virtual void Disconnect()
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
                    _serverManager.ConnectionBotEvent -= OnConnectionBotEvent;
                    _serverManager.ConnectionServerBotEvent -= OnConnectionServerBotEvent;
                    _serverManager.ConnectionServerUserEvent -= OnConnectionServerUserEvent;
                    _serverManager.MessageServerChatEvent -= OnMessageServerChatEvent;
                    _serverManager.MessageServerCommandEvent -= OnMessageServerCommandEvent;
                    _serverManager.ColorChangeEvent -= OnColorChangeEvent;
                    _serverManager.ErrorEvent -= OnErrorEvent;
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

                StopFollowerService();
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorBotConnectEventArgs
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
                FireErrorEvent(this, new ErrorBotConnectServerEventArgs
                {
                    Bot = this,
                    Exception = ex,
                    ServerName = userOrServerName,
                    ErrorConnectionEventType = ErrorConnectionEventType.ConnectToServer
                });
            }

            return null;
        }
        public virtual void LeaveServer(IServer server)
        {
            try
            {
                _serverManager.LeaveServer(server);
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorBotConnectServerEventArgs
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
                User = _bot.User
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
                User = _bot.User
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
                User = _bot.User,
            });
        }
        public virtual bool SendMessageImmediate(IServer server, string message)
        {
            if (_client != null &&
                _client.IsConnected &&
                _client.JoinedChannels.Any(s => s.Channel.Trim().ToLower() == server.ServerDTO.Username.Trim().ToLower()))
            {
                server.SendMessageImmediate(message);
                return true;
            }

            return false;
        }
        public virtual bool SendCommandImmediate(IServer server, string message)
        {
            if (_client != null &&
                _client.IsConnected &&
                _client.JoinedChannels.Any(s => s.Channel.Trim().ToLower() == server.ServerDTO.Username.Trim().ToLower()))
            {
                server.SendCommandImmediate(message);
                return true;
            }

            return false;
        }
        public virtual bool SendWhisperImmediate(IUserDTO user, string message)
        {
            try
            {
                if (_client != null &&
                    _client.IsConnected)
                {
                    _client.SendWhisper(user.Username.Trim().ToLower(), message);

                    FireMessageWhisperEvent(this, new MessageWhisperEventArgs
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
                FireErrorEvent(this, new ErrorMessageWhisperEventArgs
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

        protected virtual void StartFollowerService()
        {
            StopFollowerService();

            _followerService = new FollowerService(_twitchAPI);
            _followerService.OnNewFollowersDetected += OnNewFollowerDetected;

        }
        protected virtual void StopFollowerService()
        {
            if (_followerService != null)
            {
                try
                {
                    _followerService.OnNewFollowersDetected -= OnNewFollowerDetected;
                    _followerService.Stop();
                    _followerService = null;
                }
                catch
                { }
            }
        }

        protected virtual void OnConnected(object sender, OnConnectedArgs args)
        {
            try
            {
                _isRunning = true;

                if (_timer != null)
                {
                    _timer.Dispose();
                }

                _timer = new Timer(OnTimerTick, null, 0, GetRateLimit());

                StartFollowerService();

                FireConnectionBotEvent(sender, new ConnectionBotEventArgs
                {
                    Bot = this,
                    ConnectionEventType = ConnectionEventType.ConnectedToTwitch,
                });
            }
            catch (Exception ex)
            {
                FireErrorEvent(sender, new ErrorBotConnectEventArgs
                {
                    Bot = this,
                    ErrorConnectionEventType = ErrorConnectionEventType.ConnectBot,
                    Exception = ex
                });

                Disconnect();
            }
        }
        protected virtual void OnDisconnected(object sender, OnDisconnectedEventArgs args)
        {
            try
            {
                _isRunning = false;

                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

                StopFollowerService();

                if (_serverManager != null)
                {
                    _serverManager.Dispose();
                }

                FireConnectionBotEvent(sender, new ConnectionBotEventArgs
                {
                    Bot = this,
                    ConnectionEventType = Enums.ConnectionEventType.DisconnectFromTwitch,
                });

                if (_intervalReconnectMS > 0)
                {
                    Thread.Sleep(_intervalReconnectMS);
                    Connect(_botCredentials);
                }
            }
            catch (Exception ex)
            {
                FireErrorEvent(sender, new ErrorBotConnectEventArgs
                {
                    Bot = this,
                    ErrorConnectionEventType = ErrorConnectionEventType.DisconnectBot,
                    Exception = ex
                });

            }
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

                    FireMessageWhisperEvent(sender, new MessageWhisperEventArgs
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
                    FireErrorEvent(sender, new ErrorMessageWhisperEventArgs
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
                    _client.SendWhisper(message.User.Username.Trim().ToLower(), message.MessageText);

                    FireMessageWhisperEvent(this, new MessageWhisperEventArgs
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
                }
            }
            _serverManager.OnTimerTick();
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
        protected virtual Task OnErrorEvent(object sender, ErrorEventArgs args)
        {
            FireErrorEvent(sender, args);
            return Task.CompletedTask;
        }
        protected virtual Task OnColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            FireColorChangeEvent(sender, args);
            return Task.CompletedTask;
        }

        protected virtual void OnNewFollowerDetected(object sender, OnNewFollowersDetectedArgs args)
        {
            throw new NotImplementedException();
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
        protected virtual void FireColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            ColorChangeEvent?.Invoke(sender, args);
        }
        protected virtual void FireErrorEvent(object sender, ErrorEventArgs args)
        {
            ErrorEvent?.Invoke(sender, args);
        }

        protected virtual int GetRateLimit()
        {
            // 20 messages each 30000 MS with 80% of total time to buffer
            return Convert.ToInt32(Math.Ceiling(30000f / 20f * 0.8f));
        }
        public ICollection<IServer> GetServersConnected()
        {
            return _serverManager.Servers;
        }

        public virtual void Dispose()
        {
            Disconnect();
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

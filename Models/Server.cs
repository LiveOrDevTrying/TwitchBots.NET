using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchBots.NET.DAL;
using TwitchBots.NET.Enums;
using TwitchBots.NET.Events;
using TwitchBots.NET.Events.Args.ColorChange;
using TwitchBots.NET.Events.Args.Connection;
using TwitchBots.NET.Events.Args.Error;
using TwitchBots.NET.Events.Args.Follows;
using TwitchBots.NET.Events.Args.Message;
using TwitchBots.NET.Models.DTOs;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;
using TwitchBots.NET.Utils;
using TwitchLib.Api.Interfaces;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;

namespace TwitchBots.NET.Models
{
    public class Server : IServer
    {
        protected readonly ITwitchNETDALService _twitchNetService;
        protected readonly TwitchClient _client;
        protected readonly IServerDTO _serverDTO;
        protected readonly IBot _bot;
        protected readonly ITwitchAPI _twitchAPI;

        protected ConcurrentQueue<IMessageServer> _messagesQueued =
            new ConcurrentQueue<IMessageServer>();
        protected FollowerService _followerService;
        protected bool _isWaitingForColorChange;
        protected int _maxMessagesInQueue;
        protected ChatColorPresets _botChatColor;

        public event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        public event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        public event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        public event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        public event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        public event TwitchNETEventHandler<FollowEventArgs> FollowEvent;
        public event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        public event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        public Server(ITwitchNETDALService twitchNETService,
            TwitchClient client,
            IBot bot,
            IServerDTO serverDTO,
            ITwitchAPI twitchAPI,
            int maxMessagesInQueue)
        {
            _twitchNetService = twitchNETService;
            _serverDTO = serverDTO;
            _bot = bot;
            _twitchAPI = twitchAPI;
            _maxMessagesInQueue = maxMessagesInQueue;

            _client = client;
            _client.OnUserJoined += OnUserJoined;
            _client.OnUserLeft += OnUserLeft;
            _client.OnJoinedChannel += OnJoinedChannel;
            _client.OnMessageReceived += OnMessageReceived;
            _client.OnNewSubscriber += OnNewSubscriber;
            _client.OnLeftChannel += OnLeaveChannel;
            _client.OnChatColorChanged += OnChatColorChanged;

            if (_client.IsConnected &&
                !_client.JoinedChannels.Any(s => s.Channel.Trim().ToLower() == serverDTO.Username.Trim().ToLower()))
            {
                _client.JoinChannel(_serverDTO.Username.Trim().ToLower());
            }
        }

        public virtual void SendMessage(IMessageServerChat message)
        {
            if (_messagesQueued.Count() > _maxMessagesInQueue)
            {
                _messagesQueued.TryDequeue(out _);
            }

            _messagesQueued.Enqueue(message);
        }
        public virtual async Task SendMessageImmediateAsync(string message)
        {
            try
            {
                _client.SendMessage(_serverDTO.Username.Trim().ToLower(), message);

                await FireMessageServerChatEventAsync(this, new MessageServerChatEventArgs
                {
                    Message = new MessageServerChat
                    {
                        Bot = _bot,
                        ChatColor = TwitchNETUtils.GetHexCode(_botChatColor),
                        MessageText = message,
                        MessageType = MessageType.Sent,
                        Server = this,
                        User = _bot.BotDTO.UserDTO,
                        Id = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                await FireErrorEventAsync(this, new ErrorMessageServerChatEventArgs
                {
                    Bot = _bot,
                    ChatColor = CurrentBotChatColorHex,
                    Exception = ex,
                    Message = message,
                    ErrorMessageEventType = ErrorMessageEventType.Sending,
                    ErrorMessageSendType = ErrorMessageSendType.Immediate
                });
            }
        }
        public virtual void SendCommand(IMessageServerCommand message)
        {
            if (_messagesQueued.Count() > _maxMessagesInQueue)
            {
                _messagesQueued.TryDequeue(out _);
            }

            _messagesQueued.Enqueue(message);
        }
        public virtual async Task SendCommandImmediateAsync(string command)
        {
            try
            {
                _client.SendMessage(_serverDTO.Username.Trim().ToLower(), $"/me {command}");

                await FireMessageServerCommandEventAsync(this, new MessageServerCommandEventArgs
                {
                    Message = new MessageServerCommand
                    {
                        Bot = _bot,
                        ChatColor = TwitchNETUtils.GetHexCode(_botChatColor),
                        MessageText = command,
                        MessageType = MessageType.Sent,
                        Server = this,
                        User = _bot.BotDTO.UserDTO,
                        Id = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                await FireErrorEventAsync(this, new ErrorMessageServerCommandEventArgs
                {
                    ChatColor = TwitchNETUtils.GetHexCode(_botChatColor),
                    ErrorMessageEventType = ErrorMessageEventType.Sending,
                    ErrorMessageSendType = ErrorMessageSendType.Immediate,
                    Exception = ex,
                    Message = command,
                    Bot = _bot
                });
            }
        }

        public virtual void OnTimerTick()
        {
            if (_isWaitingForColorChange)
            {
                return;
            }

            if (_messagesQueued.TryPeek(out var message))
            {
                if (TwitchNETUtils.GetChatColor(message.ChatColor) != _botChatColor)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Change the chat color
                            _botChatColor = TwitchNETUtils.GetChatColor(message.ChatColor);
                            ChangeChatColorExt.ChangeChatColor(_client,
                                _client.JoinedChannels.FirstOrDefault(s => s.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower()),
                                _botChatColor);

                            await FireServerChatColorChangeEventAsync(this, new ServerChatColorChangeEventArgs
                            {
                                Bot = _bot,
                                HexColorCode = TwitchNETUtils.GetHexCode(_botChatColor),
                                Server = this,
                                ServerChatColorChangeEventType = ServerChatColorChangeEventType.Initiated
                            });
                        }
                        catch (Exception ex)
                        {
                            await FireErrorEventAsync(this, new ErrorBotServerColorChangeEventArgs
                            {
                                Bot = _bot,
                                Exception = ex,
                                HexCode = TwitchNETUtils.GetHexCode(_botChatColor),
                                Server = this
                            });
                        }
                    });
                }
                else
                {
                    if (_messagesQueued.TryDequeue(out message))
                    {
                        Task.Run(async () =>
                        {
                            switch (message)
                            {
                                case IMessageServerChat e:
                                    try
                                    {
                                        _client.SendMessage(_serverDTO.Username.Trim().ToLower(), message.MessageText);

                                        await FireMessageServerChatEventAsync(this, new MessageServerChatEventArgs
                                        {
                                            Message = e
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        await FireErrorEventAsync(this, new ErrorMessageServerChatEventArgs
                                        {
                                            Bot = _bot,
                                            ChatColor = TwitchNETUtils.GetHexCode(_botChatColor),
                                            ErrorMessageEventType = ErrorMessageEventType.Sending,
                                            ErrorMessageSendType = ErrorMessageSendType.QueuedSent,
                                            Exception = ex,
                                            Message = message.MessageText
                                        });
                                    }
                                    break;
                                case IMessageServerCommand e:
                                    try
                                    {
                                        _client.SendMessage(_serverDTO.Username.Trim().ToLower(), $"/me {message.MessageText}");

                                        await FireMessageServerCommandEventAsync(this, new MessageServerCommandEventArgs
                                        {
                                            Message = e
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        await FireErrorEventAsync(this, new ErrorMessageServerCommandEventArgs
                                        {
                                            Bot = _bot,
                                            ChatColor = TwitchNETUtils.GetHexCode(_botChatColor),
                                            ErrorMessageEventType = ErrorMessageEventType.Sending,
                                            ErrorMessageSendType = ErrorMessageSendType.QueuedSent,
                                            Exception = ex,
                                            Message = message.MessageText
                                        });
                                    }
                                    break;
                                default:
                                    break;
                            }
                        });
                    }
                }
            }
        }
        public virtual async Task FollowReceived(IUserDTO[] users)
        {
            await FireFollowEventAsync(this, new FollowEventArgs
            {
                NewFollows = users,
                Server = this,
            });
        }

        protected virtual void OnMessageReceived(object sender, OnMessageReceivedArgs args)
        {
            if (args.ChatMessage.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower())
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var usersOnline = (await _twitchNetService.GetUsersOnlineAsync(this)).ToList();

                        var user = usersOnline.FirstOrDefault(s => s.Username.Trim().ToLower() == args.ChatMessage.Username.Trim().ToLower());

                        if (user == null)
                        {
                            user = await _twitchNetService.GetUserByTwitchIdAsync(args.ChatMessage.UserId);

                            if (user == null)
                            {
                                user = await _twitchNetService.CreateUserAsync(new UserDTO
                                {
                                    DisplayName = args.ChatMessage.DisplayName,
                                    TwitchId = args.ChatMessage.UserId,
                                    Username = args.ChatMessage.Username
                                });
                            }
                            else if (user.DisplayName != args.ChatMessage.DisplayName ||
                                user.TwitchId != args.ChatMessage.UserId ||
                                user.Username != args.ChatMessage.Username)
                            {
                                user = await _twitchNetService.UpdateUserAsync(new UserDTO
                                {
                                    DisplayName = args.ChatMessage.DisplayName,
                                    TwitchId = args.ChatMessage.UserId,
                                    Id = user.Id,
                                    Username = args.ChatMessage.Username
                                });
                            }

                            usersOnline.Add(user);
                            await _twitchNetService.CreateUsersOnlineAsync(this, usersOnline.ToArray());
                        }
                        else if (user.DisplayName != args.ChatMessage.DisplayName ||
                                user.TwitchId != args.ChatMessage.UserId ||
                                user.Username != args.ChatMessage.Username)
                        {
                            usersOnline.Remove(user);

                            user.Dispose();

                            var dto = await _twitchNetService.UpdateUserAsync(new UserDTO
                            {
                                DisplayName = args.ChatMessage.DisplayName,
                                TwitchId = args.ChatMessage.UserId,
                                Username = args.ChatMessage.Username,
                                Id = user.Id
                            });

                            usersOnline.Add(user);

                            await _twitchNetService.CreateUsersOnlineAsync(this, usersOnline.ToArray());
                        }

                        await FireMessageServerChatEventAsync(sender, new MessageServerChatEventArgs
                        {
                            Message = new MessageServerChat
                            {
                                Bot = _bot,
                                ChatColor = args.ChatMessage.ColorHex,
                                MessageText = args.ChatMessage.Message,
                                Server = this,
                                User = user,
                                Id = Guid.NewGuid(),
                                Timestamp = DateTime.UtcNow,
                                MessageType = Enums.MessageType.Received,
                            },
                        });
                    }
                    catch (Exception ex)
                    {
                        await FireErrorEventAsync(sender, new ErrorMessageServerChatEventArgs
                        {
                            Bot = _bot,
                            ChatColor = args.ChatMessage.ColorHex,
                            ErrorMessageEventType = ErrorMessageEventType.Receiving,
                            ErrorMessageSendType = ErrorMessageSendType.Received,
                            Exception = ex,
                            Message = args.ChatMessage.Message
                        });
                    }
                });
            }
        }
        protected virtual void OnJoinedChannel(object sender, OnJoinedChannelArgs args)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (args.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower())
                    {
                        await FireConnectionServerBotEventAsync(sender, new ConnectionServerBotEventArgs
                        {
                            Server = this,
                            ConnectionEventType = ConnectionEventType.ConnectedToTwitch,
                            ConnectionServerEventType = ConnectionServerEventType.ConnectedToServer,
                            Bot = _bot,
                        });
                    }

                    StartFollowerService();
                }
                catch (Exception ex)
                {
                    await FireErrorEventAsync(sender, new ErrorBotServerConnectEventArgs
                    {
                        Bot = _bot,
                        ErrorBotServerConnectEventType = ErrorBotServerConnectEventType.JoinChannel,
                        Exception = ex,
                        Server = this,
                    });
                }
            });

        }
        protected virtual void OnLeaveChannel(object sender, OnLeftChannelArgs args)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (args.Channel.Trim().ToLower() == args.Channel.Trim().ToLower())
                    {
                        await FireConnectionServerBotEventAsync(sender, new ConnectionServerBotEventArgs
                        {
                            ConnectionServerEventType = ConnectionServerEventType.DisconnectedFromServer,
                            ConnectionEventType = ConnectionEventType.DisconnectedFromTwitch,
                            Server = this,
                            Bot = _bot
                        });
                    }
                }
                catch (Exception ex)
                {
                    await FireErrorEventAsync(sender, new ErrorBotServerConnectEventArgs
                    {
                        Bot = _bot,
                        ErrorBotServerConnectEventType = ErrorBotServerConnectEventType.LeaveChannel,
                        Exception = ex,
                        Server = this
                    });
                }
            });
        }
        protected virtual void OnNewSubscriber(object sender, OnNewSubscriberArgs args)
        {
            //if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            //    client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            //else
            //    client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
        }
        protected virtual void OnUserJoined(object sender, OnUserJoinedArgs args)
        {
            if (args.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower())
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var usersOnline = (await _twitchNetService.GetUsersOnlineAsync(this)).ToList();

                        var user = usersOnline.FirstOrDefault(s => s.Username.Trim().ToLower() == args.Username.Trim().ToLower());

                        if (user == null)
                        {
                            user = await _twitchNetService.GetUserByTwitchUsernameAsync(args.Username.Trim().ToLower());

                            if (user == null)
                            {
                                user = await _twitchNetService.CreateUserAsync(new UserDTO
                                {
                                    DisplayName = args.Username,
                                    Username = args.Username
                                });
                            }

                            usersOnline.Add(user);

                            await _twitchNetService.CreateUsersOnlineAsync(this, usersOnline.ToArray());
                        }

                        await FireConnectionServerUserEventAsync(sender, new ConnectionServerUserEventArgs
                        {
                            ConnectionServerEventType = ConnectionServerEventType.ConnectedToServer,
                            ConnectionEventType = ConnectionEventType.DisconnectedFromTwitch,
                            Server = this,
                            User = user,
                        });
                    }
                    catch (Exception ex)
                    {
                        await FireErrorEventAsync(sender, new ErrorBotServerUserEventArgs
                        {
                            Bot = _bot,
                            Exception = ex,
                            Server = this,
                            Username = args.Username
                        });
                    }
                });
            }
        }
        protected virtual void OnUserLeft(object sender, OnUserLeftArgs args)
        {
            if (args.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower())
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var usersOnline = (await _twitchNetService.GetUsersOnlineAsync(this)).ToList();

                        if (usersOnline.Any(s => s.Username.Trim().ToLower() == args.Username.Trim().ToLower()))
                        {
                            var user = usersOnline.First(s => s.Username.Trim().ToLower() == args.Username.Trim().ToLower());
                            usersOnline.Remove(user);

                            user.Dispose();

                            await _twitchNetService.CreateUsersOnlineAsync(this, usersOnline.ToArray());

                            await FireConnectionServerUserEventAsync(sender, new ConnectionServerUserEventArgs
                            {
                                ConnectionServerEventType = ConnectionServerEventType.DisconnectedFromServer,
                                ConnectionEventType = ConnectionEventType.DisconnectedFromTwitch,
                                Server = this,
                                User = user,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await FireErrorEventAsync(sender, new ErrorBotServerUserEventArgs
                        {
                            Bot = _bot,
                            Exception = ex,
                            Server = this,
                            Username = args.Username,
                        });
                    }
                });
            }
        }
        protected virtual void OnChatColorChanged(object sender, OnChatColorChangedArgs args)
        {
            if (args.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower())
            {
                _isWaitingForColorChange = false;

                Task.Run(async () =>
                {
                    await FireServerChatColorChangeEventAsync(sender, new ServerChatColorChangeEventArgs
                    {
                        Bot = _bot,
                        HexColorCode = TwitchNETUtils.GetHexCode(_botChatColor),
                        Server = this,
                        ServerChatColorChangeEventType = ServerChatColorChangeEventType.Confirmed
                    });
                });
            }
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

        protected virtual void OnNewFollowerDetected(object sender, OnNewFollowersDetectedArgs args)
        {
            Task.Run(async () =>
            {
                try
                {
                    var users = new List<IUserDTO>();

                    foreach (var follow in args.NewFollowers)
                    {
                        var user = await _twitchNetService.GetUserByTwitchIdAsync(follow.FromUserId);
                        users.Add(user);
                    }

                    await FireFollowEventAsync(this, new FollowEventArgs
                    {
                        Server = this,
                        NewFollows = users.ToArray()
                    });
                }
                catch (Exception ex)
                {
                    await FireErrorEventAsync(sender, new ErrorFollowEventArgs
                    {
                        Exception = ex,
                        UserIdsFollowed = args.NewFollowers.Select(s => s.FromUserId).ToArray(),
                    });
                }
            });
        }
        protected virtual async Task OnFollowFromServerEvent(object sender, FollowEventArgs args)
        {
            await FireFollowEventAsync(sender, args);
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
        protected virtual async Task FireFollowEventAsync(object sender, FollowEventArgs args)
        {
            await FollowEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireServerChatColorChangeEventAsync(object sender, ServerChatColorChangeEventArgs args)
        {
            await ColorChangeEvent?.Invoke(sender, args);
        }
        protected virtual async Task FireErrorEventAsync(object sender, ErrorEventArgs args)
        {
            await ErrorEvent?.Invoke(sender, args);
        }

        public virtual void Dispose()
        {
            if (_client.JoinedChannels.Any(s => s.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower()))
            {
                _client.LeaveChannel(_serverDTO.Username.Trim().ToLower());
                _client.OnJoinedChannel -= OnJoinedChannel;
                _client.OnMessageReceived -= OnMessageReceived;
                _client.OnNewSubscriber -= OnNewSubscriber;
                _client.OnLeftChannel -= OnLeaveChannel;
                _client.OnChatColorChanged -= OnChatColorChanged;
                _client.OnUserJoined -= OnUserJoined;
                _client.OnUserLeft -= OnUserLeft;
            }
        }

        public virtual async Task<ICollection<IUserDTO>> GetUsersOnlineAsync()
        {
            return await _twitchNetService.GetUsersOnlineAsync(this);
        }

        public IServerDTO ServerDTO
        {
            get
            {
                return _serverDTO;
            }
        }
        public IBot Bot
        {
            get
            {
                return _bot;
            }
        }
        public ChatColorPresets CurrentBotChatColor
        {
            get
            {
                return _botChatColor;
            }
        }
        public string CurrentBotChatColorHex
        {
            get
            {
                return TwitchNETUtils.GetHexCode(_botChatColor);
            }
        }
        public int NumberMessagesQueued
        {
            get
            {
                return _messagesQueued.Count();
            }
        }
        public TwitchClient Client
        {
            get
            {
                return _client;
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.NET.DAL;
using Twitch.NET.Enums;
using Twitch.NET.Events;
using Twitch.NET.Events.Args.ColorChange;
using Twitch.NET.Events.Args.Connection;
using Twitch.NET.Events.Args.Error;
using Twitch.NET.Events.Args.Follows;
using Twitch.NET.Events.Args.Message;
using Twitch.NET.Models.DTOs;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;
using Twitch.NET.Utils;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;

namespace Twitch.NET.Models
{
    public class Server : IServer
    {
        protected readonly ITwitchNETDALService _twitchNetService;
        protected readonly TwitchClient _client;
        protected readonly IServerDTO _serverDTO;
        protected readonly IBot _bot;

        protected ConcurrentQueue<IMessageServer> _messagesQueued =
            new ConcurrentQueue<IMessageServer>();
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
            int maxMessagesInQueue)
        {
            _twitchNetService = twitchNETService;
            _serverDTO = serverDTO;
            _bot = bot;
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
        public virtual void SendMessageImmediate(string message)
        {
            try
            {
                _client.SendMessage(_serverDTO.Username.Trim().ToLower(), message);

                FireMessageServerChatEvent(this, new MessageServerChatEventArgs
                {
                    Message = new MessageServerChat
                    {
                        Bot = _bot,
                        ChatColor = TwitchNETUtils.GetHexCode(_botChatColor),
                        MessageText = message,
                        MessageType = MessageType.Sent,
                        Server = this,
                        User = _bot.BotDTO.User,
                        Id = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorMessageServerChatEventArgs
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
        public virtual void SendCommandImmediate(string command)
        {
            try
            {
                _client.SendMessage(_serverDTO.Username.Trim().ToLower(), $"/me {command}");

                FireMessageServerCommandEvent(this, new MessageServerCommandEventArgs
                {
                    Message = new MessageServerCommand
                    {
                        Bot = _bot,
                        ChatColor = TwitchNETUtils.GetHexCode(_botChatColor),
                        MessageText = command,
                        MessageType = MessageType.Sent,
                        Server = this,
                        User = _bot.BotDTO.User,
                        Id = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                FireErrorEvent(this, new ErrorMessageServerCommandEventArgs
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
        public virtual void FollowReceived(IUserDTO[] users)
        {
            FireFollowEvent(this, new FollowEventArgs
            {
                Server = this,
                NewFollows = users
            });
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
                    try
                    {
                        // Change the chat color
                        _botChatColor = TwitchNETUtils.GetChatColor(message.ChatColor);
                        ChangeChatColorExt.ChangeChatColor(_client,
                            _client.JoinedChannels.FirstOrDefault(s => s.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower()),
                            _botChatColor);

                        FireServerChatColorChangeEvent(this, new ServerChatColorChangeEventArgs
                        {
                            Bot = _bot,
                            HexColorCode = TwitchNETUtils.GetHexCode(_botChatColor),
                            Server = this,
                            ServerChatColorChangeEventType = ServerChatColorChangeEventType.Initiated
                        });
                    }
                    catch (Exception ex)
                    {
                        FireErrorEvent(this, new ErrorBotServerColorChangeEventArgs
                        {
                            Bot = _bot,
                            Exception = ex,
                            HexCode = TwitchNETUtils.GetHexCode(_botChatColor),
                            Server = this
                        });
                    }
                }
                else
                {
                    if (_messagesQueued.TryDequeue(out message))
                    {
                        switch (message)
                        {
                            case IMessageServerChat e:
                                try
                                {
                                    _client.SendMessage(_serverDTO.Username.Trim().ToLower(), message.MessageText);

                                    FireMessageServerChatEvent(this, new MessageServerChatEventArgs
                                    {
                                        Message = e
                                    });
                                }
                                catch (Exception ex)
                                {
                                    FireErrorEvent(this, new ErrorMessageServerChatEventArgs
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

                                    FireMessageServerCommandEvent(this, new MessageServerCommandEventArgs
                                    {
                                        Message = e
                                    });
                                }
                                catch (Exception ex)
                                {
                                    FireErrorEvent(this, new ErrorMessageServerCommandEventArgs
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
                    }
                }
            }
        }

        protected virtual void OnChatColorChanged(object sender, OnChatColorChangedArgs args)
        {
            if (args.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower())
            {
                _isWaitingForColorChange = false;

                FireServerChatColorChangeEvent(sender, new ServerChatColorChangeEventArgs
                {
                    Bot = _bot,
                    HexColorCode = TwitchNETUtils.GetHexCode(_botChatColor),
                    Server = this,
                    ServerChatColorChangeEventType = ServerChatColorChangeEventType.Confirmed
                });
            }
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

                        FireMessageServerChatEvent(sender, new MessageServerChatEventArgs
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
                    catch(Exception ex)
                    {
                        FireErrorEvent(sender, new ErrorMessageServerChatEventArgs
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
            try
            {
                if (args.Channel.Trim().ToLower() == _serverDTO.Username.Trim().ToLower())
                {
                    FireConnectionServerBotEvent(sender, new ConnectionServerBotEventArgs
                    {
                        Server = this,
                        ConnectionEventType = Enums.ConnectionEventType.ConnectedToServer,
                        Bot = _bot,
                    });
                }
            }
            catch (Exception ex)
            {
                FireErrorEvent(sender, new ErrorBotServerConnectEventArgs
                {
                    Bot = _bot,
                    ErrorBotServerConnectEventType = ErrorBotServerConnectEventType.JoinChannel,
                    Exception = ex,
                    Server = this
                });
            }
        }
        protected virtual void OnLeaveChannel(object sender, OnLeftChannelArgs args)
        {
            try
            {
                if (args.Channel.Trim().ToLower() == args.Channel.Trim().ToLower())
                {
                    FireConnectionServerBotEvent(sender, new ConnectionServerBotEventArgs
                    {
                        ConnectionEventType = Enums.ConnectionEventType.DisconnectFromServer,
                        Server = this,
                        Bot = _bot
                    });
                }
            }
            catch (Exception ex)
            {
                FireErrorEvent(sender, new ErrorBotServerConnectEventArgs
                {
                    Bot = _bot,
                    ErrorBotServerConnectEventType = ErrorBotServerConnectEventType.LeaveChannel,
                    Exception = ex,
                    Server = this
                });
            }
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

                        FireConnectionServerUserEvent(sender, new ConnectionServerUserEventArgs
                        {
                            ConnectionEventType = Enums.ConnectionEventType.ConnectedToServer,
                            Server = this,
                            User = user,
                        });
                    }
                    catch (Exception ex)
                    {
                        FireErrorEvent(sender, new ErrorBotServerUserEventArgs
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

                            FireConnectionServerUserEvent(sender, new ConnectionServerUserEventArgs
                            {
                                ConnectionEventType = Enums.ConnectionEventType.DisconnectFromServer,
                                Server = this,
                                User = user,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        FireErrorEvent(sender, new ErrorBotServerUserEventArgs
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
        protected virtual void FireFollowEvent(object sender, FollowEventArgs args)
        {
            FollowEvent?.Invoke(sender, args);
        }
        protected virtual void FireServerChatColorChangeEvent(object sender, ServerChatColorChangeEventArgs args)
        {
            ColorChangeEvent?.Invoke(sender, args);
        }
        protected virtual void FireErrorEvent(object sender, ErrorEventArgs args)
        {
            ErrorEvent?.Invoke(sender, args);
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
    }
}
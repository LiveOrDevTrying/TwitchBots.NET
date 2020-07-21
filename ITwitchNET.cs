using System;
using System.Threading.Tasks;
using TwitchBots.NET.Events;
using TwitchBots.NET.Events.Args.ColorChange;
using TwitchBots.NET.Events.Args.Connection;
using TwitchBots.NET.Events.Args.Error;
using TwitchBots.NET.Events.Args.Follows;
using TwitchBots.NET.Events.Args.Message;
using TwitchBots.NET.Models;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchBots.NET.Models.Interfaces;
using TwitchLib.Client.Enums;

namespace TwitchBots.NET
{
    public interface ITwitchNET : IDisposable
    {
        IBot[] Bots { get; }

        event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        event TwitchNETEventHandler<MessageWhisperEventArgs> MessageWhisperEvent;
        event TwitchNETEventHandler<FollowEventArgs> FollowEvent;
        event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        Task<IBot> ConnectBotAsync(BotCredentials credentials, int reconnectIntervalSec);
        Task<IServer> ConnectBotToServerAsync(IBot bot, string serverName);
        Task<bool> DisconnectBotFromServerAsync(IServer server);
        Task<bool> DisconnectBotAsync(IBot bot);

        Task<IBotDTO> GetBotAsync(Guid id);
        Task<IUserDTO> GetUserAsync(Guid id);

        void SendCommandToServer(IBot bot, IServer server, string message, ChatColorPresets chatColor);
        void SendCommandToServer(IBot bot, IServer server, string message, string hexCodeColor);
        Task SendCommandToServerImmediateAsync(IBot bot, IServer server, string message);
        void SendMessageToServer(IBot bot, IServer server, string message, ChatColorPresets chatColor);
        void SendMessageToServer(IBot bot, IServer server, string message, string colorHexCode);
        Task SendMessageToServerImmediateAsync(IBot bot, IServer server, string message);
    }
}
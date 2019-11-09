using System;
using System.Threading.Tasks;
using Twitch.NET.Events;
using Twitch.NET.Events.Args.ColorChange;
using Twitch.NET.Events.Args.Connection;
using Twitch.NET.Events.Args.Error;
using Twitch.NET.Events.Args.Message;
using Twitch.NET.Models;
using Twitch.NET.Models.DTOs.Interfaces;
using Twitch.NET.Models.Interfaces;
using TwitchLib.Client.Enums;

namespace Twitch.NET
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
        event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        Task<IBot> ConnectBotAsync(BotCredentials credentials, int reconnectIntervalSec);
        Task<IServer> ConnectBotToServerAsync(IBot bot, string serverName);
        Task<bool> DisconnectBotFromServerAsync(IServer server);
        Task<bool> DisconnectBotAsync(IBot bot);

        Task<IBotDTO> GetBotAsync(Guid id);
        Task<IBotDTO> CreateBotAsync(Guid userId);
        Task<IUserDTO> GetUserAsync(Guid id);
        Task<IUserDTO> CreateUserAsync(IUserDTO user);

        void SendCommandToServer(IBot bot, IServer server, string message, ChatColorPresets chatColor);
        void SendCommandToServer(IBot bot, IServer server, string message, string hexCodeColor);
        void SendCommandToServerImmediate(IBot bot, IServer server, string message);
        void SendMessageToServer(IBot bot, IServer server, string message, ChatColorPresets chatColor);
        void SendMessageToServer(IBot bot, IServer server, string message, string colorHexCode);
        void SendMessageToServerImmediate(IBot bot, IServer server, string message);
    }
}
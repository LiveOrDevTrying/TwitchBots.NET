using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchBots.NET.Events;
using TwitchBots.NET.Events.Args.ColorChange;
using TwitchBots.NET.Events.Args.Connection;
using TwitchBots.NET.Events.Args.Error;
using TwitchBots.NET.Events.Args.Follows;
using TwitchBots.NET.Events.Args.Message;
using TwitchBots.NET.Models.DTOs.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Enums;

namespace TwitchBots.NET.Models.Interfaces
{
    public interface IBot : IBaseInterface
    {
        bool IsRunning { get; }
        TwitchClient TwitchClient { get; }
        IBotDTO BotDTO { get; }

        event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        event TwitchNETEventHandler<MessageWhisperEventArgs> MessageWhisperEvent;
        event TwitchNETEventHandler<FollowEventArgs> FollowEvent;
        event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        Task ConnectAsync(BotCredentials botCredentials);
        Task DisconnectAsync();

        ICollection<IServer> GetServersConnected();

        Task<IServer> JoinServerAsync(string userOrServerName);
        Task LeaveServerAsync(IServer server);

        void SendCommand(IServer server, string message, ChatColorPresets chatColor);
        void SendCommand(IServer server, string message, string hexCodeColor);
        void SendMessage(IServer server, string message, ChatColorPresets chatColor);
        void SendMessage(IServer server, string message, string hexCodeColor);
        Task<bool> SendCommandImmediateAsync(IServer server, string message);
        Task<bool> SendMessageImmediateAsync(IServer server, string message);
        Task<bool> SendWhisperImmediateAsync(IUserDTO user, string message);
        void SendWhisper(IUserDTO user, string message);
    }
}
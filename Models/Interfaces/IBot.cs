using System.Collections.Generic;
using System.Threading.Tasks;
using Twitch.NET.Events;
using Twitch.NET.Events.Args.ColorChange;
using Twitch.NET.Events.Args.Connection;
using Twitch.NET.Events.Args.Error;
using Twitch.NET.Events.Args.Follows;
using Twitch.NET.Events.Args.Message;
using Twitch.NET.Models.DTOs.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Enums;

namespace Twitch.NET.Models.Interfaces
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

        void Connect(BotCredentials botCredentials);
        void Disconnect();

        ICollection<IServer> GetServersConnected();

        Task<IServer> JoinServerAsync(string userOrServerName);
        void LeaveServer(IServer server);

        void SendCommand(IServer server, string message, ChatColorPresets chatColor);
        void SendCommand(IServer server, string message, string hexCodeColor);
        bool SendCommandImmediate(IServer server, string message);
        void SendMessage(IServer server, string message, ChatColorPresets chatColor);
        void SendMessage(IServer server, string message, string hexCodeColor);
        bool SendMessageImmediate(IServer server, string message);
        void SendWhisper(IUserDTO user, string message);
        bool SendWhisperImmediate(IUserDTO user, string message);
    }
}
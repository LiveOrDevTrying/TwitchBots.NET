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
    public interface IServer : IBaseInterface
    {
        Task<ICollection<IUserDTO>> GetUsersOnlineAsync();

        event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        event TwitchNETEventHandler<FollowEventArgs> FollowEvent;
        event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        void OnTimerTick();

        void SendCommand(IMessageServerCommand command);
        Task SendCommandImmediateAsync(string command);
        void SendMessage(IMessageServerChat message);
        Task SendMessageImmediateAsync(string message);
        Task FollowReceived(IUserDTO[] users);

        IServerDTO ServerDTO { get; }
        IBot Bot { get; }
        ChatColorPresets CurrentBotChatColor { get; }
        string CurrentBotChatColorHex { get; }
        int NumberMessagesQueued { get; }
        TwitchClient Client { get; }
    }
}
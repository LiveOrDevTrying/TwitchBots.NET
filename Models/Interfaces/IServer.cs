using System.Collections.Generic;
using System.Threading.Tasks;
using Twitch.NET.Events;
using Twitch.NET.Events.Args.ColorChange;
using Twitch.NET.Events.Args.Connection;
using Twitch.NET.Events.Args.Error;
using Twitch.NET.Events.Args.Message;
using Twitch.NET.Models.DTOs.Interfaces;
using TwitchLib.Client.Enums;

namespace Twitch.NET.Models.Interfaces
{
    public interface IServer : IBaseInterface
    {
        Task<ICollection<IUserDTO>> GetUsersOnlineAsync();

        event TwitchNETEventHandler<ConnectionBotEventArgs> ConnectionBotEvent;
        event TwitchNETEventHandler<ConnectionServerBotEventArgs> ConnectionServerBotEvent;
        event TwitchNETEventHandler<ConnectionServerUserEventArgs> ConnectionServerUserEvent;
        event TwitchNETEventHandler<MessageServerChatEventArgs> MessageServerChatEvent;
        event TwitchNETEventHandler<MessageServerCommandEventArgs> MessageServerCommandEvent;
        event TwitchNETEventHandler<ServerChatColorChangeEventArgs> ColorChangeEvent;
        event TwitchNETEventHandler<ErrorEventArgs> ErrorEvent;

        void OnTimerTick();

        void SendCommand(IMessageServerCommand command);
        void SendCommandImmediate(string command);
        void SendMessage(IMessageServerChat message);
        void SendMessageImmediate(string message);
        void FollowReceived(IUserDTO[] users);

        IServerDTO ServerDTO { get; }
        IBot Bot { get; }
        ChatColorPresets CurrentBotChatColor { get; }
        string CurrentBotChatColorHex { get; }
        int NumberMessagesQueued { get; }
    }
}
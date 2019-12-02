namespace TwitchBots.NET.Models.Interfaces
{
    public interface IMessageServer : IMessage
    {
        string ChatColor { get; set; }
        IServer Server { get; set; }
    }
}
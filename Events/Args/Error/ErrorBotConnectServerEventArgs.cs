using TwitchBots.NET.Models.Interfaces;

namespace TwitchBots.NET.Events.Args.Error
{
    public class ErrorBotConnectServerEventArgs : ErrorBotConnectEventArgs
    {
        public IServer Server { get; set; }
        public string ServerName { get; set; }
    }
}

using Twitch.NET.Models.Interfaces;

namespace Twitch.NET.Events.Args.Error
{
    public class ErrorBotConnectServerEventArgs : ErrorBotConnectEventArgs
    {
        public IServer Server { get; set; }
        public string ServerName { get; set; }
    }
}

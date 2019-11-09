namespace Twitch.NET.Events.Args.Error
{
    public class ErrorBotServerColorChangeEventArgs : ErrorBotServerEventArgs
    {
        public string HexCode { get; set; }
    }
}

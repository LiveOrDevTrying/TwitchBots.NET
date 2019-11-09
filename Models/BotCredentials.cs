namespace Twitch.NET.Models
{
    public struct BotCredentials
    {
        public string Username { get; set; }
        public string ClientId { get; set; }
        public string OAuthToken { get; set; }
        public int MaxMessagesInQueue { get; set; }
    }
}

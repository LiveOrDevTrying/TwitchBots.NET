using System;

namespace Twitch.NET.Models
{
    public struct BotCredentials
    {
        public Guid BotId { get; set; }
        public string Username { get; set; }
        public string ClientId { get; set; }
        public string OAuthToken { get; set; }
        public int MaxMessagesInQueue { get; set; }
    }
}

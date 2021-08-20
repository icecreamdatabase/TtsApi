using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace TtsApi.Controllers.EventSubController
{
    public class EventSubHeaders
    {
        public string MessageId { get; }
        public DateTime MessageTimestamp { get; }
        
        
        public EventSubHeaders(IHeaderDictionary headers)
        {
            if (!headers.TryGetValue("Twitch-Eventsub-Message-Id", out StringValues messageId) ||
                !headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out StringValues messageTimestamp))
                throw new ArgumentException("Missing Request headers");

            MessageId = messageId;
            MessageTimestamp = DateTime.Parse(messageTimestamp);
        }
    }
}

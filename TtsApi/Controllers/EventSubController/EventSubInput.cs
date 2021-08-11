using System.Text.Json.Serialization;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes;

namespace TtsApi.Controllers.EventSubController
{
    public class EventSubInput
    {
        [JsonPropertyName("challenge")]
        public string Challenge { get; init; }
        
        [JsonPropertyName("subscription")]
        public Subscription Subscription { get; init; }
        
        [JsonPropertyName("event")]
        public Event Event { get; init; }
    }
}

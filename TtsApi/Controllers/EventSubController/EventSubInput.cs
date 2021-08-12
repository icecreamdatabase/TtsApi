using System.Text.Json.Serialization;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes;

namespace TtsApi.Controllers.EventSubController
{
    public class EventSubInput<TCondition, TEvent> : BareEventSubInput
    {
        [JsonPropertyName("event")]
        public TEvent Event { get; init; }
        
        [JsonPropertyName("subscription")]
        public new Subscription<TCondition> Subscription { get; init; }
    }
}

using System.Text.Json.Serialization;
using TtsApi.ExternalApis.Twitch.Eventsub.Datatypes;

namespace TtsApi.Controllers.EventSubController
{
    public class BareEventSubInput
    {
        [JsonPropertyName("challenge")]
        public string Challenge { get; init; }
        
        [JsonPropertyName("subscription")]
        public BareSubscription Subscription { get; init; }
    }
}

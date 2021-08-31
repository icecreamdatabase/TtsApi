using System.Text.Json.Serialization;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes;

namespace TtsApi.Controllers.EventSubController
{
    public class BareEventSubInput
    {
        [JsonPropertyName("challenge")]
        public string Challenge { get; init; }
        
        [JsonPropertyName("subscription")]
        public BareSubscription Subscription { get; init; }
        
        [JsonIgnore]
        public EventSubHeaders EventSubHeaders { get; set; }
    }
}

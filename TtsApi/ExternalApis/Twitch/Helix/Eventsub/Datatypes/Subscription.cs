using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes
{
    public class Subscription<T> : BareSubscription
    {
        [JsonPropertyName("condition")]
        public T Condition { get; init; }
    }
}

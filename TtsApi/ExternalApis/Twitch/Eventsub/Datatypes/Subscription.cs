using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes
{
    public class Subscription<T> : BareSubscription
    {
        [JsonPropertyName("condition")]
        public T Condition { get; init; }
    }
}

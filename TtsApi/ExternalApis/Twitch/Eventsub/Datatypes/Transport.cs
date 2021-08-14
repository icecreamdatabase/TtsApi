using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes
{
    public class Transport
    {
        [JsonPropertyName("method")]
        public string Method { get; init; }

        [JsonPropertyName("callback")]
        public string Callback { get; init; }

        [JsonPropertyName("secret")]
        public string Secret { get; init; }
    }
}

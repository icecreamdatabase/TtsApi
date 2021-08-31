using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes
{
    public class Request
    {
        [JsonPropertyName("type")]
        public string Type { get; init; }

        [JsonPropertyName("version")]
        public string Version { get; init; }

        [JsonPropertyName("condition")]
        public dynamic Condition { get; init; }

        [JsonPropertyName("transport")]
        public Transport Transport { get; init; } = Transport.Default;
    }
}

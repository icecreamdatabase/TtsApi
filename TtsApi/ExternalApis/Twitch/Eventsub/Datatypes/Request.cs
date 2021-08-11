using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes
{
    public class Request
    {
        [JsonPropertyName("type")]
        public string Type { get; init; }

        [JsonPropertyName("version")]
        public string Version { get; init; }

        [JsonPropertyName("condition")]
        public Condition Condition { get; init; }
        
        [JsonPropertyName("transport")]
        public Transport Transport { get; init; }
    }
}

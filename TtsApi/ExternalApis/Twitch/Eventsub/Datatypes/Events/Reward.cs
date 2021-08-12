using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Events
{
    public class Reward
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; }

        [JsonPropertyName("cost")]
        public int Cost { get; init; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; init; }
    }
}

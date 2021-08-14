using System.Text.Json.Serialization;
using TtsApi.Model;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes
{
    public class Transport
    {
        [JsonIgnore]
        public static readonly Transport Default = new()
        {
            Method = "webhook",
            Callback = "https://apitest.icdb.dev/eventsub",
            Secret = BotDataAccess.Hmacsha256Key
        };

        [JsonPropertyName("method")]
        public string Method { get; init; }

        [JsonPropertyName("callback")]
        public string Callback { get; init; }

        [JsonPropertyName("secret")]
        public string Secret { get; init; }
    }
}

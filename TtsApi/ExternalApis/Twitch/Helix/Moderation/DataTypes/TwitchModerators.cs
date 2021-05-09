using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.Moderation.DataTypes
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class TwitchModerators
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("user_login")]
        public string UserLogin { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }
    }
}

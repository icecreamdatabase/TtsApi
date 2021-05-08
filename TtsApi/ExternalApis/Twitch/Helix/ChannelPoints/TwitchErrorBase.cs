using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints
{
    public class TwitchErrorBase
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
        
        [JsonPropertyName("status")]
        public int? Status { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; }
        
    }
}

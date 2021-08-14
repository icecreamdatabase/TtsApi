using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions
{
    public class BroadcasterUserIdBase
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; init; }
    }
}

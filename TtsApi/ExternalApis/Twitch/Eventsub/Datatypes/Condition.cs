using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes
{
    public class Condition
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; init; }
        
        [JsonPropertyName("reward_id")]
        public string RewardId { get; init; }
    }
}

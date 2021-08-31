using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events
{
    public class ChannelPointsCustomRewardRedemptionEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; init; }

        [JsonPropertyName("broadcaster_user_login")]
        public string BroadcasterUserLogin { get; init; }

        [JsonPropertyName("broadcaster_user_name")]
        public string BroadcasterUserName { get; init; }
        
        [JsonPropertyName("user_id")]
        public string UserId { get; init; }
        
        [JsonPropertyName("user_login")]
        public string UserLogin { get; init; }
        
        [JsonPropertyName("user_name")]
        public string UserName { get; init; }
        
        [JsonPropertyName("user_input")]
        public string UserInput { get; init; }
        
        [JsonPropertyName("status")]
        public string Status { get; init; }
        
        [JsonPropertyName("reward")]
        public Reward Reward { get; init; }
        
        [JsonPropertyName("redeemed_at")]
        public string RedeemedAt { get; init; }
    }
}

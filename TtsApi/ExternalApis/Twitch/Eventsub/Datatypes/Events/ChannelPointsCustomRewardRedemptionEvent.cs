using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Events
{
    public class ChannelPointsCustomRewardRedemptionEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("broadcaster_user_id")]
        public string BroadCasterUserId { get; init; }

        [JsonPropertyName("broadcaster_user_login")]
        public string BroadCasterUserLogin { get; init; }

        [JsonPropertyName("broadcaster_user_name")]
        public string BroadCasterUserName { get; init; }
        
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

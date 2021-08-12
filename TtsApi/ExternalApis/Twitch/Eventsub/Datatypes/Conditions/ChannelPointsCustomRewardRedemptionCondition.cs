using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions
{
    public class ChannelPointsCustomRewardRedemptionCondition
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadCasterUserId { get; init; }

        [JsonPropertyName("reward_id")]
        public string RewardId { get; init; }
    }
}

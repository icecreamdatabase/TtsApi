using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions
{
    public class ChannelPointsCustomRewardRedemptionAddCondition
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; init; }

        [JsonPropertyName("reward_id")]
        public string RewardId { get; init; }
    }
}

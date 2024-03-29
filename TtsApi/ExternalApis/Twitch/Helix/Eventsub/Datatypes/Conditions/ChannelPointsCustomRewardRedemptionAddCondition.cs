﻿using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions
{
    public class ChannelPointsCustomRewardRedemptionAddCondition : BroadcasterUserIdBase
    {
        [JsonPropertyName("reward_id")]
        public string RewardId { get; init; }
    }
}

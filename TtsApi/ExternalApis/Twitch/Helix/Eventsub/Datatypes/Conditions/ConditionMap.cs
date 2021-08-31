using System;
using System.Collections.Generic;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Conditions
{
    public static class ConditionMap
    {
        public const string ChannelPointsCustomRewardRedemptionAdd =
            "channel.channel_points_custom_reward_redemption.add";

        public const string ChannelPointsCustomRewardRedemptionUpdate =
            "channel.channel_points_custom_reward_redemption.update";

        public const string UserAuthorizationRevoke = "user.authorization.revoke";

        public const string ChannelBan = "channel.ban";

        public static readonly Dictionary<Type, string> Map = new()
        {
            { typeof(ChannelPointsCustomRewardRedemptionAddCondition), ChannelPointsCustomRewardRedemptionAdd },
            { typeof(ChannelPointsCustomRewardRedemptionUpdateCondition), ChannelPointsCustomRewardRedemptionUpdate },
            { typeof(UserAuthorizationRevokeCondition), UserAuthorizationRevoke },
            { typeof(ChannelBanCondition), ChannelBan }
        };
    }
}

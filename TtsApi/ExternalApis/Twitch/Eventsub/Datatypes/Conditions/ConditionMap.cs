using System;
using System.Collections.Generic;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Conditions
{
    public static class ConditionMap
    {
        public const string ChannelPointsCustomRewardRedemptionAdd =
            "channel.channel_points_custom_reward_redemption.add";

        public const string ChannelPointsCustomRewardRedemptionUpdate =
            "channel.channel_points_custom_reward_redemption.update";

        public const string UserAuthorizationRevoke = "user.authorization.revoke";

        public static readonly Dictionary<Type, string> Map = new()
        {
            { typeof(ChannelPointsCustomRewardRedemptionAddCondition), ChannelPointsCustomRewardRedemptionAdd },
            { typeof(ChannelPointsCustomRewardRedemptionUpdateCondition), ChannelPointsCustomRewardRedemptionUpdate },
            { typeof(UserAuthorizationRevokeCondition), UserAuthorizationRevoke }
        };
    }
}

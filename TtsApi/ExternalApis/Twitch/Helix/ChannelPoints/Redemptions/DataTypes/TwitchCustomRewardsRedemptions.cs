using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions.DataTypes
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class TwitchCustomRewardsRedemptions : ChannelPointsCustomRewardRedemptionEvent
    {
    }
}

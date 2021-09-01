using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Redemptions.DataTypes
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class TwitchCustomRewardsRedemptionsInput
    {
        public static readonly TwitchCustomRewardsRedemptionsInput Fulfilled = new() { Status = "FULFILLED" };
        public static readonly TwitchCustomRewardsRedemptionsInput Canceled = new() { Status = "CANCELED" };

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}

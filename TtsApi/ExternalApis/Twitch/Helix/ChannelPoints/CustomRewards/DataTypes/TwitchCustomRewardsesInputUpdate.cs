using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.CustomRewards.DataTypes
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class TwitchCustomRewardsesInputUpdate : TwitchCustomRewardsInputCreate
    {
        [JsonPropertyName("is_paused")]
        public bool? IsPaused { get; set; }
    }
}

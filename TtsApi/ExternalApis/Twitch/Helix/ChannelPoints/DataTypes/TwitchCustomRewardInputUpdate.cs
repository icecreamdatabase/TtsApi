using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.DataTypes
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class TwitchCustomRewardInputUpdate : TwitchCustomRewardInputCreate
    {
        [JsonPropertyName("is_paused")]
        public bool? IsPaused { get; set; }
    }
}

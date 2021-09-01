#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.CustomRewards.DataTypes
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
    public class TwitchCustomRewardsInputCreate
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("prompt")]
        public string? Prompt { get; set; }

        [JsonPropertyName("cost")]
        public int? Cost { get; set; }

        [JsonPropertyName("is_enabled")]
        public bool? IsEnabled { get; set; }

        [JsonPropertyName("background_color")]
        public string? BackgroundColour { get; set; }

        [JsonPropertyName("is_user_input_required")]
        public bool? IsUserInputRequired { get; set; }

        [JsonPropertyName("is_max_per_stream_enabled")]
        public bool? IsMaxPerStreamEnabled { get; set; }

        [JsonPropertyName("max_per_user_per_stream")]
        public int? MaxPerUserPerStream { get; set; }

        [JsonPropertyName("is_global_cooldown_enabled")]
        public bool? IsGlobalCooldownEnabled { get; set; }

        [JsonPropertyName("global_cooldown_seconds")]
        public int? GlobalCooldownSeconds { get; set; }

        [JsonPropertyName("should_redemptions_skip_request_queue")]
        public bool? ShouldRedemptionsSkipRequestQueue { get; set; }
    }
}

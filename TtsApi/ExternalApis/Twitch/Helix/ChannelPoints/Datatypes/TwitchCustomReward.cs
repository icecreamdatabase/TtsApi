using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Datatypes
{
    public class TwitchCustomReward
    {
        [JsonPropertyName("broadcaster_id")]
        public string BroadcasterId { get; set; }

        [JsonPropertyName("broadcaster_login")]
        public string BroadcasterLogin { get; set; }

        [JsonPropertyName("broadcaster_name")]
        public string BroadcasterName { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("cost")]
        public int? Cost { get; set; }

        [JsonPropertyName("image")]
        public Image Image { get; set; }

        [JsonPropertyName("default_image")]
        public Image DefaultImage { get; set; }

        [JsonPropertyName("background_color")]
        public string BackgroundColour { get; set; }

        [JsonPropertyName("is_enabled")]
        public bool? IsEnabled { get; set; }

        [JsonPropertyName("is_user_input_required")]
        public bool? IsUserInputRequired { get; set; }

        [JsonPropertyName("max_per_stream_setting")]
        public MaxPerStreamSetting MaxPerStreamSetting { get; set; }

        [JsonPropertyName("max_per_user_per_stream_setting")]
        public MaxPerUserPerStreamSetting MaxPerUserPerStreamSetting { get; set; }

        [JsonPropertyName("global_cooldown_setting")]
        public GlobalCooldownSetting GlobalCooldownSetting { get; set; }

        [JsonPropertyName("is_paused")]
        public bool? IsPaused { get; set; }

        [JsonPropertyName("is_in_stock")]
        public bool? IsInStock { get; set; }

        [JsonPropertyName("should_redemptions_skip_request_queue")]
        public bool? ShouldRedemptionsSkipRequestQueue { get; set; }

        [JsonPropertyName("redemptions_redeemed_current_stream")]
        public int? RedemptionsRedeemedCurrentStream { get; set; }

        [JsonPropertyName("cooldown_expires_at")]
        public int? CooldownExpiresAt { get; set; }
    }

    public class Image
    {
        [JsonPropertyName("url_1x")]
        public string Url1X { get; set; }

        [JsonPropertyName("url_2x")]
        public string Url2X { get; set; }

        [JsonPropertyName("url_4x")]
        public string Url4X { get; set; }
    }

    public class MaxPerStreamSetting
    {
        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("max_per_stream")]
        public int MaxPerStream { get; set; }
    }

    public class MaxPerUserPerStreamSetting
    {
        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("max_per_user_per_stream")]
        public int MaxPerUserPerStream { get; set; }
    }

    public class GlobalCooldownSetting
    {
        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("global_cooldown_seconds")]
        public int GlobalCooldownSeconds { get; set; }
    }
}

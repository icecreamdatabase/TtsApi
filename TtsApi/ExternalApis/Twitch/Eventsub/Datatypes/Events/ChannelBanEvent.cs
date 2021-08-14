using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes.Events
{
    public class ChannelBanEvent
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadCasterUserId { get; init; }

        [JsonPropertyName("broadcaster_user_login")]
        public string BroadCasterUserLogin { get; init; }

        [JsonPropertyName("broadcaster_user_name")]
        public string BroadCasterUserName { get; init; }

        [JsonPropertyName("moderator_user_id")]
        public string ModeratorUserId { get; init; }

        [JsonPropertyName("moderator_user_login")]
        public string ModeratorUserLogin { get; init; }

        [JsonPropertyName("moderator_user_name")]
        public string ModeratorUserName { get; init; }

        [JsonPropertyName("user_id")]
        public string UserId { get; init; }

        [JsonPropertyName("user_login")]
        public string UserLogin { get; init; }

        [JsonPropertyName("user_name")]
        public string UserName { get; init; }

        [JsonPropertyName("reason")]
        public string Reason { get; init; }

        [JsonPropertyName("ends_at")]
        public string EndsAt { get; init; }

        [JsonPropertyName("is_permanent")]
        public bool IsPermanent { get; init; }
    }
}

﻿using System;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix.Eventsub.Datatypes.Events
{
    public class ChannelBanEvent
    {
        [JsonPropertyName("broadcaster_user_id")]
        public string BroadcasterUserId { get; init; }

        [JsonPropertyName("broadcaster_user_login")]
        public string BroadcasterUserLogin { get; init; }

        [JsonPropertyName("broadcaster_user_name")]
        public string BroadcasterUserName { get; init; }

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

        [JsonIgnore]
        public DateTime EndsAtDateTime => DateTime.Parse(EndsAt);

        [JsonPropertyName("is_permanent")]
        public bool IsPermanent { get; init; }
    }
}

﻿using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes
{
    public class BareSubscription
    {
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; }

        [JsonPropertyName("type")]
        public string Type { get; init; }

        [JsonPropertyName("version")]
        public string Version { get; init; }

        [JsonPropertyName("cost")]
        public int Cost { get; init; }
        
        [JsonPropertyName("transport")]
        public Transport Transport { get; init; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; init; }
    }
}
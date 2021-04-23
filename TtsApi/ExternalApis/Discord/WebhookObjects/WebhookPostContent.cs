using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Discord.WebhookObjects
{
    public class WebhookPostContent
    {
        [JsonIgnore]
        public LogChannel LogChannel { get; set; } = LogChannel.Main;

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonPropertyName("file")]
        public string FileContent { get; set; }

        [JsonPropertyName("embeds")]
        public List<WebhookEmbeds> Embeds { get; set; }

        [JsonPropertyName("payload_json")]
        public string PayloadJson { get; set; }
    }
}

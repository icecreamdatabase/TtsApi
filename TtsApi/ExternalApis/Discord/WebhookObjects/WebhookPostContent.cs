using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Discord.WebhookObjects
{
    public class WebhookPostContent
    {
        [JsonPropertyName("username")]
        public string Username;

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl;

        [JsonPropertyName("postContent")]
        public List<WebhookEmbeds> PostContent;
    }
}
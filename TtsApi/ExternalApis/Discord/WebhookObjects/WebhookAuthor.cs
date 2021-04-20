using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Discord.WebhookObjects
{
    public class WebhookAuthor
    {
        [JsonPropertyName("name")]
        public string Name;

        [JsonPropertyName("url")]
        public string Url;

        [JsonPropertyName("icon_url")]
        public string IconUrl;
    }
}
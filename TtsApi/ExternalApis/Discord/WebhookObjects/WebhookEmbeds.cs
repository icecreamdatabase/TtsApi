using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Discord.WebhookObjects
{
    public class WebhookEmbeds
    {
        [JsonPropertyName("title")]
        public string Title;

        [JsonPropertyName("description")]
        public string Description;

        [JsonPropertyName("url")]
        public string Url;

        [JsonPropertyName("timestamp")]
        public string Timestamp;

        [JsonPropertyName("color")]
        public int Color;

        [JsonPropertyName("footer")]
        public WebhookFooter Footer;

        [JsonPropertyName("author")]
        public WebhookAuthor Author;
    }
}
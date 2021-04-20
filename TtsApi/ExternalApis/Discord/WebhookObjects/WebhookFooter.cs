using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Discord.WebhookObjects
{
    public class WebhookFooter
    {
        [JsonPropertyName("text")]
        public string Text;

        [JsonPropertyName("icon_url")]
        public string IconUrl;
    }
}
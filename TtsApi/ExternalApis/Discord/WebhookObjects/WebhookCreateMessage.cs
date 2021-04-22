using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Discord.WebhookObjects
{
    public class WebhookCreateMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("nonce")]
        public string Nonce { get; set; }

        [JsonPropertyName("tts")]
        public bool Tts { get; set; }

        [JsonPropertyName("embeds")]
        public List<WebhookEmbeds> Embed { get; set; }
    }
}

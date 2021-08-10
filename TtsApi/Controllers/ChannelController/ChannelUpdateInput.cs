using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TtsApi.Controllers.ChannelController
{
    public class ChannelUpdateInput
    {
        [JsonPropertyName("maxIcrMessageLength")]
        [Range(50, 450)]
        public int? MaxIrcMessageLength { get; set; }

        [JsonPropertyName("maxMessageTimeSeconds")]
        [Range(0, 300)]
        public int? MaxMessageTimeSeconds { get; set; }
        
        [JsonPropertyName("maxTtsCharactersPerRequest")]
        [Range(50, 500)]
        public int? MaxTtsCharactersPerRequest { get; set; }

        [JsonPropertyName("minCooldown")]
        [Range(0, 300)]
        public int? MinCooldown { get; set; }

        [JsonPropertyName("timeoutCheckTime")]
        [Range(0, 15)]
        public int? TimeoutCheckTime { get; set; }

        [JsonPropertyName("ircMuted")]
        public bool? IrcMuted { get; set; }

        [JsonPropertyName("isQueueMessages")]
        public bool? IsQueueMessages { get; set; }

        [JsonPropertyName("allowNeuralVoices")]
        public bool? AllowNeuralVoices { get; set; }

        [JsonPropertyName("volume")]
        [Range(0, 100)]
        public int? Volume { get; set; }

        [JsonPropertyName("allModsAreEditors")]
        public bool? AllModsAreEditors { get; set; }
    }
}

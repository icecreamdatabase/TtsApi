using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Helix
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    public class DataHolder<T> : TwitchErrorBase
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; }
    }
}

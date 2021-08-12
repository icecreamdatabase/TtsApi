using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Twitch.Eventsub.Datatypes
{
    public class GetResponse<T>
    {
        [JsonPropertyName("data")]
        public Subscription<T>[] Data { get; init; }
        
        [JsonPropertyName("total")]
        public int Total { get; init; }
        
        [JsonPropertyName("total_cost")]
        public int TotalCost { get; init; }
        
        [JsonPropertyName("max_total_cost")]
        public int MaxTotalCost { get; init; }
        
        [JsonPropertyName("pagination")]
        public object Pagination { get; init; }
    }
}

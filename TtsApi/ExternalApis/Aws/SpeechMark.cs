using System.Text.Json.Serialization;

namespace TtsApi.ExternalApis.Aws;

public class SpeechMark
{
    [JsonPropertyName("time")]
    public int? Time { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("start")]
    public int? Start { get; set; }

    [JsonPropertyName("end")]
    public int? End { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

    public static async Task<List<SpeechMark>> ParseSpeechMarks(Stream stream)
    {
        using TextReader textReader = new StreamReader(stream);
        string text = await textReader.ReadToEndAsync();
        return text.Trim().Split('\n')
            .Select(line => JsonSerializer.Deserialize<SpeechMark>(line))
            .Where(speechMark => speechMark != null)
            .ToList()!;
    }
}

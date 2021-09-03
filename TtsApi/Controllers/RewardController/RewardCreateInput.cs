using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using Amazon.Polly;
using Amazon.Polly.Model;
using TtsApi.ExternalApis.Aws;

namespace TtsApi.Controllers.RewardController
{
    public class RewardCreateInput : IValidatableObject
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("cost")]
        public int Cost { get; set; }

        public string VoiceId { get; set; }
        public VoiceId GetVoiceId() => Amazon.Polly.VoiceId.FindValue(VoiceId);

        public string VoiceEngine { get; set; }
        public Engine GetVoiceEngine() => Amazon.Polly.Engine.FindValue(VoiceEngine);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (typeof(VoiceId).GetFields().All(info => info.Name != VoiceId))
            {
                string valid = string.Join(", ", typeof(VoiceId).GetFields().Select(info => info.Name));
                yield return new ValidationResult(
                    $"Not a valid {nameof(VoiceId)}. Options are: {valid}",
                    new[] { nameof(VoiceId) }
                );
            }
            else if (typeof(Engine).GetFields().All(info => info.Name != VoiceEngine) ||
                     (!Polly.VoicesData.FirstOrDefault(voice => voice.Id == VoiceId)?
                         .SupportedEngines.Contains(VoiceEngine.ToLowerInvariant()) ?? false))
            {
                string valid = string.Join(", ", Polly.VoicesData.First(voice => voice.Id == VoiceId).SupportedEngines);
                yield return new ValidationResult(
                    $"Not a valid {nameof(Engine)} for {nameof(VoiceEngine)}: {VoiceId}. Options are: {valid}",
                    new[] { nameof(VoiceEngine) }
                );
            }
        }
    }
}

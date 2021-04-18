using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Amazon.Polly;

namespace TtsApi.Controllers.SynthesizeSpeechController
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class SynthesizeSpeechInput : IValidatableObject
    {
        [StringLength(3000)]
        public string Text { get; set; }

        public string VoiceId { get; set; }
        public VoiceId GetVoiceId() => Amazon.Polly.VoiceId.FindValue(VoiceId);

        public string Engine { get; set; }
        public Engine GetEngine() => Amazon.Polly.Engine.FindValue(Engine);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (typeof(VoiceId).GetFields().All(info => info.Name != VoiceId))
            {
                string valid = string.Join(", ", typeof(VoiceId).GetFields().Select(info => info.Name));
                yield return new ValidationResult(
                    $"Not a valid {nameof(VoiceId)}. Options are: {valid}",
                    new[] {nameof(VoiceId)}
                );
            }

            if (typeof(Engine).GetFields().All(info => info.Name != Engine))
            {
                string valid = string.Join(", ", typeof(Engine).GetFields().Select(info => info.Name));
                yield return new ValidationResult(
                    $"Not a valid {nameof(Engine)}. Options are: {valid}",
                    new[] {nameof(Engine)}
                );
            }
        }
    }
}

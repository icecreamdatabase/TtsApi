#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using Amazon.Polly;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.DataTypes;

namespace TtsApi.Controllers.RedemptionController
{
    public class RedemptionUpdateInput : TwitchCustomRewardInputUpdate, IValidatableObject
    {
        public string? VoiceId { get; set; }
        public VoiceId GetVoiceId() => Amazon.Polly.VoiceId.FindValue(VoiceId);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(VoiceId) && typeof(VoiceId).GetFields().All(info => info.Name != VoiceId))
            {
                string valid = string.Join(", ", typeof(VoiceId).GetFields().Select(info => info.Name));
                yield return new ValidationResult(
                    $"Not a valid {nameof(VoiceId)}. Options are: {valid}",
                    new[] {nameof(VoiceId)}
                );
            }
        }
    }
}

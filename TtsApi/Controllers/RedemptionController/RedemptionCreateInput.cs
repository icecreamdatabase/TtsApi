using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TtsApi.Controllers.RedemptionController
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class RedemptionCreateInput : IValidatableObject
    {
        [JsonIgnore]
        private const string HexColourRegexPattern = @"^#(?:[0-9a-fA-F]{3}){1,2}$";

        private static readonly Regex HexColourRegex = new(
            HexColourRegexPattern,
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(100)
        );

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("cost")]
        public int Cost { get; set; }

        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; } = true;

        [JsonPropertyName("background_color")]
        public string BackgroundColour { get; set; }

        [JsonPropertyName("is_user_input_required")]
        public bool IsUserInputRequired { get; set; } = true;

        [JsonPropertyName("is_max_per_stream_enabled")]
        public bool IsMaxPerStreamEnabled { get; set; }

        [JsonPropertyName("max_per_user_per_stream")]
        public int MaxPerUserPerStream { get; set; }

        [JsonPropertyName("is_global_cooldown_enabled")]
        public bool IsGlobalCooldownEnabled { get; set; }

        [JsonPropertyName("global_cooldown_seconds")]
        public int GlobalCooldownSeconds { get; set; }

        [JsonPropertyName("should_redemptions_skip_request_queue")]
        public bool ShouldRedemptionsSkipRequestQueue { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(BackgroundColour) && !HexColourRegex.IsMatch(BackgroundColour))
                yield return new ValidationResult(
                    $"Not a valid HEX colour string: {BackgroundColour}",
                    new[] {nameof(BackgroundColour)}
                );
        }
    }
}

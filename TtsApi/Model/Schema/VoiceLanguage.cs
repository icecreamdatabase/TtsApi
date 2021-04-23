using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class VoiceLanguage
    {
        [Key]
        [Required]
        public string LanguageCode { get; set; }

        [Required]
        public string LanguageName { get; set; }

        public List<Voice> Voices { get; set; }
    }
}

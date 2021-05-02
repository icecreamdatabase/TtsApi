using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class Voice
    {
        [Key]
        [Required]
        public string VoiceId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        [ForeignKey("VoiceLanguage")]
        public string LanguageCode { get; set; }

        public virtual VoiceLanguage VoiceLanguage { get; set; }

        public virtual List<VoiceEngine> VoiceEngines { get; set; }

        public virtual List<Reward> Rewards { get; set; }

        public virtual List<AllowedConversationVoice> AllowedConversationVoices { get; set; }
    }
}

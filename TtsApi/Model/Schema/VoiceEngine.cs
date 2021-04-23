using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class VoiceEngine
    {
        [Key]
        [Required]
        public int EngineId { get; set; }

        [Required]
        public string EngineName { get; set; }

        public List<Voice> Voices { get; set; }
    }
}

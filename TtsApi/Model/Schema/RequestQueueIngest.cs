using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class RequestQueueIngest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Reward")]
        public string RewardId { get; set; }

        public virtual Reward Reward { get; set; }

        [Required]
        public string RequesterId { get; set; }

        [Required]
        public string RequesterDisplayName { get; set; }

        [Required]
        public bool IsSubOrMod { get; set; }

        [Required]
        public string RawMessage { get; set; }

        [Required]
        public string MessageId { get; set; }

        [Required]
        [Column(TypeName = "TIMESTAMP")]
        public DateTime RequestTimestamp { get; set; }
    }
}

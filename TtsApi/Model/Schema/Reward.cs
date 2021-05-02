using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class Reward
    {
        [Key]
        [Required]
        public string RewardId { get; set; }

        [Required]
        [ForeignKey("Channel")]
        public int ChannelId { get; set; }

        public virtual Channel Channel { get; set; }


        [Required]
        [ForeignKey("Voice")]
        public string VoiceId { get; set; }

        public virtual Voice Voice { get; set; }

        [Required]
        public bool IsConversation { get; set; }

        [Required]
        public bool IsSubOnly { get; set; }

        [Required]
        public int Cooldown { get; set; }

        public virtual List<RequestQueueIngest> RequestQueueIngests { get; set; }

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reward>(entity =>
            {
                entity.Property(e => e.IsConversation).HasDefaultValue(true);
                entity.Property(e => e.IsSubOnly).HasDefaultValue(false);
                entity.Property(e => e.Cooldown).HasDefaultValue(0);
            });
        }
    }
}

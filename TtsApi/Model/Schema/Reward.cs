using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using Amazon.Polly;
using Amazon.Polly.Model;
using Microsoft.EntityFrameworkCore;
using TtsApi.ExternalApis.Aws;

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
        public VoiceId VoiceId { get; set; }

        [JsonIgnore]
        public Voice Voice => Polly.VoicesData.First(v => v.Id == VoiceId);

        [Required]
        public Engine VoiceEngine { get; set; }

        [Required]
        public bool IsConversation { get; set; } = true;

        [Required]
        public float DefaultPlaybackSpeed { get; set; } = 1.0f;

        [Required]
        public bool IsSubOnly { get; set; } = false;

        [Required]
        public int Cooldown { get; set; } = 0;

        public virtual List<RequestQueueIngest> RequestQueueIngests { get; set; }

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reward>(entity =>
            {
                entity.Property(e => e.IsConversation).HasDefaultValue(true);
                entity.Property(e => e.IsSubOnly).HasDefaultValue(false);
                entity.Property(e => e.Cooldown).HasDefaultValue(0);
                entity.Property(e => e.DefaultPlaybackSpeed).HasDefaultValue(1.0f);
                entity.Property(e => e.VoiceId).HasConversion(
                    v => v.ToString(),
                    v => VoiceId.FindValue(v)
                );
                entity.Property(e => e.VoiceEngine).HasConversion(
                    e => e.ToString(),
                    e => Engine.FindValue(e)
                );
            });
        }
    }
}

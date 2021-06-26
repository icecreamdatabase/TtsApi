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
    public class AllowedConversationVoice
    {
        [Required]
        [ForeignKey("Channel")]
        public int ChannelId { get; set; }

        public virtual Channel Channel { get; set; }

        [Required]
        public VoiceId VoiceId { get; set; }

        [JsonIgnore]
        public Voice Voice => Polly.VoicesData.First(v => v.Id == VoiceId);

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AllowedConversationVoice>(entity =>
            {
                entity.HasKey(nameof(ChannelId), nameof(VoiceId));
                entity.Property(e => e.VoiceId).HasConversion(
                    v => v.ToString(),
                    v => VoiceId.FindValue(v)
                );
            });
        }
    }
}

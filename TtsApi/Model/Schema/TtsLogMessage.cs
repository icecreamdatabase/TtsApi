using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TtsApi.Model.Schema
{
    public enum MessageType
    {
        PlayedFully,
        Skipped,
        SkippedBeforePlaying,
        SkippedAfterTime,
        SkippedNoQueue,
        NotPlayedTimedOut,
        NotPlayedSubOnly,
        NotPlayedIsOnGlobalBlacklist,
        NotPlayedIsOnChannelBlacklist,
        FailedNoParts,
        BadWordFilter
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class TtsLogMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RewardId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [Required]
        public bool IsSubOrHigher { get; set; }

        [Required]
        public string RawMessage { get; set; }

        [Required]
        public string VoicesId { get; set; }

        [Required]
        public bool WasTimedOut { get; set; }

        [Required]
        public MessageType MessageType { get; set; }

        [Required]
        [Column(TypeName = "TIMESTAMP")]
        public DateTime RequestTimestamp { get; set; }

        [Required]
        public string RedemptionId { get; set; }

        [Required]
        public int CharacterCostStandard { get; set; } = 0;

        [Required]
        public int CharacterCostNeural { get; set; } = 0;

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            // modelBuilder.Entity<TtsLogMessage>()
            //     .HasOne(e => e.Reward)
            //     .WithMany(e => e.TtsLogMessages)
            //     .OnDelete(DeleteBehavior.ClientCascade);
            // Add this to Rewards in order to use it:
            // public virtual List<TtsLogMessage> TtsLogMessages { get; set; }

            modelBuilder.Entity<TtsLogMessage>(builder =>
            {
                builder.Property(p => p.MessageType).HasConversion(new EnumToStringConverter<MessageType>());
                builder.Property(p => p.CharacterCostStandard).HasDefaultValue(0);
                builder.Property(p => p.CharacterCostNeural).HasDefaultValue(0);
            });
        }

        public TtsLogMessage()
        {
        }

        public TtsLogMessage(RequestQueueIngest rqi, MessageType reason)
        {
            RewardId = rqi.RewardId;
            RoomId = rqi.Reward.ChannelId;
            RequesterId = rqi.RequesterId;
            IsSubOrHigher = rqi.IsSubOrHigher;
            RawMessage = rqi.RawMessage;
            VoicesId = rqi.Reward.VoiceId;
            WasTimedOut = rqi.WasTimedOut;
            MessageType = reason;
            RequestTimestamp = rqi.RequestTimestamp;
            RedemptionId = rqi.RedemptionId;
            // If skipped before even requesting we won't have a cost
            CharacterCostStandard = rqi.CharacterCostStandard ?? 0;
            CharacterCostNeural = rqi.CharacterCostNeural ?? 0;
        }
    }
}

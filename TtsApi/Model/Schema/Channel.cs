using System;
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
    public class Channel
    {
        [Key]
        [Required]
        public int RoomId { get; set; }

        [Required]
        public string ChannelName { get; set; }

        [Required]
        public bool Enabled { get; set; }

        [Required]
        public bool IsTwitchPartner { get; set; }

        [Required]
        public int MaxMessageLength { get; set; }

        [Required]
        public int MinCooldown { get; set; }

        [Required]
        public int TimeoutCheckTime { get; set; }

        [Required]
        [Column(TypeName = "TIMESTAMP")]
        public DateTime AddDate { get; set; }

        [Required]
        public bool IrcMuted { get; set; }

        [Required]
        public bool IsQueueMessages { get; set; }

        [Required]
        public int Volume { get; set; }

        [Required]
        public bool AllModsAreEditors { get; set; }

        public virtual List<Reward> Rewards { get; set; }

        public virtual List<AllowedConversationVoice> AllowedConversationVoices { get; set; }
        
        public virtual List<ChannelEditor> ChannelEditors { get; set; }

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.Property(e => e.Enabled).HasDefaultValue(true);
                entity.Property(e => e.IsTwitchPartner).HasDefaultValue(false);
                entity.Property(e => e.MaxMessageLength).HasDefaultValue(450);
                entity.Property(e => e.MinCooldown).HasDefaultValue(0);
                entity.Property(e => e.TimeoutCheckTime).HasDefaultValue(2);
                entity.Property(e => e.AddDate).ValueGeneratedOnAdd();
                entity.Property(e => e.IrcMuted).HasDefaultValue(false);
                entity.Property(e => e.IsQueueMessages).HasDefaultValue(true);
                entity.Property(e => e.Volume).HasDefaultValue(100);
                entity.Property(e => e.AllModsAreEditors).HasDefaultValue(true);
            });
        }
    }
}

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
        public bool Enabled { get; set; } = true;

        [Required]
        public bool IsTwitchPartner { get; set; } = false;

        [Required]
        public int MaxIrcMessageLength { get; set; } = 450;

        [Required]
        public int MaxMessageTimeSeconds { get; set; } = 0;

        [Required]
        public int MaxTtsCharactersPerRequest { get; set; } = 500;

        [Required]
        public int MinCooldown { get; set; } = 0;

        [Required]
        public int TimeoutCheckTime { get; set; } = 2;

        [Required]
        [Column(TypeName = "TIMESTAMP")]
        public DateTime AddDate { get; set; }

        [Required]
        public bool IrcMuted { get; set; } = false;

        [Required]
        public bool IsQueueMessages { get; set; } = true;

        [Required]
        public bool AllowNeuralVoices { get; set; } = false;

        [Required]
        public int Volume { get; set; } = 100;

        [Required]
        public bool AllModsAreEditors { get; set; } = false;

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public virtual List<Reward> Rewards { get; set; }

        public virtual List<AllowedConversationVoice> AllowedConversationVoices { get; set; }

        public virtual List<ChannelEditor> ChannelEditors { get; set; }
        
        public virtual List<ChannelUserBlacklist> ChannelUserBlacklist { get; set; }

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.Property(e => e.Enabled).HasDefaultValue(true);
                entity.Property(e => e.IsTwitchPartner).HasDefaultValue(false);
                entity.Property(e => e.MaxIrcMessageLength).HasDefaultValue(450);
                entity.Property(e => e.MaxMessageTimeSeconds).HasDefaultValue(0);
                entity.Property(e => e.MaxTtsCharactersPerRequest).HasDefaultValue(500);
                entity.Property(e => e.MinCooldown).HasDefaultValue(0);
                entity.Property(e => e.TimeoutCheckTime).HasDefaultValue(2);
                entity.Property(e => e.AddDate).ValueGeneratedOnAdd();
                entity.Property(e => e.IrcMuted).HasDefaultValue(false);
                entity.Property(e => e.IsQueueMessages).HasDefaultValue(true);
                entity.Property(e => e.AllowNeuralVoices).HasDefaultValue(false);
                entity.Property(e => e.Volume).HasDefaultValue(100);
                entity.Property(e => e.AllModsAreEditors).HasDefaultValue(false);
            });
        }
    }
}

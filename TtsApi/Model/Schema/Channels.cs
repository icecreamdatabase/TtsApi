using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Channels
    {
        public int RoomId { get; set; }
        public string ChannelName { get; set; }
        public bool Enabled { get; set; }
        public bool IsTwitchPartner { get; set; }
        public int MaxMessageLength { get; set; }
        public int MinCooldown { get; set; }
        public int TimeoutCheckTime { get; set; }

        [Column(TypeName = "TIMESTAMP")]
        public DateTime AddDate { get; set; }

        public bool IrcMuted { get; set; }
        public bool IsQueueMessages { get; set; }
        public int Volume { get; set; }
        public bool AllModsAreEditors { get; set; }


        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channels>(entity =>
            {
                entity.HasKey(e => e.RoomId);

                entity.Property(e => e.RoomId).IsRequired();
                entity.Property(e => e.ChannelName).IsRequired();
                entity.Property(e => e.Enabled).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.IsTwitchPartner).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.MaxMessageLength).IsRequired().HasDefaultValue(450);
                entity.Property(e => e.MinCooldown).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.TimeoutCheckTime).IsRequired().HasDefaultValue(2);
                entity.Property(e => e.AddDate).IsRequired().ValueGeneratedOnAdd();
                entity.Property(e => e.IrcMuted).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.IsQueueMessages).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.Volume).IsRequired().HasDefaultValue(100);
                entity.Property(e => e.AllModsAreEditors).IsRequired().HasDefaultValue(true);
            });
        }
    }
}

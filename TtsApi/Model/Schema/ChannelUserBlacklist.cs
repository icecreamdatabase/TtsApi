﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class ChannelUserBlacklist
    {
        [Required]
        [ForeignKey("Channel")]
        public int ChannelId { get; set; }

        public virtual Channel Channel { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [Required]
        [Column(TypeName = "TIMESTAMP")]
        public DateTime AddDate { get; set; }
        
        [Column(TypeName = "TIMESTAMP")]
        public DateTime? UntilDate { get; set; }

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChannelUserBlacklist>(entity =>
            {
                entity.HasKey(nameof(ChannelId), nameof(UserId));
                entity.Property(e => e.AddDate).ValueGeneratedOnAdd();
            });
        }
    }
}
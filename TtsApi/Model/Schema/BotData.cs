using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class BotData
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BotData>(entity =>
            {
                entity.Property(e => e.LastUpdated).ValueGeneratedOnAddOrUpdate();
            });
        }
    }
}

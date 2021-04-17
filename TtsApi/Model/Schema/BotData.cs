using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class BotData
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime LastUpdated { get; set; }
        
        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BotData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired();
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.LastUpdated).ValueGeneratedOnAddOrUpdate();
            });
        }
    }
}

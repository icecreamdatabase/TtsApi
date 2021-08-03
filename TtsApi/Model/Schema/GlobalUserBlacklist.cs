using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class GlobalUserBlacklist
    {
        [Key]
        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "DATETIME")]
        public DateTime AddDate { get; set; }

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GlobalUserBlacklist>(entity =>
            {
                entity.Property(e => e.AddDate)
                    .HasDefaultValueSql("UTC_TIMESTAMP()")
                    .ValueGeneratedOnAdd();
            });
        }
    }
}

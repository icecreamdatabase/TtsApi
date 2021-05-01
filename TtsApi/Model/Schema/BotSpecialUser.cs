using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace TtsApi.Model.Schema
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class BotSpecialUser
    {
        [Key]
        [Required]
        public int UserId { get; set; }

        [Required]
        public bool IsIrcBot { get; set; }

        [Required]
        public bool IsBotOwner { get; set; }

        [Required]
        public bool IsBotAdmin { get; set; }

        protected internal static void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BotSpecialUser>(entity =>
            {
                entity.Property(e => e.IsIrcBot).HasDefaultValue(false);
                entity.Property(e => e.IsBotOwner).HasDefaultValue(false);
                entity.Property(e => e.IsBotAdmin).HasDefaultValue(false);
            });
        }
    }
}

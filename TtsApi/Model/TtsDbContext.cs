using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using TtsApi.Model.Schema;

namespace TtsApi.Model
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // They are used by DbContext
    public class TtsDbContext : DbContext
    {
        public DbSet<BotData> BotData { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Voice> Voices { get; set; }
        public DbSet<VoiceLanguage> VoicesLanguages { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<AllowedConversationVoice> AllowedConversationVoices { get; set; }
        public DbSet<ChannelEditor> ChannelEditors { get; set; }
        public DbSet<RequestQueueIngest> RequestQueueIngest { get; set; }
        public DbSet<BotSpecialUser> BotSpecialUsers { get; set; }

        public TtsDbContext(DbContextOptions<TtsDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Schema.BotData.BuildModel(modelBuilder);
            Schema.Channel.BuildModel(modelBuilder);
            Schema.Reward.BuildModel(modelBuilder);
            Schema.AllowedConversationVoice.BuildModel(modelBuilder);
            Schema.ChannelEditor.BuildModel(modelBuilder);
            Schema.BotSpecialUser.BuildModel(modelBuilder);
        }
    }
}

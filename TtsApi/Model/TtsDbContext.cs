using Microsoft.EntityFrameworkCore;
using TtsApi.Model.Schema;

namespace TtsApi.Model
{
    public class TtsDbContext : DbContext
    {
        public DbSet<BotData> BotData { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Voice> Voices { get; set; }
        public DbSet<VoiceLanguage> VoicesLanguages { get; set; }

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
        }
    }
}

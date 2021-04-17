using Microsoft.EntityFrameworkCore;
using TtsApi.Model.Schema;

namespace TtsApi.Model
{
    public class TtsDbContext : DbContext
    {
        public DbSet<BotData> BotData { get; set; }
        public DbSet<Channels> Channels { get; set; }

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
            Schema.Channels.BuildModel(modelBuilder);

            //modelBuilder.Entity<Publisher>(entity =>
            //{
            //    entity.HasKey(e => e.ID);
            //    entity.Property(e => e.Name).IsRequired();
            //});

            //modelBuilder.Entity<Book>(entity =>
            //{
            //    entity.HasKey(e => e.ISBN);
            //    entity.Property(e => e.Title).IsRequired();
            //    entity.HasOne(d => d.Publisher)
            //        .WithMany(p => p.Books);
            //});
        }
    }
}

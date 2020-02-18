using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace sc2dsstats.test
{
    public class DSReplayContext : DbContext
    {
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddFilter((c, l) => l == LogLevel.Information && !c.EndsWith("Connection")));

        public DbSet<DSUnit> DSUnits { get; set; }
        public DbSet<DSPlayer> DSPlayers { get; set; }
        public DbSet<PLDuplicate> PLDuplicates { get; set; }
        public DbSet<DSReplay> DSReplays { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseMySQL("server=192.168.178.28;port=3366;database=sc2dsstats_replaysfiltered;user=pax;password=test123");
            optionsBuilder
                .UseMySql("server=192.168.178.28;port=3366;database=sc2dsstats.replays;user=pax;password=test123")
                .UseLoggerFactory(_loggerFactory);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DSReplay>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(b => b.HASH)
                    .HasMaxLength(32)
                    .IsFixedLength();
                entity.HasIndex(b => b.HASH)
                    .IsUnique();
            });

            modelBuilder.Entity<DSPlayer>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.HasOne(d => d.DSReplay)
                  .WithMany(p => p.DSPlayer);
                entity.Property(b => b.NAME).IsUnicode();
            });

            modelBuilder.Entity<DSUnit>(entitiy =>
            {
                entitiy.HasKey(e => e.ID);
                entitiy.HasOne(d => d.DSPlayer)
                    .WithMany(p => p.DSUnit);
            });

            modelBuilder.Entity<PLDuplicate>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.HasOne(d => d.DSReplay)
                    .WithMany(p => p.PLDuplicate);
                entity.Property(b => b.Hash)
                    .HasMaxLength(64)
                    .IsFixedLength();
            });
        }
    }
}

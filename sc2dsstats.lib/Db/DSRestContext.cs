using Microsoft.EntityFrameworkCore;
using sc2dsstats.lib.Models;
using System;

namespace sc2dsstats.lib.Db
{
    public class DSRestContext : DbContext
    {
        //private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddFilter((c, l) => l == LogLevel.Information && !c.EndsWith("Connection")));

        public DSRestContext(DbContextOptions<DSRestContext> options)
            : base(options)
        {
            Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        }

        public DbSet<DSRestPlayer> DSRestPlayers { get; set; }
        public DbSet<DSRestUpload> DSRestUploads { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DSRestPlayer>(entity =>
            {
                entity.ToTable("DSRestPlayer");
                entity.HasKey(e => e.ID);
                entity.Property(p => p.Name)
                    .HasMaxLength(64)
                    .IsFixedLength();
                entity.HasIndex(b => b.Name)
                ;
            });

            modelBuilder.Entity<DSRestUpload>(entity =>
            {
                entity.ToTable("DSRestUpload");
                entity.HasKey(e => e.ID);
                entity.HasOne(o => o.DSRestPlayer)
                .WithMany(m => m.Uploads);
            });

        }

    }

}
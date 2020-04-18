using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace sc2dsstats.lib.Db.Models
{
    public partial class sc2dsstats_replaysContext : DbContext
    {
        public sc2dsstats_replaysContext()
        {
        }

        public sc2dsstats_replaysContext(DbContextOptions<sc2dsstats_replaysContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Dsplayers> Dsplayers { get; set; }
        public virtual DbSet<Dsreplays> Dsreplays { get; set; }
        public virtual DbSet<Dsunits> Dsunits { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("server=127.0.0.1;port=3306;database=sc2dsstats_replays;user=pax;password=test123", x => x.ServerVersion("5.7.29-mysql"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dsplayers>(entity =>
            {
                entity.ToTable("DSPlayers");

                entity.HasIndex(e => e.DsreplayId);

                entity.HasIndex(e => e.Race);

                entity.HasIndex(e => new { e.Race, e.Opprace });

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Army)
                    .HasColumnName("ARMY")
                    .HasColumnType("int(11)");

                entity.Property(e => e.DsreplayId)
                    .HasColumnName("DSReplayID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Gas)
                    .HasColumnName("GAS")
                    .HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.Income)
                    .HasColumnName("INCOME")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Killsum)
                    .HasColumnName("KILLSUM")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.Property(e => e.Opprace)
                    .HasColumnName("OPPRACE")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.Property(e => e.Pduration)
                    .HasColumnName("PDURATION")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Pos)
                    .HasColumnName("POS")
                    .HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.Race)
                    .HasColumnName("RACE")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.Property(e => e.Realpos)
                    .HasColumnName("REALPOS")
                    .HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.Team)
                    .HasColumnName("TEAM")
                    .HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.Win).HasColumnName("WIN");

                entity.HasOne(d => d.Dsreplay)
                    .WithMany(p => p.Dsplayers)
                    .HasForeignKey(d => d.DsreplayId);
            });

            modelBuilder.Entity<Dsreplays>(entity =>
            {
                entity.ToTable("DSReplays");

                entity.HasIndex(e => e.Hash)
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Duration)
                    .HasColumnName("DURATION")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Gamemode)
                    .HasColumnName("GAMEMODE")
                    .HasColumnType("longtext")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.Property(e => e.Gametime).HasColumnName("GAMETIME");

                entity.Property(e => e.Hash)
                    .HasColumnName("HASH")
                    .HasColumnType("char(32)")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.Property(e => e.Isbrawl).HasColumnName("ISBRAWL");

                entity.Property(e => e.Maxkillsum)
                    .HasColumnName("MAXKILLSUM")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Maxleaver)
                    .HasColumnName("MAXLEAVER")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Minarmy)
                    .HasColumnName("MINARMY")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Minincome)
                    .HasColumnName("MININCOME")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Minkillsum)
                    .HasColumnName("MINKILLSUM")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Playercount)
                    .HasColumnName("PLAYERCOUNT")
                    .HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.Replay)
                    .HasColumnName("REPLAY")
                    .HasColumnType("longtext")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.Property(e => e.Replaypath)
                    .HasColumnName("REPLAYPATH")
                    .HasColumnType("longtext")
                    .HasCharSet("latin1")
                    .HasCollation("latin1_swedish_ci");

                entity.Property(e => e.Reported)
                    .HasColumnName("REPORTED")
                    .HasColumnType("tinyint(3) unsigned");

                entity.Property(e => e.Version)
                    .HasColumnName("VERSION")
                    .HasColumnType("longtext")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.Property(e => e.Winner)
                    .HasColumnName("WINNER")
                    .HasColumnType("tinyint(4)");
            });

            modelBuilder.Entity<Dsunits>(entity =>
            {
                entity.ToTable("DSUnits");

                entity.HasIndex(e => e.DsplayerId);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Bp)
                    .HasColumnName("BP")
                    .HasColumnType("longtext")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.Property(e => e.Count).HasColumnType("int(11)");

                entity.Property(e => e.DsplayerId)
                    .HasColumnName("DSPlayerID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Name)
                    .HasColumnType("longtext")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_general_ci");

                entity.HasOne(d => d.Dsplayer)
                    .WithMany(p => p.Dsunits)
                    .HasForeignKey(d => d.DsplayerId);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

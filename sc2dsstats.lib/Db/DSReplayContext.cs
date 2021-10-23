using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;

namespace sc2dsstats.lib.Db
{
    public class DSReplayContext : DbContext
    {
        //private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddFilter((c, l) => l == LogLevel.Information && !c.EndsWith("Connection")));

        public DSReplayContext(DbContextOptions<DSReplayContext> options)
            : base(options)
        {
            Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        }

        public DbSet<DSUnit> DSUnits { get; set; }
        public DbSet<DSPlayer> DSPlayers { get; set; }
        public DbSet<DSReplay> DSReplays { get; set; }
        public DbSet<DbBreakpoint> Breakpoints { get; set; }
        public DbSet<DbMiddle> Middle { get; set; }
        public virtual DbSet<DbStatsResult> DbStatsResults { get; set; }
        //public DbSet<DbUnit> Units { get; set; }
        //public DbSet<DbRefinery> Refineries { get; set; }
        //public DbSet<DbUpgrade> Upgrades { get; set; }

        /*
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (String.IsNullOrEmpty(DSdata.ServerConfig.DBConnectionString))
                throw new NotSupportedException();

            optionsBuilder
                //.UseSqlServer(sc2dsstatslib.Config.DBConnectionString)
                .UseMySql(DSdata.ServerConfig.DBConnectionString, mySqlOptions => mySqlOptions
                .ServerVersion(new ServerVersion(new Version(5, 7, 29), ServerType.MySql)))
                //.ServerVersion(new ServerVersion(new Version(8, 0, 17), ServerType.MySql)))
                //.UseLoggerFactory(_loggerFactory)
                ;
        }
        */

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .HasDbFunction(typeof(DBFunctions).GetMethod(nameof(DBFunctions.GetOpp)))
                .HasTranslation(args => SqlFunctionExpression.Create("GetOpp", args, typeof(int), null));
            modelBuilder
                .HasDbFunction(typeof(DBFunctions).GetMethod(nameof(DBFunctions.GetPl)))
                .HasTranslation(args => SqlFunctionExpression.Create("GetPl", args, typeof(int), null));

            modelBuilder.Entity<DbStatsResult>(entity =>
            {
                entity.HasKey(k => k.DbStatsResultId);
                entity.HasIndex(p => new { p.GameTime, p.Player });
                entity.HasIndex(p => new { p.GameTime, p.Player, p.Race });
                entity.HasIndex(p => new { p.GameTime, p.Player, p.Race, p.OppRace });
            });

            modelBuilder.Entity<DSReplay>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(b => b.HASH)
                    .HasMaxLength(32)
                    .IsFixedLength();
                entity.HasIndex(b => b.HASH);
                //    .IsUnique();
                entity.HasIndex(b => b.REPLAY);
            });

            modelBuilder.Entity<DbMiddle>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.HasOne(p => p.Replay)
                .WithMany(d => d.Middle);
            });

            modelBuilder.Entity<DSPlayer>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(p => p.NAME)
                    .HasMaxLength(64);
                entity.Property(p => p.RACE)
                    .HasMaxLength(64);
                entity.Property(p => p.OPPRACE)
                    .HasMaxLength(64);
                entity.HasOne(d => d.DSReplay)
                  .WithMany(p => p.DSPlayer);
                entity.HasIndex(b => b.RACE);
                entity.HasIndex(p => new { p.RACE, p.OPPRACE });

            });

            modelBuilder.Entity<DbBreakpoint>(entity =>
            {
                entity.HasKey(p => p.ID);
                entity.HasOne(p => p.Player)
                .WithMany(d => d.Breakpoints);
            });

            modelBuilder.Entity<DSUnit>(entity =>
            {
                entity.HasKey(k => k.ID);
                entity.HasOne(p => p.DSPlayer)
                .WithMany(d => d.DSUnit);
            });
        }
    }
}

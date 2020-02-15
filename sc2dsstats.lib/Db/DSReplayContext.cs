using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace sc2dsstats.lib.Db
{
    public class DSReplayContext : DbContext
    {
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddFilter((c, l) => l == LogLevel.Information && !c.EndsWith("Connection")));

        public DbSet<DSUnit> DSUnits { get; set; }
        public DbSet<DSPlayer> DSPlayers { get; set; }
        public DbSet<PLDuplicate> PLDuplicates { get; set; }
        public DbSet<DSReplay> DSReplays { get; set; }
        public DbSet<DefaultFilterCmdr> DefaultFilterCmdrs { get; set; }
        public DbSet<DSPlayerResult> DSPlayerResults { get; set; }

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
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .HasDbFunction(typeof(DBFunctions).GetMethod(nameof(DBFunctions.GetOpp)))
                .HasTranslation(args => SqlFunctionExpression.Create("GetOpp", args, typeof(int), null));
            modelBuilder
                .HasDbFunction(typeof(DBFunctions).GetMethod(nameof(DBFunctions.GetPl)))
                .HasTranslation(args => SqlFunctionExpression.Create("GetPl", args, typeof(int), null));

            modelBuilder.Entity<DSReplay>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(b => b.HASH)
                    .HasMaxLength(32)
                    .IsFixedLength();
                entity.HasIndex(b => b.HASH)
                    .IsUnique();
            });

            modelBuilder.Entity<DefaultFilterCmdr>(e => e.ToView("DefaultFilterCmdr").HasNoKey());
            modelBuilder.Entity<DSPlayerResult>(e => e.ToView("DSPlayerResult").HasNoKey());

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

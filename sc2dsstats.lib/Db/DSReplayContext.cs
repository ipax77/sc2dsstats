using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (String.IsNullOrEmpty(sc2dsstatslib.Config.DBConnectionString))
                sc2dsstatslib.LoadConfig();

            optionsBuilder
                .UseMySql(sc2dsstatslib.Config.DBConnectionString, mySqlOptions => mySqlOptions
                .ServerVersion(new ServerVersion(new Version(5, 7, 27), ServerType.MySql)))
                //.UseLoggerFactory(_loggerFactory)
                ; 
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .HasDbFunction(typeof(DBFunctions).GetMethod(nameof(DBFunctions.GetOpp)))
                .HasTranslation(args => SqlFunctionExpression.Create("GetOpp", args, typeof(int), null));

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

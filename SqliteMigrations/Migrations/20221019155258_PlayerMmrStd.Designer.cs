﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using pax.dsstats.dbng;

#nullable disable

namespace SqliteMigrations.Migrations
{
    [DbContext(typeof(ReplayContext))]
    [Migration("20221019155258_PlayerMmrStd")]
    partial class PlayerMmrStd
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.10");

            modelBuilder.Entity("pax.dsstats.dbng.Event", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("EventGuid")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("EventStart")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("EventId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Events");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Player", b =>
                {
                    b.Property<int>("PlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LatestUpload")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.Property<double>("Mmr")
                        .HasColumnType("REAL");

                    b.Property<string>("MmrOverTime")
                        .HasMaxLength(2000)
                        .HasColumnType("TEXT");

                    b.Property<double>("MmrStd")
                        .HasColumnType("REAL");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<int>("ToonId")
                        .HasColumnType("INTEGER");

                    b.HasKey("PlayerId");

                    b.HasIndex("ToonId")
                        .IsUnique();

                    b.ToTable("Players");
                });

            modelBuilder.Entity("pax.dsstats.dbng.PlayerUpgrade", b =>
                {
                    b.Property<int>("PlayerUpgradeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Gameloop")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ReplayPlayerId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UpgradeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("PlayerUpgradeId");

                    b.HasIndex("ReplayPlayerId");

                    b.HasIndex("UpgradeId");

                    b.ToTable("PlayerUpgrades");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Replay", b =>
                {
                    b.Property<int>("ReplayId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Bunker")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Cannon")
                        .HasColumnType("INTEGER");

                    b.Property<string>("CommandersTeam1")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CommandersTeam2")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("DefaultFilter")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Downloads")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Duration")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<int>("GameMode")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("GameTime")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.Property<int>("Maxkillsum")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Maxleaver")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Middle")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("TEXT");

                    b.Property<int>("Minarmy")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Minincome")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Minkillsum")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Objective")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("Playercount")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ReplayEventId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ReplayHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("TEXT")
                        .IsFixedLength();

                    b.Property<int>("Views")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WinnerTeam")
                        .HasColumnType("INTEGER");

                    b.HasKey("ReplayId");

                    b.HasIndex("FileName");

                    b.HasIndex("ReplayEventId");

                    b.HasIndex("ReplayHash")
                        .IsUnique();

                    b.HasIndex("GameTime", "GameMode");

                    b.HasIndex("GameTime", "GameMode", "DefaultFilter");

                    b.ToTable("Replays");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayDownloadCount", b =>
                {
                    b.Property<int>("ReplayDownloadCountId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ReplayHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("TEXT");

                    b.HasKey("ReplayDownloadCountId");

                    b.ToTable("ReplayDownloadCounts");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayEvent", b =>
                {
                    b.Property<int>("ReplayEventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Ban1")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Ban2")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Ban3")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Ban4")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Ban5")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Round")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("RunnerTeam")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("WinnerTeam")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("ReplayEventId");

                    b.HasIndex("EventId");

                    b.ToTable("ReplayEvents");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayPlayer", b =>
                {
                    b.Property<int>("ReplayPlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("APM")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Army")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Clan")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<int>("Downloads")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Duration")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GamePos")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Income")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsUploader")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Kills")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<int>("OppRace")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlayerId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PlayerResult")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Race")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Refineries")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<int>("ReplayId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Team")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TierUpgrades")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<int?>("UpgradeId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UpgradesSpent")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Views")
                        .HasColumnType("INTEGER");

                    b.HasKey("ReplayPlayerId");

                    b.HasIndex("PlayerId");

                    b.HasIndex("Race");

                    b.HasIndex("ReplayId");

                    b.HasIndex("UpgradeId");

                    b.HasIndex("Race", "OppRace");

                    b.ToTable("ReplayPlayers");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayViewCount", b =>
                {
                    b.Property<int>("ReplayViewCountId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ReplayHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("TEXT");

                    b.HasKey("ReplayViewCountId");

                    b.ToTable("ReplayViewCounts");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Spawn", b =>
                {
                    b.Property<int>("SpawnId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ArmyValue")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Gameloop")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GasCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Income")
                        .HasColumnType("INTEGER");

                    b.Property<int>("KilledValue")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ReplayPlayerId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UpgradeSpent")
                        .HasColumnType("INTEGER");

                    b.HasKey("SpawnId");

                    b.HasIndex("ReplayPlayerId");

                    b.ToTable("Spawns");
                });

            modelBuilder.Entity("pax.dsstats.dbng.SpawnUnit", b =>
                {
                    b.Property<int>("SpawnUnitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte>("Count")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Poss")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("TEXT");

                    b.Property<int>("SpawnId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UnitId")
                        .HasColumnType("INTEGER");

                    b.HasKey("SpawnUnitId");

                    b.HasIndex("SpawnId");

                    b.HasIndex("UnitId");

                    b.ToTable("SpawnUnits");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Unit", b =>
                {
                    b.Property<int>("UnitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Commander")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Cost")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("TEXT");

                    b.HasKey("UnitId");

                    b.ToTable("Units");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Upgrade", b =>
                {
                    b.Property<int>("UpgradeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Cost")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.HasKey("UpgradeId");

                    b.ToTable("Upgrades");
                });

            modelBuilder.Entity("pax.dsstats.dbng.PlayerUpgrade", b =>
                {
                    b.HasOne("pax.dsstats.dbng.ReplayPlayer", "ReplayPlayer")
                        .WithMany("Upgrades")
                        .HasForeignKey("ReplayPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Upgrade", "Upgrade")
                        .WithMany()
                        .HasForeignKey("UpgradeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ReplayPlayer");

                    b.Navigation("Upgrade");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Replay", b =>
                {
                    b.HasOne("pax.dsstats.dbng.ReplayEvent", "ReplayEvent")
                        .WithMany("Replays")
                        .HasForeignKey("ReplayEventId");

                    b.Navigation("ReplayEvent");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayEvent", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Event", "Event")
                        .WithMany("ReplayEvents")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayPlayer", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Player", "Player")
                        .WithMany("ReplayPlayers")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Replay", "Replay")
                        .WithMany("Players")
                        .HasForeignKey("ReplayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Upgrade", null)
                        .WithMany("ReplayPlayers")
                        .HasForeignKey("UpgradeId");

                    b.Navigation("Player");

                    b.Navigation("Replay");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Spawn", b =>
                {
                    b.HasOne("pax.dsstats.dbng.ReplayPlayer", null)
                        .WithMany("Spawns")
                        .HasForeignKey("ReplayPlayerId");
                });

            modelBuilder.Entity("pax.dsstats.dbng.SpawnUnit", b =>
                {
                    b.HasOne("pax.dsstats.dbng.Spawn", "Spawn")
                        .WithMany("Units")
                        .HasForeignKey("SpawnId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("pax.dsstats.dbng.Unit", "Unit")
                        .WithMany()
                        .HasForeignKey("UnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Spawn");

                    b.Navigation("Unit");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Event", b =>
                {
                    b.Navigation("ReplayEvents");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Player", b =>
                {
                    b.Navigation("ReplayPlayers");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Replay", b =>
                {
                    b.Navigation("Players");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayEvent", b =>
                {
                    b.Navigation("Replays");
                });

            modelBuilder.Entity("pax.dsstats.dbng.ReplayPlayer", b =>
                {
                    b.Navigation("Spawns");

                    b.Navigation("Upgrades");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Spawn", b =>
                {
                    b.Navigation("Units");
                });

            modelBuilder.Entity("pax.dsstats.dbng.Upgrade", b =>
                {
                    b.Navigation("ReplayPlayers");
                });
#pragma warning restore 612, 618
        }
    }
}

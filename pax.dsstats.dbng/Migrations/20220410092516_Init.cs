using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pax.dsstats.dbng.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                });

            migrationBuilder.CreateTable(
                name: "Replays",
                columns: table => new
                {
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GameTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerTeam = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerTeamName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RunnerTeamName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GameMode = table.Column<int>(type: "INTEGER", nullable: false),
                    Objective = table.Column<int>(type: "INTEGER", nullable: false),
                    Bunker = table.Column<int>(type: "INTEGER", nullable: false),
                    Cannon = table.Column<int>(type: "INTEGER", nullable: false),
                    Minkillsum = table.Column<int>(type: "INTEGER", nullable: false),
                    Maxkillsum = table.Column<int>(type: "INTEGER", nullable: false),
                    Minarmy = table.Column<int>(type: "INTEGER", nullable: false),
                    Minincome = table.Column<int>(type: "INTEGER", nullable: false),
                    Maxleaver = table.Column<int>(type: "INTEGER", nullable: false),
                    Playercount = table.Column<byte>(type: "INTEGER", nullable: false),
                    ReplayHash = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    DefaultFilter = table.Column<bool>(type: "INTEGER", nullable: false),
                    Views = table.Column<int>(type: "INTEGER", nullable: false),
                    Downloads = table.Column<int>(type: "INTEGER", nullable: false),
                    Middle = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CommandersTeam1 = table.Column<string>(type: "TEXT", nullable: false),
                    CommandersTeam2 = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replays", x => x.ReplayId);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Cost = table.Column<int>(type: "INTEGER", nullable: false),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.UnitId);
                });

            migrationBuilder.CreateTable(
                name: "Upgrades",
                columns: table => new
                {
                    UpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Cost = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Upgrades", x => x.UpgradeId);
                });

            migrationBuilder.CreateTable(
                name: "ReplayPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Clan = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    GamePos = table.Column<int>(type: "INTEGER", nullable: false),
                    Team = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerResult = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Race = table.Column<int>(type: "INTEGER", nullable: false),
                    OppRace = table.Column<int>(type: "INTEGER", nullable: false),
                    APM = table.Column<int>(type: "INTEGER", nullable: false),
                    Income = table.Column<int>(type: "INTEGER", nullable: false),
                    Army = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    UpgradesSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUploader = table.Column<bool>(type: "INTEGER", nullable: false),
                    TierUpgrades = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Refineries = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Downloads = table.Column<int>(type: "INTEGER", nullable: false),
                    Views = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplayPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayers_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerUpgrades",
                columns: table => new
                {
                    PlayerUpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Gameloop = table.Column<int>(type: "INTEGER", nullable: false),
                    UpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerUpgrades", x => x.PlayerUpgradeId);
                    table.ForeignKey(
                        name: "FK_PlayerUpgrades_Upgrades_UpgradeId",
                        column: x => x.UpgradeId,
                        principalTable: "Upgrades",
                        principalColumn: "UpgradeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayPlayerUpgrade",
                columns: table => new
                {
                    ReplayPlayersId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpgradesUpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayPlayerUpgrade", x => new { x.ReplayPlayersId, x.UpgradesUpgradeId });
                    table.ForeignKey(
                        name: "FK_ReplayPlayerUpgrade_ReplayPlayers_ReplayPlayersId",
                        column: x => x.ReplayPlayersId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerUpgrade_Upgrades_UpgradesUpgradeId",
                        column: x => x.UpgradesUpgradeId,
                        principalTable: "Upgrades",
                        principalColumn: "UpgradeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Spawns",
                columns: table => new
                {
                    SpawnId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Income = table.Column<int>(type: "INTEGER", nullable: false),
                    GasCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ArmyValue = table.Column<int>(type: "INTEGER", nullable: false),
                    KilledValue = table.Column<int>(type: "INTEGER", nullable: false),
                    UpgradeSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spawns", x => x.SpawnId);
                    table.ForeignKey(
                        name: "FK_Spawns_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SpawnUnits",
                columns: table => new
                {
                    SpawnUnitId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PosX = table.Column<int>(type: "INTEGER", nullable: false),
                    PosY = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    SpawnId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnUnits", x => x.SpawnUnitId);
                    table.ForeignKey(
                        name: "FK_SpawnUnits_Spawns_SpawnId",
                        column: x => x.SpawnId,
                        principalTable: "Spawns",
                        principalColumn: "SpawnId");
                    table.ForeignKey(
                        name: "FK_SpawnUnits_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_Name",
                table: "Players",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUpgrades_UpgradeId",
                table: "PlayerUpgrades",
                column: "UpgradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_PlayerId",
                table: "ReplayPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_Race",
                table: "ReplayPlayers",
                column: "Race");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_Race_OppRace",
                table: "ReplayPlayers",
                columns: new[] { "Race", "OppRace" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_ReplayId",
                table: "ReplayPlayers",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerUpgrade_UpgradesUpgradeId",
                table: "ReplayPlayerUpgrade",
                column: "UpgradesUpgradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode_DefaultFilter",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode", "DefaultFilter" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_ReplayHash",
                table: "Replays",
                column: "ReplayHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Spawns_ReplayPlayerId",
                table: "Spawns",
                column: "ReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnUnits_SpawnId",
                table: "SpawnUnits",
                column: "SpawnId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnUnits_UnitId",
                table: "SpawnUnits",
                column: "UnitId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerUpgrades");

            migrationBuilder.DropTable(
                name: "ReplayPlayerUpgrade");

            migrationBuilder.DropTable(
                name: "SpawnUnits");

            migrationBuilder.DropTable(
                name: "Upgrades");

            migrationBuilder.DropTable(
                name: "Spawns");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "ReplayPlayers");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Replays");
        }
    }
}

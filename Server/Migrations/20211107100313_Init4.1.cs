using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sc2dsstats._2022.Server.Migrations
{
    public partial class Init41 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommanderNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderNames", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UnitNamesUpdate = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpgradeNamesUpdate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsInfo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsPlayerNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AppId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DbId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Hash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LatestReplay = table.Column<DateTime>(type: "datetime", nullable: false),
                    LatestUpload = table.Column<DateTime>(type: "datetime", nullable: false),
                    TotlaReplays = table.Column<int>(type: "int", nullable: false),
                    AppVersion = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NamesMapped = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsPlayerNames", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dsreplays",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    REPLAY = table.Column<string>(type: "varchar(95)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GAMETIME = table.Column<DateTime>(type: "datetime", maxLength: 6, nullable: false),
                    WINNER = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    DURATION = table.Column<int>(type: "int(11)", nullable: false),
                    MINKILLSUM = table.Column<int>(type: "int(11)", nullable: false),
                    MAXKILLSUM = table.Column<int>(type: "int(11)", nullable: false),
                    MINARMY = table.Column<int>(type: "int(11)", nullable: false),
                    MININCOME = table.Column<int>(type: "int(11)", nullable: false),
                    MAXLEAVER = table.Column<int>(type: "int(11)", nullable: false),
                    PLAYERCOUNT = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    REPORTED = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    ISBRAWL = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Gamemode = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    VERSION = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HASH = table.Column<string>(type: "char(32)", fixedLength: true, maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    REPLAYPATH = table.Column<string>(type: "varchar(95)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OBJECTIVE = table.Column<int>(type: "int(11)", nullable: false),
                    Bunker = table.Column<int>(type: "int", nullable: false),
                    Cannon = table.Column<int>(type: "int", nullable: false),
                    Upload = table.Column<DateTime>(type: "datetime", maxLength: 6, nullable: false),
                    DefaultFilter = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mid1 = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Mid2 = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dsreplays", x => x.ID);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DSRestPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Json = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastRep = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpload = table.Column<DateTime>(type: "datetime", nullable: false),
                    Data = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DSRestPlayers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsTimeResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Player = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Timespan = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cmdr = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Opp = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    MVP = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Kills = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Army = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsTimeResults", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsTimeResultValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Player = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Gametime = table.Column<DateTime>(type: "datetime", maxLength: 100, nullable: false),
                    Cmdr = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Opp = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Win = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MVP = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Kills = table.Column<int>(type: "int", nullable: false),
                    Army = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsTimeResultValues", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UnitNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitNames", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UpgradeNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeNames", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dsplayers",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    POS = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    REALPOS = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    NAME = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Race = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Opprace = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    WIN = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TEAM = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    KILLSUM = table.Column<int>(type: "int(11)", nullable: false),
                    INCOME = table.Column<int>(type: "int(11)", nullable: false),
                    PDURATION = table.Column<int>(type: "int(11)", nullable: false),
                    ARMY = table.Column<int>(type: "int(11)", nullable: false),
                    GAS = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    DSReplayID = table.Column<int>(type: "int(11)", nullable: true),
                    isPlayer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlayerNameId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dsplayers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_dsplayers_DsPlayerNames_PlayerNameId",
                        column: x => x.PlayerNameId,
                        principalTable: "DsPlayerNames",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DSPlayers_DSReplays_DSReplayID",
                        column: x => x.DSReplayID,
                        principalTable: "dsreplays",
                        principalColumn: "ID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "middle",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Gameloop = table.Column<int>(type: "int(11)", nullable: false),
                    Team = table.Column<sbyte>(type: "tinyint(3)", nullable: false),
                    ReplayID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_middle", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Middle_DSReplays_ReplayID",
                        column: x => x.ReplayID,
                        principalTable: "dsreplays",
                        principalColumn: "ID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Cmdr = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    DsTimeResultId = table.Column<int>(type: "int", nullable: true),
                    DsTimeResultId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participants_DsTimeResults_DsTimeResultId",
                        column: x => x.DsTimeResultId,
                        principalTable: "DsTimeResults",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Participants_DsTimeResults_DsTimeResultId1",
                        column: x => x.DsTimeResultId1,
                        principalTable: "DsTimeResults",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dsparticipantsvalues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Cmdr = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Win = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DsTimeResultValuesId = table.Column<int>(type: "int", nullable: true),
                    DsTimeResultValuesId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dsparticipantsvalues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DsParticipantsValues_DsTimeResultValues_DsTimeResultValuesId",
                        column: x => x.DsTimeResultValuesId,
                        principalTable: "DsTimeResultValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DsParticipantsValues_DsTimeResultValues_DsTimeResultValuesId1",
                        column: x => x.DsTimeResultValuesId1,
                        principalTable: "DsTimeResultValues",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "breakpoints",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Breakpoint = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Gas = table.Column<int>(type: "int(11)", nullable: false),
                    Income = table.Column<int>(type: "int(11)", nullable: false),
                    Army = table.Column<int>(type: "int(11)", nullable: false),
                    Kills = table.Column<int>(type: "int(11)", nullable: false),
                    Upgrades = table.Column<int>(type: "int(11)", nullable: false),
                    Tier = table.Column<int>(type: "int(11)", nullable: false),
                    PlayerID = table.Column<int>(type: "int", nullable: true),
                    Mid = table.Column<int>(type: "int(11)", nullable: false),
                    dsUnitsString = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dbUnitsString = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dbUpgradesString = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_breakpoints", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Breakpoints_DSPlayers_PlayerID",
                        column: x => x.PlayerID,
                        principalTable: "dsplayers",
                        principalColumn: "ID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dsunits",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BP = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Count = table.Column<int>(type: "int(11)", nullable: false),
                    BreakpointID = table.Column<int>(type: "int", nullable: true),
                    DSPlayerID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dsunits", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DSUnits_Breakpoints_BreakpointID",
                        column: x => x.BreakpointID,
                        principalTable: "breakpoints",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_DSUnits_DSPlayers_DSPlayerID",
                        column: x => x.DSPlayerID,
                        principalTable: "dsplayers",
                        principalColumn: "ID");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "DsInfo",
                columns: new[] { "Id", "UnitNamesUpdate", "UpgradeNamesUpdate" },
                values: new object[] { 1, new DateTime(2021, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2021, 10, 31, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_Breakpoints_PlayerID",
                table: "breakpoints",
                column: "PlayerID");

            migrationBuilder.CreateIndex(
                name: "IX_DsParticipantsValues_DsTimeResultValuesId",
                table: "dsparticipantsvalues",
                column: "DsTimeResultValuesId");

            migrationBuilder.CreateIndex(
                name: "IX_DsParticipantsValues_DsTimeResultValuesId1",
                table: "dsparticipantsvalues",
                column: "DsTimeResultValuesId1");

            migrationBuilder.CreateIndex(
                name: "IX_DsPlayerNames_AppId",
                table: "DsPlayerNames",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_DsPlayerNames_DbId",
                table: "DsPlayerNames",
                column: "DbId");

            migrationBuilder.CreateIndex(
                name: "IX_DsPlayerNames_Hash",
                table: "DsPlayerNames",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_DsPlayerNames_Name",
                table: "DsPlayerNames",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DSPlayers_DSReplayID",
                table: "dsplayers",
                column: "DSReplayID");

            migrationBuilder.CreateIndex(
                name: "IX_DSPlayers_NAME",
                table: "dsplayers",
                column: "NAME");

            migrationBuilder.CreateIndex(
                name: "IX_dsplayers_PlayerNameId",
                table: "dsplayers",
                column: "PlayerNameId");

            migrationBuilder.CreateIndex(
                name: "IX_DSPlayers_RACE",
                table: "dsplayers",
                column: "Race");

            migrationBuilder.CreateIndex(
                name: "IX_DSPlayers_RACE_OPPRACE",
                table: "dsplayers",
                columns: new[] { "Race", "Opprace" });

            migrationBuilder.CreateIndex(
                name: "IX_DSPlayers_RACE_OPPRACE_PLAYER",
                table: "dsplayers",
                columns: new[] { "Race", "Opprace", "isPlayer" });

            migrationBuilder.CreateIndex(
                name: "IX_DSPlayers_RACE_PLAYER",
                table: "dsplayers",
                columns: new[] { "Race", "isPlayer" });

            migrationBuilder.CreateIndex(
                name: "IX_dsreplays_GAMETIME",
                table: "dsreplays",
                column: "GAMETIME");

            migrationBuilder.CreateIndex(
                name: "IX_dsreplays_GAMETIME_DefaultFilter",
                table: "dsreplays",
                columns: new[] { "GAMETIME", "DefaultFilter" });

            migrationBuilder.CreateIndex(
                name: "IX_DSReplays_HASH",
                table: "dsreplays",
                column: "HASH");

            migrationBuilder.CreateIndex(
                name: "IX_DSReplays_REPLAY",
                table: "dsreplays",
                column: "REPLAY");

            migrationBuilder.CreateIndex(
                name: "IX_DSReplays_REPLAYPATH",
                table: "dsreplays",
                column: "REPLAYPATH");

            migrationBuilder.CreateIndex(
                name: "IX_DsTimeResults_Cmdr",
                table: "DsTimeResults",
                column: "Cmdr");

            migrationBuilder.CreateIndex(
                name: "IX_DsTimeResults_Opp",
                table: "DsTimeResults",
                column: "Opp");

            migrationBuilder.CreateIndex(
                name: "IX_DsTimeResults_Player",
                table: "DsTimeResults",
                column: "Player");

            migrationBuilder.CreateIndex(
                name: "IX_DsTimeResults_Timespan",
                table: "DsTimeResults",
                column: "Timespan");

            migrationBuilder.CreateIndex(
                name: "IX_DsTimeResults_Timespan_Cmdr",
                table: "DsTimeResults",
                columns: new[] { "Timespan", "Cmdr" });

            migrationBuilder.CreateIndex(
                name: "IX_DsTimeResults_Timespan_Cmdr_Opp",
                table: "DsTimeResults",
                columns: new[] { "Timespan", "Cmdr", "Opp" });

            migrationBuilder.CreateIndex(
                name: "IX_DsTimeResults_Timespan_Cmdr_Opp_Player",
                table: "DsTimeResults",
                columns: new[] { "Timespan", "Cmdr", "Opp", "Player" });

            migrationBuilder.CreateIndex(
                name: "IX_DsTimeResults_Timespan_Cmdr_Player",
                table: "DsTimeResults",
                columns: new[] { "Timespan", "Cmdr", "Player" });

            migrationBuilder.CreateIndex(
                name: "IX_DSUnits_BreakpointID",
                table: "dsunits",
                column: "BreakpointID");

            migrationBuilder.CreateIndex(
                name: "IX_DSUnits_DSPlayerID",
                table: "dsunits",
                column: "DSPlayerID");

            migrationBuilder.CreateIndex(
                name: "IX_Middle_ReplayID",
                table: "middle",
                column: "ReplayID");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_DsTimeResultId",
                table: "participants",
                column: "DsTimeResultId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_DsTimeResultId1",
                table: "participants",
                column: "DsTimeResultId1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommanderNames");

            migrationBuilder.DropTable(
                name: "DsInfo");

            migrationBuilder.DropTable(
                name: "dsparticipantsvalues");

            migrationBuilder.DropTable(
                name: "DSRestPlayers");

            migrationBuilder.DropTable(
                name: "dsunits");

            migrationBuilder.DropTable(
                name: "middle");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "UnitNames");

            migrationBuilder.DropTable(
                name: "UpgradeNames");

            migrationBuilder.DropTable(
                name: "DsTimeResultValues");

            migrationBuilder.DropTable(
                name: "breakpoints");

            migrationBuilder.DropTable(
                name: "DsTimeResults");

            migrationBuilder.DropTable(
                name: "dsplayers");

            migrationBuilder.DropTable(
                name: "DsPlayerNames");

            migrationBuilder.DropTable(
                name: "dsreplays");
        }
    }
}

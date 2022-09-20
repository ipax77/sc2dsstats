using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class Counts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplayDownloadCounts",
                columns: table => new
                {
                    ReplayDownloadCountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReplayHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayDownloadCounts", x => x.ReplayDownloadCountId);
                });

            migrationBuilder.CreateTable(
                name: "ReplayViewCounts",
                columns: table => new
                {
                    ReplayViewCountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReplayHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayViewCounts", x => x.ReplayViewCountId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Name",
                table: "Events",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayDownloadCounts");

            migrationBuilder.DropTable(
                name: "ReplayViewCounts");

            migrationBuilder.DropIndex(
                name: "IX_Events_Name",
                table: "Events");
        }
    }
}

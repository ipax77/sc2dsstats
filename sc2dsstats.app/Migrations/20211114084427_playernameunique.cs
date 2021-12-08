using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sc2dsstats.app.Migrations
{
    public partial class playernameunique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DsPlayerNames_Name",
                table: "DsPlayerNames");

            migrationBuilder.CreateIndex(
                name: "IX_DsPlayerNames_Name",
                table: "DsPlayerNames",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DsPlayerNames_Name",
                table: "DsPlayerNames");

            migrationBuilder.CreateIndex(
                name: "IX_DsPlayerNames_Name",
                table: "DsPlayerNames",
                column: "Name");
        }
    }
}

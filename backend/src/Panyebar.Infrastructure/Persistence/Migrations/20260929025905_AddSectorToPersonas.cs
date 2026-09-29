using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorToPersonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectorId",
                table: "Personas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personas_SectorId",
                table: "Personas",
                column: "SectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personas_Sectores_SectorId",
                table: "Personas",
                column: "SectorId",
                principalTable: "Sectores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personas_Sectores_SectorId",
                table: "Personas");

            migrationBuilder.DropIndex(
                name: "IX_Personas_SectorId",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Personas");
        }
    }
}

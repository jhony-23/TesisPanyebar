using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowHistoricalPaymentApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AplicacionesPago_ObligacionId",
                table: "AplicacionesPago");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesPago_ObligacionId",
                table: "AplicacionesPago",
                column: "ObligacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AplicacionesPago_ObligacionId",
                table: "AplicacionesPago");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesPago_ObligacionId",
                table: "AplicacionesPago",
                column: "ObligacionId",
                unique: true);
        }
    }
}

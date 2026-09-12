using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AplicacionesPago_ObligacionId",
                table: "AplicacionesPago");

            migrationBuilder.AlterColumn<string>(
                name: "Concepto",
                table: "Pagos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pagos_MontoPositivo",
                table: "Pagos",
                sql: "[Monto] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesPago_ObligacionId",
                table: "AplicacionesPago",
                column: "ObligacionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Pagos_MontoPositivo",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_AplicacionesPago_ObligacionId",
                table: "AplicacionesPago");

            migrationBuilder.AlterColumn<string>(
                name: "Concepto",
                table: "Pagos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesPago_ObligacionId",
                table: "AplicacionesPago",
                column: "ObligacionId");
        }
    }
}

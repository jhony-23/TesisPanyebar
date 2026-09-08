using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureSupplyProcesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcesosSuministro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuministroId = table.Column<int>(type: "int", nullable: false),
                    TipoProceso = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "int", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioAdministrativoId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcesosSuministro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcesosSuministro_Suministros_SuministroId",
                        column: x => x.SuministroId,
                        principalTable: "Suministros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcesosSuministro_UsuariosAdministrativos_UsuarioAdministrativoId",
                        column: x => x.UsuarioAdministrativoId,
                        principalTable: "UsuariosAdministrativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesNuevoServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonaSolicitanteId = table.Column<int>(type: "int", nullable: false),
                    SectorId = table.Column<int>(type: "int", nullable: false),
                    DireccionReferencia = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioResolucionId = table.Column<int>(type: "int", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuministroId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesNuevoServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesNuevoServicio_Personas_PersonaSolicitanteId",
                        column: x => x.PersonaSolicitanteId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesNuevoServicio_Sectores_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesNuevoServicio_Suministros_SuministroId",
                        column: x => x.SuministroId,
                        principalTable: "Suministros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesNuevoServicio_UsuariosAdministrativos_UsuarioResolucionId",
                        column: x => x.UsuarioResolucionId,
                        principalTable: "UsuariosAdministrativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosSuministro_SuministroId_Fecha",
                table: "ProcesosSuministro",
                columns: new[] { "SuministroId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosSuministro_UsuarioAdministrativoId",
                table: "ProcesosSuministro",
                column: "UsuarioAdministrativoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesNuevoServicio_Estado_FechaSolicitud",
                table: "SolicitudesNuevoServicio",
                columns: new[] { "Estado", "FechaSolicitud" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesNuevoServicio_PersonaSolicitanteId",
                table: "SolicitudesNuevoServicio",
                column: "PersonaSolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesNuevoServicio_SectorId",
                table: "SolicitudesNuevoServicio",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesNuevoServicio_SuministroId",
                table: "SolicitudesNuevoServicio",
                column: "SuministroId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesNuevoServicio_UsuarioResolucionId",
                table: "SolicitudesNuevoServicio",
                column: "UsuarioResolucionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcesosSuministro");

            migrationBuilder.DropTable(
                name: "SolicitudesNuevoServicio");
        }
    }
}

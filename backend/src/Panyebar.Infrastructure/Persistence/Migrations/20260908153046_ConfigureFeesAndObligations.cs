using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureFeesAndObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // PERMISOS ADMINISTRATIVOS - CUOTAS Y OBLIGACIONES
            // ============================================================

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'CUOTAS.VER')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (
                        N'CUOTAS.VER',
                        N'Ver cuotas',
                        N'Permite consultar las cuotas administrativas.',
                        1
                    )
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'CUOTAS.GESTIONAR')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (
                        N'CUOTAS.GESTIONAR',
                        N'Gestionar cuotas',
                        N'Permite crear, modificar y administrar las cuotas.',
                        1
                    )
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'OBLIGACIONES.VER')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (
                        N'OBLIGACIONES.VER',
                        N'Ver obligaciones',
                        N'Permite consultar las obligaciones registradas.',
                        1
                    )
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'OBLIGACIONES.GESTIONAR')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (
                        N'OBLIGACIONES.GESTIONAR',
                        N'Gestionar obligaciones',
                        N'Permite generar, administrar y anular obligaciones.',
                        1
                    )
                END
                """);

            // ============================================================
            // OBLIGACIONES
            // ============================================================

            migrationBuilder.DropIndex(
                name: "IX_Obligaciones_CuotaId",
                table: "Obligaciones");

            migrationBuilder.DropIndex(
                name: "IX_Obligaciones_PersonaId",
                table: "Obligaciones");

            migrationBuilder.DropIndex(
                name: "IX_Obligaciones_SuministroId",
                table: "Obligaciones");

            migrationBuilder.AlterColumn<string>(
                name: "Periodo",
                table: "Obligaciones",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Concepto",
                table: "Obligaciones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Obligaciones",
                type: "datetime2",
                nullable: true);

            // ============================================================
            // CUOTAS
            // ============================================================

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Cuotas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Cuotas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // ============================================================
            // INDICES DE OBLIGACIONES
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "IX_Obligaciones_CuotaId_SuministroId_Periodo",
                table: "Obligaciones",
                columns: new[] { "CuotaId", "SuministroId", "Periodo" },
                unique: true,
                filter: "[CuotaId] IS NOT NULL AND [SuministroId] IS NOT NULL AND [Periodo] IS NOT NULL AND [Estado] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_Obligaciones_PersonaId_Estado",
                table: "Obligaciones",
                columns: new[] { "PersonaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Obligaciones_SuministroId_Estado",
                table: "Obligaciones",
                columns: new[] { "SuministroId", "Estado" });

            // ============================================================
            // INTEGRIDAD XOR DEL TITULAR
            // ============================================================

            migrationBuilder.AddCheckConstraint(
                name: "CK_Obligaciones_TitularXor",
                table: "Obligaciones",
                sql: "([SuministroId] IS NOT NULL AND [PersonaId] IS NULL) OR ([SuministroId] IS NULL AND [PersonaId] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // REVERSIÓN SEGURA DE PERMISOS
            // Solo elimina permisos que no estén asignados a ningún rol.
            // ============================================================

            migrationBuilder.Sql("""
                DELETE permiso
                FROM [Permisos] AS permiso
                WHERE permiso.[Codigo] IN (
                    N'CUOTAS.VER',
                    N'CUOTAS.GESTIONAR',
                    N'OBLIGACIONES.VER',
                    N'OBLIGACIONES.GESTIONAR'
                )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [RolPermisos] AS rolPermiso
                        WHERE rolPermiso.[PermisoId] = permiso.[Id]
                    )
                """);

            // ============================================================
            // REVERSIÓN OBLIGACIONES
            // ============================================================

            migrationBuilder.DropIndex(
                name: "IX_Obligaciones_CuotaId_SuministroId_Periodo",
                table: "Obligaciones");

            migrationBuilder.DropIndex(
                name: "IX_Obligaciones_PersonaId_Estado",
                table: "Obligaciones");

            migrationBuilder.DropIndex(
                name: "IX_Obligaciones_SuministroId_Estado",
                table: "Obligaciones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Obligaciones_TitularXor",
                table: "Obligaciones");

            migrationBuilder.DropColumn(
                name: "Concepto",
                table: "Obligaciones");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Obligaciones");

            // ============================================================
            // REVERSIÓN CUOTAS
            // ============================================================

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Cuotas");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Cuotas");

            migrationBuilder.AlterColumn<string>(
                name: "Periodo",
                table: "Obligaciones",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            // ============================================================
            // RESTAURACIÓN DE ÍNDICES ANTERIORES
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "IX_Obligaciones_CuotaId",
                table: "Obligaciones",
                column: "CuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Obligaciones_PersonaId",
                table: "Obligaciones",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Obligaciones_SuministroId",
                table: "Obligaciones",
                column: "SuministroId");
        }
    }
}
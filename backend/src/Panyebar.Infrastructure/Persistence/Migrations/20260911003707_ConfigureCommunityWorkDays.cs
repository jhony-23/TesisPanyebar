using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureCommunityWorkDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // PERMISOS ADMINISTRATIVOS - JORNADAS
            // ============================================================

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'JORNADAS.VER')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (
                        N'JORNADAS.VER',
                        N'Ver jornadas',
                        N'Permite consultar las jornadas comunitarias y sus participaciones.',
                        1
                    )
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'JORNADAS.GESTIONAR')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (
                        N'JORNADAS.GESTIONAR',
                        N'Gestionar jornadas',
                        N'Permite crear, modificar y administrar jornadas comunitarias y sus participaciones.',
                        1
                    )
                END
                """);

            // ============================================================
            // JORNADAS Y PARTICIPACIONES
            // ============================================================

            migrationBuilder.DropIndex(
                name: "IX_ObligacionesJornada_ObligacionId",
                table: "ObligacionesJornada");

            migrationBuilder.DropIndex(
                name: "IX_ObligacionesJornada_ParticipacionJornadaId_ObligacionId",
                table: "ObligacionesJornada");

            migrationBuilder.AlterColumn<string>(
                name: "Observacion",
                table: "ParticipacionesJornada",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Jornadas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HoraFin",
                table: "Jornadas",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HoraInicio",
                table: "Jornadas",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Jornadas",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Jornadas]
                SET [Nombre] = LEFT(
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM([Descripcion])), N'') IS NULL
                            THEN CONCAT(N'Jornada ', [Id])
                        ELSE [Descripcion]
                    END,
                    150
                )
                WHERE [Nombre] IS NULL
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Jornadas",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ubicacion",
                table: "Jornadas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParticipacionesJornada_ResultadoValido",
                table: "ParticipacionesJornada",
                sql: "[Resultado] IN (0, 1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_ObligacionesJornada_ObligacionId",
                table: "ObligacionesJornada",
                column: "ObligacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObligacionesJornada_ParticipacionJornadaId",
                table: "ObligacionesJornada",
                column: "ParticipacionJornadaId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Jornadas_EstadoValido",
                table: "Jornadas",
                sql: "[Estado] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Jornadas_HorarioValido",
                table: "Jornadas",
                sql: "[HoraInicio] IS NULL OR [HoraFin] IS NULL OR [HoraFin] > [HoraInicio]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Jornadas_MontoIncumplimientoPositivo",
                table: "Jornadas",
                sql: "[MontoIncumplimiento] IS NULL OR [MontoIncumplimiento] > 0");
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
                    N'JORNADAS.VER',
                    N'JORNADAS.GESTIONAR'
                )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [RolPermisos] AS rolPermiso
                        WHERE rolPermiso.[PermisoId] = permiso.[Id]
                    )
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_ParticipacionesJornada_ResultadoValido",
                table: "ParticipacionesJornada");

            migrationBuilder.DropIndex(
                name: "IX_ObligacionesJornada_ObligacionId",
                table: "ObligacionesJornada");

            migrationBuilder.DropIndex(
                name: "IX_ObligacionesJornada_ParticipacionJornadaId",
                table: "ObligacionesJornada");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Jornadas_EstadoValido",
                table: "Jornadas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Jornadas_HorarioValido",
                table: "Jornadas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Jornadas_MontoIncumplimientoPositivo",
                table: "Jornadas");

            migrationBuilder.DropColumn(
                name: "HoraFin",
                table: "Jornadas");

            migrationBuilder.DropColumn(
                name: "HoraInicio",
                table: "Jornadas");

            migrationBuilder.DropColumn(
                name: "Ubicacion",
                table: "Jornadas");

            migrationBuilder.AlterColumn<string>(
                name: "Observacion",
                table: "ParticipacionesJornada",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.Sql("""
                UPDATE [Jornadas]
                SET [Descripcion] = COALESCE(
                    NULLIF(LTRIM(RTRIM([Descripcion])), N''),
                    [Nombre],
                    CONCAT(N'Jornada ', [Id])
                )
                WHERE [Descripcion] IS NULL OR LTRIM(RTRIM([Descripcion])) = N''
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Jornadas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Jornadas");

            migrationBuilder.CreateIndex(
                name: "IX_ObligacionesJornada_ObligacionId",
                table: "ObligacionesJornada",
                column: "ObligacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ObligacionesJornada_ParticipacionJornadaId_ObligacionId",
                table: "ObligacionesJornada",
                columns: new[] { "ParticipacionJornadaId", "ObligacionId" },
                unique: true);
        }
    }
}

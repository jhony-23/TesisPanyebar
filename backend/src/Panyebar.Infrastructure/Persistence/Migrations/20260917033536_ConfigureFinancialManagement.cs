using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureFinancialManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Concepto",
                table: "Egresos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Estado",
                table: "Egresos",
                type: "int",
                nullable: false,
                // Los egresos preexistentes se conservan como Registrados.
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Egresos_EstadoValido",
                table: "Egresos",
                sql: "[Estado] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Egresos_MontoPositivo",
                table: "Egresos",
                sql: "[Monto] > 0");

            // Mismo seed idempotente por código utilizado en Cuotas y Jornadas.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'FINANZAS.VER')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (N'FINANZAS.VER', N'Ver finanzas',
                        N'Permite consultar ingresos, egresos, movimientos, totales y balance.', 1)
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'FINANZAS.GESTIONAR')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (N'FINANZAS.GESTIONAR', N'Gestionar finanzas',
                        N'Permite registrar, editar y anular egresos.', 1)
                END
                """);

            // Compatibilidad con el administrador demo existente, sin IDs fijos,
            // creación de usuarios/roles ni cambios a los permisos de Pagos.
            migrationBuilder.Sql("""
                INSERT INTO [RolPermisos] ([RolId], [PermisoId])
                SELECT DISTINCT usuarioRol.[RolId], permiso.[Id]
                FROM [UsuariosAdministrativos] AS usuario
                INNER JOIN [UsuarioRoles] AS usuarioRol
                    ON usuarioRol.[UsuarioAdministrativoId] = usuario.[Id]
                INNER JOIN [Roles] AS rol ON rol.[Id] = usuarioRol.[RolId]
                CROSS JOIN [Permisos] AS permiso
                WHERE usuario.[NombreUsuario] = N'demo.admin'
                    AND usuario.[Estado] = 1 AND rol.[Estado] = 1
                    AND permiso.[Estado] = 1
                    AND permiso.[Codigo] IN (N'FINANZAS.VER', N'FINANZAS.GESTIONAR')
                    AND NOT EXISTS (
                        SELECT 1 FROM [RolPermisos] AS existente
                        WHERE existente.[RolId] = usuarioRol.[RolId]
                            AND existente.[PermisoId] = permiso.[Id]
                    )
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Como en Jornadas, se conservan permisos asignados a roles.
            migrationBuilder.Sql("""
                DELETE permiso
                FROM [Permisos] AS permiso
                WHERE permiso.[Codigo] IN (N'FINANZAS.VER', N'FINANZAS.GESTIONAR')
                    AND NOT EXISTS (
                        SELECT 1 FROM [RolPermisos] AS rolPermiso
                        WHERE rolPermiso.[PermisoId] = permiso.[Id]
                    )
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_Egresos_EstadoValido",
                table: "Egresos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Egresos_MontoPositivo",
                table: "Egresos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Egresos");

            migrationBuilder.AlterColumn<string>(
                name: "Concepto",
                table: "Egresos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}

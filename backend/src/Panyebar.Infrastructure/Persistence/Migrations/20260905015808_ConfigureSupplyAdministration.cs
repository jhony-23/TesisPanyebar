using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureSupplyAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'SUMINISTROS.VER')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (N'SUMINISTROS.VER', N'Ver suministros', N'Permite consultar los suministros administrados.', 1)
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Permisos] WHERE [Codigo] = N'SUMINISTROS.GESTIONAR')
                BEGIN
                    INSERT INTO [Permisos] ([Codigo], [Nombre], [Descripcion], [Estado])
                    VALUES (N'SUMINISTROS.GESTIONAR', N'Gestionar suministros', N'Permite crear, modificar y cambiar el estado de los suministros.', 1)
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                DELETE permiso
                                FROM [Permisos] AS permiso
                                WHERE permiso.[Codigo] IN (N'SUMINISTROS.VER', N'SUMINISTROS.GESTIONAR')
                                    AND NOT EXISTS (
                                            SELECT 1
                                            FROM [RolPermisos] AS rolPermiso
                                            WHERE rolPermiso.[PermisoId] = permiso.[Id]
                                    )
                                """);
        }
    }
}

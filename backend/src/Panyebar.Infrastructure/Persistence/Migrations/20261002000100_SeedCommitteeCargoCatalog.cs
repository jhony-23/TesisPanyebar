using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Panyebar.Infrastructure.Persistence.Migrations;

[Migration("20261002000100_SeedCommitteeCargoCatalog")]
public partial class SeedCommitteeCargoCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [Cargos] WHERE [Nombre] = N'Presidente')
            BEGIN
                INSERT INTO [Cargos] ([Nombre], [Descripcion], [Estado])
                VALUES (N'Presidente', N'Coordina y representa la administración del Comité.', 1)
            END
            ELSE
            BEGIN
                UPDATE [Cargos] SET [Estado] = 1 WHERE [Nombre] = N'Presidente'
            END
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [Cargos] WHERE [Nombre] = N'Secretario')
            BEGIN
                INSERT INTO [Cargos] ([Nombre], [Descripcion], [Estado])
                VALUES (N'Secretario', N'Apoya la documentación y los acuerdos del Comité.', 1)
            END
            ELSE
            BEGIN
                UPDATE [Cargos] SET [Estado] = 1 WHERE [Nombre] = N'Secretario'
            END
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [Cargos] WHERE [Nombre] = N'Tesorero')
            BEGIN
                INSERT INTO [Cargos] ([Nombre], [Descripcion], [Estado])
                VALUES (N'Tesorero', N'Apoya el control de ingresos y egresos del Comité.', 1)
            END
            ELSE
            BEGIN
                UPDATE [Cargos] SET [Estado] = 1 WHERE [Nombre] = N'Tesorero'
            END
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [Cargos] WHERE [Nombre] = N'Vocal I')
            BEGIN
                INSERT INTO [Cargos] ([Nombre], [Descripcion], [Estado])
                VALUES (N'Vocal I', N'Apoya las actividades y acuerdos del Comité.', 1)
            END
            ELSE
            BEGIN
                UPDATE [Cargos] SET [Estado] = 1 WHERE [Nombre] = N'Vocal I'
            END
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [Cargos] WHERE [Nombre] = N'Vocal II')
            BEGIN
                INSERT INTO [Cargos] ([Nombre], [Descripcion], [Estado])
                VALUES (N'Vocal II', N'Apoya las actividades y acuerdos del Comité.', 1)
            END
            ELSE
            BEGIN
                UPDATE [Cargos] SET [Estado] = 1 WHERE [Nombre] = N'Vocal II'
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No se eliminan cargos: la migracion no puede distinguir sus filas de
        // cargos preexistentes ni descartar referencias historicas de forma segura.
    }
}
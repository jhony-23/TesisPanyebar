using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Persistence.Migrations;

namespace Panyebar.Security.Tests;

public sealed class CommitteeCargoSeedMigrationTests
{
    private static readonly (string Name, string Description)[] DefaultCargos =
    [
        ("Presidente", "Coordina y representa la administración del Comité."),
        ("Secretario", "Apoya la documentación y los acuerdos del Comité."),
        ("Tesorero", "Apoya el control de ingresos y egresos del Comité."),
        ("Vocal I", "Apoya las actividades y acuerdos del Comité."),
        ("Vocal II", "Apoya las actividades y acuerdos del Comité.")
    ];

    [Fact]
    public void Migration_seeds_all_default_cargos_as_active_and_idempotently()
    {
        var migration = new SeedCommitteeCargoCatalog();
        var operations = migration.UpOperations.OfType<SqlOperation>().ToList();

        Assert.Equal(DefaultCargos.Length, operations.Count);

        foreach (var (name, description) in DefaultCargos)
        {
            var sql = Assert.Single(operations, operation =>
                operation.Sql.Contains($"N'{name}'", StringComparison.Ordinal));

            Assert.Contains("IF NOT EXISTS", sql.Sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("INSERT INTO [Cargos]", sql.Sql, StringComparison.Ordinal);
            Assert.Contains($"N'{description}'", sql.Sql, StringComparison.Ordinal);
            Assert.Contains("[Estado]", sql.Sql, StringComparison.Ordinal);
            Assert.Contains(", 1)", sql.Sql, StringComparison.Ordinal);
            Assert.Contains("UPDATE [Cargos] SET [Estado] = 1", sql.Sql, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migration_down_does_not_delete_preexisting_or_referenced_cargos()
    {
        var migration = new SeedCommitteeCargoCatalog();

        Assert.Empty(migration.DownOperations);
    }

    [Fact]
    public async Task Current_administration_can_obtain_all_seeded_active_cargos()
    {
        await using var db = CreateContext();

        db.AdministracionesComite.Add(new AdministracionComite
        {
            Nombre = "Comité 2026-2027",
            FechaInicio = new DateTime(2026, 1, 1),
            Estado = EstadoRegistro.Activo
        });

        db.Cargos.AddRange(DefaultCargos.Select(cargo => new Cargo
        {
            Nombre = cargo.Name,
            Descripcion = cargo.Description,
            Estado = EstadoRegistro.Activo
        }));

        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);
        var administration = Assert.Single(await service.GetAllAsync());
        var cargos = await service.GetCargosAsync();

        Assert.True(administration.Activa);
        Assert.Equal(
            DefaultCargos.Select(cargo => cargo.Name).OrderBy(name => name),
            cargos.Select(cargo => cargo.Nombre).OrderBy(name => name));
        Assert.All(cargos, cargo => Assert.True(cargo.Activo));
    }

    [Fact]
    public async Task Existing_administration_and_integrantes_remain_compatible_with_cargo_catalog()
    {
        await using var db = CreateContext();
        var administration = new AdministracionComite
        {
            Nombre = "Comité existente",
            FechaInicio = new DateTime(2026, 1, 1),
            Estado = EstadoRegistro.Activo
        };
        var person = new Persona
        {
            Nombres = "Ana",
            Apellidos = "Pérez",
            Estado = EstadoRegistro.Activo
        };
        var cargo = new Cargo
        {
            Nombre = "Presidente",
            Descripcion = "Cargo existente",
            Estado = EstadoRegistro.Activo
        };

        db.AddRange(administration, person, cargo);
        await db.SaveChangesAsync();

        db.IntegrantesAdministracion.Add(new IntegranteAdministracion
        {
            AdministracionComiteId = administration.Id,
            PersonaId = person.Id,
            CargoId = cargo.Id
        });
        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);
        var result = await service.GetAllAsync();

        var integrante = Assert.Single(Assert.Single(result).Integrantes);
        Assert.Equal(person.Id, integrante.PersonaId);
        Assert.Equal(cargo.Id, integrante.CargoId);
        Assert.Equal("Presidente", integrante.CargoNombre);
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PanyebarDbContext(options);
    }
}
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Personas;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class PersonaServiceTests
{
    [Fact]
    public async Task Create_with_sector_returns_and_persists_sector()
    {
        await using var db = CreateContext();
        var sector = await SeedSector(db);
        var result = await new PersonaService(db).CreateAsync(Input(sector.Id));
        Assert.Equal(PersonaOperationError.None, result.Error);
        Assert.Equal(sector.Id, result.Value!.SectorId);
        Assert.Equal(sector.Nombre, result.Value.SectorNombre);
        Assert.Equal(sector.Id, (await db.Personas.SingleAsync()).SectorId);
    }

    [Fact]
    public async Task Create_without_sector_is_valid()
    {
        await using var db = CreateContext();
        var result = await new PersonaService(db).CreateAsync(Input(null));
        Assert.Equal(PersonaOperationError.None, result.Error);
        Assert.Null(result.Value!.SectorId);
        Assert.Null(result.Value.SectorNombre);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_inactive_sector_is_rejected_for_create_and_update(bool inactive)
    {
        await using var db = CreateContext();
        var sector = await SeedSector(db);
        sector.Estado = EstadoRegistro.Inactivo;
        await db.SaveChangesAsync();
        var service = new PersonaService(db);
        var person = (await service.CreateAsync(Input(null))).Value!;
        var invalidId = inactive ? sector.Id : sector.Id + 100;
        Assert.Equal(PersonaOperationError.Invalid, (await service.CreateAsync(Input(invalidId))).Error);
        Assert.Equal(PersonaOperationError.Invalid, (await service.UpdateAsync(person.Id, Input(invalidId))).Error);
        Assert.Null((await db.Personas.SingleAsync()).SectorId);
    }

    [Fact]
    public async Task Update_assigns_changes_and_removes_sector()
    {
        await using var db = CreateContext();
        var first = await SeedSector(db);
        var second = new Sector { Nombre = "Otro sector" };
        db.Sectores.Add(second);
        await db.SaveChangesAsync();
        var service = new PersonaService(db);
        var person = (await service.CreateAsync(Input(null))).Value!;
        foreach (var sector in new[] { first, second })
        {
            var result = await service.UpdateAsync(person.Id, Input(sector.Id));
            Assert.Equal(PersonaOperationError.None, result.Error);
            Assert.Equal(sector.Id, result.Value!.SectorId);
            Assert.Equal(sector.Nombre, result.Value.SectorNombre);
        }
        var removed = await service.UpdateAsync(person.Id, Input(null));
        Assert.Equal(PersonaOperationError.None, removed.Error);
        Assert.Null(removed.Value!.SectorId);
        Assert.Null(removed.Value.SectorNombre);
        Assert.Null((await db.Personas.SingleAsync()).SectorId);
    }

    [Fact]
    public async Task Deactivate_sector_preserves_person_and_assignment()
    {
        await using var db = CreateContext();
        var sector = await SeedSector(db);
        var person = (await new PersonaService(db).CreateAsync(Input(sector.Id))).Value!;
        await new SectorService(db).SetEstadoAsync(sector.Id, EstadoRegistro.Inactivo);
        db.ChangeTracker.Clear();
        var persisted = await db.Personas.SingleAsync();
        Assert.Equal(person.Id, persisted.Id);
        Assert.Equal(sector.Id, persisted.SectorId);
        Assert.Equal(EstadoRegistro.Activo, persisted.Estado);
    }

    [Fact]
    public void SqlServer_model_has_nullable_indexed_restrict_foreign_key()
    {
        using var db = new PanyebarDbContext(new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;").Options);
        var entity = db.Model.FindEntityType(typeof(Persona))!;
        Assert.True(entity.FindProperty(nameof(Persona.SectorId))!.IsNullable);
        var fk = Assert.Single(entity.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(Sector));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        Assert.False(fk.IsRequired);
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Single().Name == nameof(Persona.SectorId));
    }

    [Fact]
    public async Task Existing_person_without_sector_remains_readable_and_editable()
    {
        await using var db = CreateContext();
        var existing = new Persona { Nombres = "Persona", Apellidos = "Existente" };
        db.Personas.Add(existing);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new PersonaService(db);
        Assert.Null((await service.GetByIdAsync(existing.Id))!.SectorId);
        var result = await service.UpdateAsync(existing.Id, Input(null));
        Assert.Equal(PersonaOperationError.None, result.Error);
        Assert.Null(result.Value!.SectorId);
    }

    [Fact]
    public async Task List_detail_and_status_return_sector_after_reloading()
    {
        await using var db = CreateContext();
        var sector = await SeedSector(db);
        var service = new PersonaService(db);
        var person = (await service.CreateAsync(Input(sector.Id))).Value!;
        await service.CreateAsync(Input(null));
        db.ChangeTracker.Clear();
        var list = await service.ListAsync();
        Assert.Equal(2, list.Count);
        Assert.Null(Assert.Single(list, x => x.Id != person.Id).SectorNombre);
        foreach (var dto in new[] { Assert.Single(list, x => x.Id == person.Id),
            (await service.GetByIdAsync(person.Id))!,
            (await service.SetEstadoAsync(person.Id, EstadoRegistro.Inactivo)).Value! })
        {
            Assert.Equal(sector.Id, dto.SectorId);
            Assert.Equal(sector.Nombre, dto.SectorNombre);
        }
    }

    private static PersonaInput Input(int? sectorId) => new("Ana", "Lopez", null, null, null, sectorId);

    private static async Task<Sector> SeedSector(PanyebarDbContext db)
    {
        var sector = new Sector { Nombre = "Centro", Estado = EstadoRegistro.Activo };
        db.Sectores.Add(sector);
        await db.SaveChangesAsync();
        return sector;
    }

    private static PanyebarDbContext CreateContext() => new(new DbContextOptionsBuilder<PanyebarDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

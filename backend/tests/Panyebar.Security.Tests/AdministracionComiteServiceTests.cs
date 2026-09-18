using Microsoft.EntityFrameworkCore;
using Panyebar.Application.AdministracionComite;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public sealed class AdministracionComiteServiceTests
{
    [Fact]
    public async Task Create_creates_active_administration_and_audits()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);
        var service = new AdministracionComiteService(db);

        var result = await service.CreateAsync(
            new CreateAdministracionComiteInput(
                "Comité 2026-2028",
                new DateOnly(2026, 1, 1)),
            actor.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Activa);
        Assert.Null(result.Value.FechaFin);

        Assert.Single(await db.AdministracionesComite.ToListAsync());

        Assert.Contains(
            await db.Auditorias.ToListAsync(),
            a => a.Accion == "ADMINISTRACION.CREAR");
    }

    [Fact]
    public async Task Create_rejects_second_active_administration()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);

        db.AdministracionesComite.Add(
            new Panyebar.Domain.Entities.AdministracionComite
            {
                Nombre = "Actual",
                FechaInicio = new DateTime(2026, 1, 1),
                Estado = EstadoRegistro.Activo
            });

        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);

        var result = await service.CreateAsync(
            new CreateAdministracionComiteInput(
                "Otra",
                new DateOnly(2026, 2, 1)),
            actor.Id);

        Assert.Equal(
            AdministracionComiteError.Conflict,
            result.Error);

        Assert.Single(await db.AdministracionesComite.ToListAsync());
    }

    [Fact]
    public async Task SetIntegrante_assigns_active_person_and_cargo()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);
        var administracion = await SeedAdministration(db);

        var persona = new Persona
        {
            Nombres = "Ana",
            Apellidos = "López",
            Estado = EstadoRegistro.Activo
        };

        var cargo = new Cargo
        {
            Nombre = "Presidenta",
            Estado = EstadoRegistro.Activo
        };

        db.Personas.Add(persona);
        db.Cargos.Add(cargo);
        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);

        var result = await service.SetIntegranteAsync(
            administracion.Id,
            new SetIntegranteAdministracionInput(
                persona.Id,
                cargo.Id),
            actor.Id);

        Assert.True(result.Succeeded);

        var integrante =
            Assert.Single(await db.IntegrantesAdministracion.ToListAsync());

        Assert.Equal(persona.Id, integrante.PersonaId);
        Assert.Equal(cargo.Id, integrante.CargoId);

        Assert.Contains(
            await db.Auditorias.ToListAsync(),
            a => a.Accion == "ADMINISTRACION.INTEGRANTE.ASIGNAR");
    }

    [Fact]
    public async Task SetIntegrante_replaces_previous_holder_of_same_cargo()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);
        var administracion = await SeedAdministration(db);

        var first = new Persona
        {
            Nombres = "Primera",
            Apellidos = "Persona",
            Estado = EstadoRegistro.Activo
        };

        var second = new Persona
        {
            Nombres = "Segunda",
            Apellidos = "Persona",
            Estado = EstadoRegistro.Activo
        };

        var cargo = new Cargo
        {
            Nombre = "Tesorero",
            Estado = EstadoRegistro.Activo
        };

        db.Personas.AddRange(first, second);
        db.Cargos.Add(cargo);
        await db.SaveChangesAsync();

        db.IntegrantesAdministracion.Add(
            new IntegranteAdministracion
            {
                AdministracionComiteId = administracion.Id,
                PersonaId = first.Id,
                CargoId = cargo.Id
            });

        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);

        var result = await service.SetIntegranteAsync(
            administracion.Id,
            new SetIntegranteAdministracionInput(
                second.Id,
                cargo.Id),
            actor.Id);

        Assert.True(result.Succeeded);

        var integrante =
            Assert.Single(await db.IntegrantesAdministracion.ToListAsync());

        Assert.Equal(second.Id, integrante.PersonaId);
        Assert.Equal(cargo.Id, integrante.CargoId);
    }

    [Fact]
    public async Task SetIntegrante_moves_person_from_previous_cargo()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);
        var administracion = await SeedAdministration(db);

        var persona = new Persona
        {
            Nombres = "Carlos",
            Apellidos = "Pérez",
            Estado = EstadoRegistro.Activo
        };

        var firstCargo = new Cargo
        {
            Nombre = "Vocal I",
            Estado = EstadoRegistro.Activo
        };

        var secondCargo = new Cargo
        {
            Nombre = "Secretario",
            Estado = EstadoRegistro.Activo
        };

        db.Personas.Add(persona);
        db.Cargos.AddRange(firstCargo, secondCargo);
        await db.SaveChangesAsync();

        db.IntegrantesAdministracion.Add(
            new IntegranteAdministracion
            {
                AdministracionComiteId = administracion.Id,
                PersonaId = persona.Id,
                CargoId = firstCargo.Id
            });

        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);

        var result = await service.SetIntegranteAsync(
            administracion.Id,
            new SetIntegranteAdministracionInput(
                persona.Id,
                secondCargo.Id),
            actor.Id);

        Assert.True(result.Succeeded);

        var integrante =
            Assert.Single(await db.IntegrantesAdministracion.ToListAsync());

        Assert.Equal(persona.Id, integrante.PersonaId);
        Assert.Equal(secondCargo.Id, integrante.CargoId);
    }

    [Fact]
    public async Task SetIntegrante_rejects_inactive_person_or_cargo()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);
        var administracion = await SeedAdministration(db);

        var persona = new Persona
        {
            Nombres = "Inactiva",
            Apellidos = "Persona",
            Estado = EstadoRegistro.Inactivo
        };

        var cargo = new Cargo
        {
            Nombre = "Presidente",
            Estado = EstadoRegistro.Activo
        };

        db.Personas.Add(persona);
        db.Cargos.Add(cargo);
        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);

        var result = await service.SetIntegranteAsync(
            administracion.Id,
            new SetIntegranteAdministracionInput(
                persona.Id,
                cargo.Id),
            actor.Id);

        Assert.Equal(
            AdministracionComiteError.Invalid,
            result.Error);

        Assert.Empty(await db.IntegrantesAdministracion.ToListAsync());
    }

    [Fact]
    public async Task Finish_closes_administration_and_preserves_integrantes()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);
        var administracion = await SeedAdministration(db);

        var persona = new Persona
        {
            Nombres = "María",
            Apellidos = "Gómez",
            Estado = EstadoRegistro.Activo
        };

        var cargo = new Cargo
        {
            Nombre = "Presidenta",
            Estado = EstadoRegistro.Activo
        };

        db.Personas.Add(persona);
        db.Cargos.Add(cargo);
        await db.SaveChangesAsync();

        db.IntegrantesAdministracion.Add(
            new IntegranteAdministracion
            {
                AdministracionComiteId = administracion.Id,
                PersonaId = persona.Id,
                CargoId = cargo.Id
            });

        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);

        var result = await service.FinishAsync(
            administracion.Id,
            new DateOnly(2026, 12, 31),
            actor.Id);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.Activa);
        Assert.Equal(
            new DateOnly(2026, 12, 31),
            result.Value.FechaFin);

        Assert.Single(await db.IntegrantesAdministracion.ToListAsync());

        Assert.Contains(
            await db.Auditorias.ToListAsync(),
            a => a.Accion == "ADMINISTRACION.FINALIZAR");
    }

    [Fact]
    public async Task Finish_rejects_date_before_start()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);
        var administracion = await SeedAdministration(db);
        var service = new AdministracionComiteService(db);

        var result = await service.FinishAsync(
            administracion.Id,
            new DateOnly(2025, 12, 31),
            actor.Id);

        Assert.Equal(
            AdministracionComiteError.Invalid,
            result.Error);

        Assert.Null(
            (await db.AdministracionesComite.FindAsync(
                administracion.Id))!.FechaFin);
    }

    [Fact]
    public async Task Historical_administration_cannot_change_integrantes()
    {
        await using var db = CreateContext();
        var actor = await SeedActor(db);

        var administracion =
            new Panyebar.Domain.Entities.AdministracionComite
            {
                Nombre = "Histórica",
                FechaInicio = new DateTime(2024, 1, 1),
                FechaFin = new DateTime(2025, 12, 31),
                Estado = EstadoRegistro.Inactivo
            };

        var persona = new Persona
        {
            Nombres = "Persona",
            Apellidos = "Histórica",
            Estado = EstadoRegistro.Activo
        };

        var cargo = new Cargo
        {
            Nombre = "Presidente",
            Estado = EstadoRegistro.Activo
        };

        db.AdministracionesComite.Add(administracion);
        db.Personas.Add(persona);
        db.Cargos.Add(cargo);
        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);

        var result = await service.SetIntegranteAsync(
            administracion.Id,
            new SetIntegranteAdministracionInput(
                persona.Id,
                cargo.Id),
            actor.Id);

        Assert.Equal(
            AdministracionComiteError.Conflict,
            result.Error);
    }

    [Fact]
    public async Task GetAll_returns_current_and_history_with_names()
    {
        await using var db = CreateContext();
        await SeedActor(db);

        var historical =
            new Panyebar.Domain.Entities.AdministracionComite
            {
                Nombre = "Comité 2024",
                FechaInicio = new DateTime(2024, 1, 1),
                FechaFin = new DateTime(2025, 12, 31),
                Estado = EstadoRegistro.Inactivo
            };

        var current =
            new Panyebar.Domain.Entities.AdministracionComite
            {
                Nombre = "Comité 2026",
                FechaInicio = new DateTime(2026, 1, 1),
                Estado = EstadoRegistro.Activo
            };

        var persona = new Persona
        {
            Nombres = "Juan",
            Apellidos = "Pérez",
            Estado = EstadoRegistro.Activo
        };

        var cargo = new Cargo
        {
            Nombre = "Presidente",
            Estado = EstadoRegistro.Activo
        };

        db.AdministracionesComite.AddRange(
            historical,
            current);

        db.Personas.Add(persona);
        db.Cargos.Add(cargo);

        await db.SaveChangesAsync();

        db.IntegrantesAdministracion.Add(
            new IntegranteAdministracion
            {
                AdministracionComiteId = current.Id,
                PersonaId = persona.Id,
                CargoId = cargo.Id
            });

        await db.SaveChangesAsync();

        var service = new AdministracionComiteService(db);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Comité 2026", result[0].Nombre);
        Assert.True(result[0].Activa);

        var integrante = Assert.Single(result[0].Integrantes);

        Assert.Equal("Juan Pérez", integrante.PersonaNombre);
        Assert.Equal("Presidente", integrante.CargoNombre);

        Assert.False(result[1].Activa);
    }

    private static async Task<UsuarioAdministrativo> SeedActor(
        PanyebarDbContext db)
    {
        var actor = new UsuarioAdministrativo
        {
            NombreUsuario = "demo.admin",
            PasswordHash =
                new PasswordHasherAdapter().Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        db.UsuariosAdministrativos.Add(actor);
        await db.SaveChangesAsync();

        return actor;
    }

    private static async Task<Panyebar.Domain.Entities.AdministracionComite>
        SeedAdministration(PanyebarDbContext db)
    {
        var entity =
            new Panyebar.Domain.Entities.AdministracionComite
            {
                Nombre = "Comité actual",
                FechaInicio = new DateTime(2026, 1, 1),
                Estado = EstadoRegistro.Activo
            };

        db.AdministracionesComite.Add(entity);
        await db.SaveChangesAsync();

        return entity;
    }

    private static PanyebarDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PanyebarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new PanyebarDbContext(options);
    }
}

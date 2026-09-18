using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Abastecimiento;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class AbastecimientoServiceTests
{
    [Fact]
    public async Task Create_creates_programmed_schedule_and_audit()
    {
        await using var db = CreateContext();
        db.Sectores.Add(new Sector
        {
            Id = 1,
            Nombre = "Centro",
            Estado = EstadoRegistro.Activo
        });
        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateAsync(
            new ProgramacionAbastecimientoInput(
                1,
                new DateOnly(2026, 9, 20),
                new TimeSpan(6, 0, 0),
                new TimeSpan(10, 0, 0),
                "Turno matutino"),
            10);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadosProgramacionAbastecimiento.Programado, result.Value!.Estado);
        Assert.Equal("Centro", result.Value.SectorNombre);
        Assert.Single(db.ProgramacionesAbastecimiento);
        Assert.Contains(db.Auditorias, a => a.Accion == "ABASTECIMIENTO.CREAR");
    }

    [Fact]
    public async Task Create_rejects_inactive_sector()
    {
        await using var db = CreateContext();
        db.Sectores.Add(new Sector
        {
            Id = 1,
            Nombre = "Centro",
            Estado = EstadoRegistro.Inactivo
        });
        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateAsync(
            Input(1, 6, 10),
            10);

        Assert.Equal(
            AbastecimientoOperationError.SectorInactive,
            result.Error);
    }

    [Fact]
    public async Task Create_rejects_invalid_interval()
    {
        await using var db = CreateContext();
        var service = new AbastecimientoService(db);

        var result = await service.CreateAsync(
            Input(1, 10, 6),
            10);

        Assert.Equal(
            AbastecimientoOperationError.Invalid,
            result.Error);
    }

    [Fact]
    public async Task Create_rejects_overlap_for_same_sector()
    {
        await using var db = CreateContext();
        db.Sectores.Add(new Sector
        {
            Id = 1,
            Nombre = "Centro",
            Estado = EstadoRegistro.Activo
        });

        db.ProgramacionesAbastecimiento.Add(new ProgramacionAbastecimiento
        {
            SectorId = 1,
            Fecha = new DateTime(2026, 9, 20),
            HoraInicio = new TimeSpan(6, 0, 0),
            HoraFin = new TimeSpan(10, 0, 0),
            Estado = EstadosProgramacionAbastecimiento.Programado
        });

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateAsync(
            Input(1, 9, 12),
            10);

        Assert.Equal(
            AbastecimientoOperationError.Conflict,
            result.Error);
    }

    [Fact]
    public async Task Create_allows_same_time_for_different_sectors()
    {
        await using var db = CreateContext();

        db.Sectores.AddRange(
            new Sector
            {
                Id = 1,
                Nombre = "Centro",
                Estado = EstadoRegistro.Activo
            },
            new Sector
            {
                Id = 2,
                Nombre = "Norte",
                Estado = EstadoRegistro.Activo
            });

        db.ProgramacionesAbastecimiento.Add(new ProgramacionAbastecimiento
        {
            SectorId = 1,
            Fecha = new DateTime(2026, 9, 20),
            HoraInicio = new TimeSpan(6, 0, 0),
            HoraFin = new TimeSpan(10, 0, 0),
            Estado = EstadosProgramacionAbastecimiento.Programado
        });

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateAsync(
            Input(2, 6, 10),
            10);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Update_only_allows_programmed_schedule()
    {
        await using var db = CreateContext();

        db.Sectores.Add(new Sector
        {
            Id = 1,
            Nombre = "Centro",
            Estado = EstadoRegistro.Activo
        });

        db.ProgramacionesAbastecimiento.Add(new ProgramacionAbastecimiento
        {
            Id = 5,
            SectorId = 1,
            Fecha = new DateTime(2026, 9, 20),
            HoraInicio = new TimeSpan(6, 0, 0),
            HoraFin = new TimeSpan(10, 0, 0),
            Estado = EstadosProgramacionAbastecimiento.Completado
        });

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.UpdateAsync(
            5,
            Input(1, 7, 11),
            10);

        Assert.Equal(
            AbastecimientoOperationError.Conflict,
            result.Error);
    }

    [Fact]
    public async Task Complete_changes_state_and_terminal_state_cannot_change_again()
    {
        await using var db = CreateContext();

        db.Sectores.Add(new Sector
        {
            Id = 1,
            Nombre = "Centro",
            Estado = EstadoRegistro.Activo
        });

        db.ProgramacionesAbastecimiento.Add(new ProgramacionAbastecimiento
        {
            Id = 5,
            SectorId = 1,
            Fecha = new DateTime(2026, 9, 20),
            HoraInicio = new TimeSpan(6, 0, 0),
            HoraFin = new TimeSpan(10, 0, 0),
            Estado = EstadosProgramacionAbastecimiento.Programado
        });

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var completed = await service.CompleteAsync(5, 10);
        var second = await service.CancelAsync(5, null, 10);

        Assert.True(completed.Succeeded);
        Assert.Equal(
            EstadosProgramacionAbastecimiento.Completado,
            completed.Value!.Estado);

        Assert.Equal(
            AbastecimientoOperationError.Conflict,
            second.Error);
    }

    [Fact]
    public async Task Cancel_changes_state_and_can_store_observation()
    {
        await using var db = CreateContext();

        db.Sectores.Add(new Sector
        {
            Id = 1,
            Nombre = "Centro",
            Estado = EstadoRegistro.Activo
        });

        db.ProgramacionesAbastecimiento.Add(new ProgramacionAbastecimiento
        {
            Id = 5,
            SectorId = 1,
            Fecha = new DateTime(2026, 9, 20),
            HoraInicio = new TimeSpan(6, 0, 0),
            HoraFin = new TimeSpan(10, 0, 0),
            Estado = EstadosProgramacionAbastecimiento.Programado
        });

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CancelAsync(
            5,
            new ActualizarEstadoAbastecimientoInput(
                "Mantenimiento de línea"),
            10);

        Assert.True(result.Succeeded);
        Assert.Equal(
            EstadosProgramacionAbastecimiento.Cancelado,
            result.Value!.Estado);
        Assert.Equal(
            "Mantenimiento de línea",
            result.Value.Observacion);
    }

    [Fact]
    public async Task GetAll_filters_by_date_and_sector()
    {
        await using var db = CreateContext();

        db.Sectores.AddRange(
            new Sector
            {
                Id = 1,
                Nombre = "Centro",
                Estado = EstadoRegistro.Activo
            },
            new Sector
            {
                Id = 2,
                Nombre = "Norte",
                Estado = EstadoRegistro.Activo
            });

        db.ProgramacionesAbastecimiento.AddRange(
            Schedule(1, 1, 20),
            Schedule(2, 1, 21),
            Schedule(3, 2, 20));

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.GetAllAsync(
            new DateOnly(2026, 9, 20),
            new DateOnly(2026, 9, 20),
            1);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        Assert.Equal(1, result.Value![0].Id);
    }

    [Fact]
    public async Task GetAll_rejects_invalid_range()
    {
        await using var db = CreateContext();
        var service = new AbastecimientoService(db);

        var result = await service.GetAllAsync(
            new DateOnly(2026, 9, 21),
            new DateOnly(2026, 9, 20),
            null);

        Assert.Equal(
            AbastecimientoOperationError.Invalid,
            result.Error);
    }

    private static ProgramacionAbastecimientoInput Input(
        int sectorId,
        int startHour,
        int endHour) =>
        new(
            sectorId,
            new DateOnly(2026, 9, 20),
            TimeSpan.FromHours(startHour),
            TimeSpan.FromHours(endHour),
            null);

    private static ProgramacionAbastecimiento Schedule(
        int id,
        int sectorId,
        int day) =>
        new()
        {
            Id = id,
            SectorId = sectorId,
            Fecha = new DateTime(2026, 9, day),
            HoraInicio = new TimeSpan(6, 0, 0),
            HoraFin = new TimeSpan(10, 0, 0),
            Estado = EstadosProgramacionAbastecimiento.Programado
        };

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PanyebarDbContext(options);
    }
}

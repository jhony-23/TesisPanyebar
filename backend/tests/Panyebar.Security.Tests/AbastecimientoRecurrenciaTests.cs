using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Abastecimiento;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class AbastecimientoRecurrenciaTests
{
    [Fact]
    public async Task Update_excludes_current_schedule_from_overlap()
    {
        await using var db = CreateContext();

        db.Sectores.Add(ActiveSector());

        db.ProgramacionesAbastecimiento.Add(new ProgramacionAbastecimiento
        {
            Id = 1,
            SectorId = 1,
            Fecha = new DateTime(2026, 9, 20),
            HoraInicio = new TimeSpan(6, 0, 0),
            HoraFin = new TimeSpan(10, 0, 0),
            Estado = EstadosProgramacionAbastecimiento.Programado
        });

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.UpdateAsync(
            1,
            new ProgramacionAbastecimientoInput(
                1,
                new DateOnly(2026, 9, 21),
                new TimeSpan(6, 30, 0),
                new TimeSpan(10, 30, 0),
                "Editada"),
            10);

        Assert.True(result.Succeeded);
        Assert.Equal(
            new DateTime(2026, 9, 21),
            result.Value!.Fecha);
    }

    [Fact]
    public async Task Weekly_recurrence_creates_independent_occurrences()
    {
        await using var db = CreateContext();
        db.Sectores.Add(ActiveSector());
        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateRecurringAsync(
            Recurring(
                TipoRecurrenciaAbastecimiento.Semanal,
                count: 4),
            10);

        Assert.True(result.Succeeded);
        Assert.Equal(4, result.Value!.CantidadCreada);

        Assert.Equal(
            new[]
            {
                new DateTime(2026, 9, 20),
                new DateTime(2026, 9, 27),
                new DateTime(2026, 10, 4),
                new DateTime(2026, 10, 11)
            },
            result.Value.Programaciones
                .Select(x => x.Fecha)
                .ToArray());

        Assert.All(
            result.Value.Programaciones,
            item => Assert.Equal(
                EstadosProgramacionAbastecimiento.Programado,
                item.Estado));
    }

    [Fact]
    public async Task Monthly_recurrence_skips_month_without_original_day()
    {
        await using var db = CreateContext();
        db.Sectores.Add(ActiveSector());
        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateRecurringAsync(
            new ProgramacionRecurrenteAbastecimientoInput(
                1,
                new DateOnly(2026, 1, 31),
                new TimeSpan(6, 0, 0),
                new TimeSpan(10, 0, 0),
                null,
                TipoRecurrenciaAbastecimiento.Mensual,
                3,
                null),
            10);

        Assert.True(result.Succeeded);

        Assert.Equal(
            new[]
            {
                new DateTime(2026, 1, 31),
                new DateTime(2026, 3, 31),
                new DateTime(2026, 5, 31)
            },
            result.Value!.Programaciones
                .Select(x => x.Fecha)
                .ToArray());
    }

    [Fact]
    public async Task Annual_february_29_skips_non_leap_years()
    {
        await using var db = CreateContext();
        db.Sectores.Add(ActiveSector());
        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateRecurringAsync(
            new ProgramacionRecurrenteAbastecimientoInput(
                1,
                new DateOnly(2028, 2, 29),
                new TimeSpan(6, 0, 0),
                new TimeSpan(10, 0, 0),
                null,
                TipoRecurrenciaAbastecimiento.Anual,
                3,
                null),
            10);

        Assert.True(result.Succeeded);

        Assert.Equal(
            new[]
            {
                new DateTime(2028, 2, 29),
                new DateTime(2032, 2, 29),
                new DateTime(2036, 2, 29)
            },
            result.Value!.Programaciones
                .Select(x => x.Fecha)
                .ToArray());
    }

    [Fact]
    public async Task Recurrence_by_end_date_stops_at_requested_date()
    {
        await using var db = CreateContext();
        db.Sectores.Add(ActiveSector());
        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var input = Recurring(
            TipoRecurrenciaAbastecimiento.Semanal,
            count: null,
            endDate: new DateOnly(2026, 10, 5));

        var result = await service.CreateRecurringAsync(input, 10);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Value!.CantidadCreada);
    }

    [Fact]
    public async Task Recurrence_rejects_more_than_52_occurrences()
    {
        await using var db = CreateContext();
        db.Sectores.Add(ActiveSector());
        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateRecurringAsync(
            Recurring(
                TipoRecurrenciaAbastecimiento.Semanal,
                count: 53),
            10);

        Assert.Equal(
            AbastecimientoOperationError.Invalid,
            result.Error);

        Assert.Empty(db.ProgramacionesAbastecimiento);
    }

    [Fact]
    public async Task Recurrence_rejects_conflict_without_creating_partial_series()
    {
        await using var db = CreateContext();

        db.Sectores.Add(ActiveSector());

        db.ProgramacionesAbastecimiento.Add(
            new ProgramacionAbastecimiento
            {
                SectorId = 1,
                Fecha = new DateTime(2026, 9, 27),
                HoraInicio = new TimeSpan(8, 0, 0),
                HoraFin = new TimeSpan(12, 0, 0),
                Estado = EstadosProgramacionAbastecimiento.Programado
            });

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateRecurringAsync(
            Recurring(
                TipoRecurrenciaAbastecimiento.Semanal,
                count: 4),
            10);

        Assert.Equal(
            AbastecimientoOperationError.Conflict,
            result.Error);

        Assert.Single(db.ProgramacionesAbastecimiento);
    }

    [Fact]
    public async Task Recurring_creation_adds_audit_for_each_occurrence()
    {
        await using var db = CreateContext();
        db.Sectores.Add(ActiveSector());
        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.CreateRecurringAsync(
            Recurring(
                TipoRecurrenciaAbastecimiento.Semanal,
                count: 3),
            10);

        Assert.True(result.Succeeded);

        Assert.Equal(
            3,
            db.Auditorias.Count(
                a => a.Accion ==
                    "ABASTECIMIENTO.CREAR.RECURRENTE"));
    }

    [Fact]
    public async Task GetAll_preserves_real_schedule_id()
    {
        await using var db = CreateContext();

        db.Sectores.Add(ActiveSector());

        db.ProgramacionesAbastecimiento.Add(
            new ProgramacionAbastecimiento
            {
                Id = 37,
                SectorId = 1,
                Fecha = new DateTime(2026, 9, 17),
                HoraInicio = new TimeSpan(6, 11, 0),
                HoraFin = new TimeSpan(10, 0, 0),
                Estado = EstadosProgramacionAbastecimiento.Programado,
                Observacion = "Prueba ID"
            });

        await db.SaveChangesAsync();

        var service = new AbastecimientoService(db);

        var result = await service.GetAllAsync(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            null);

        Assert.True(result.Succeeded);

        var item = Assert.Single(result.Value!);

        Assert.Equal(37, item.Id);
        Assert.True(item.Id > 0);
    }
    private static ProgramacionRecurrenteAbastecimientoInput Recurring(
        TipoRecurrenciaAbastecimiento type,
        int? count,
        DateOnly? endDate = null) =>
        new(
            1,
            new DateOnly(2026, 9, 20),
            new TimeSpan(6, 0, 0),
            new TimeSpan(10, 0, 0),
            null,
            type,
            count,
            endDate);

    private static Sector ActiveSector() =>
        new()
        {
            Id = 1,
            Nombre = "Centro",
            Estado = EstadoRegistro.Activo
        };

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PanyebarDbContext(options);
    }
}

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Finanzas;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class FinanzaServiceTests
{
    private static readonly DateOnly Day = new(2026, 9, 16);

    [Fact]
    public async Task Register_PreservesCivilDateAndCreatesUtcAudit()
    {
        using var db = Context();
        var result = await new FinanzaService(db).RegisterAsync(new("  Reparación  ", 30.25m, Day), 1);
        Assert.True(result.Succeeded);
        Assert.Equal("Reparación", result.Value!.Concepto);
        Assert.Equal(Day, result.Value.Fecha);
        Assert.Equal(EstadoEgreso.Registrado, result.Value.Estado);
        var row = Assert.Single(db.Egresos);
        Assert.Equal(DateTimeKind.Unspecified, row.Fecha.Kind);
        Assert.Equal(TimeSpan.Zero, row.Fecha.TimeOfDay);
        var audit = Assert.Single(db.Auditorias);
        Assert.Equal("EGRESO.REGISTRAR", audit.Accion);
        Assert.Equal("Egreso", audit.Entidad);
        Assert.Equal(row.Id, audit.EntidadId);
        Assert.Equal(1, audit.UsuarioAdministrativoId);
        Assert.Null(audit.ValorAnterior);
        Assert.Contains("Monto:30.25", audit.ValorNuevo);
        Assert.Equal(DateTimeKind.Utc, audit.Fecha.Kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.001)]
    [InlineData(10000000000000000)]
    public async Task Register_RejectsInvalidAmount(decimal amount)
    {
        using var db = Context();
        var result = await new FinanzaService(db).RegisterAsync(new("Gasto", amount, Day), 1);
        Assert.Equal(FinanzaOperationError.Invalid, result.Error);
        Assert.Empty(db.Egresos);
        Assert.Empty(db.Auditorias);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_RejectsMissingConcept(string? concept)
    {
        using var db = Context();
        Assert.Equal(FinanzaOperationError.Invalid,
            (await new FinanzaService(db).RegisterAsync(new(concept, 1m, Day), 1)).Error);
        Assert.Empty(db.Egresos);
    }

    [Fact]
    public async Task Register_RejectsLongConceptMissingDateAndUnknownAuthor()
    {
        using var db = Context();
        var service = new FinanzaService(db);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.RegisterAsync(new(new string('x', 201), 1m, Day), 1)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.RegisterAsync(new("Gasto", 1m, default), 1)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.RegisterAsync(new("Gasto", 1m, Day), 999)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.RegisterAsync(null!, 1)).Error);
        Assert.Empty(db.Egresos);
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task Update_ChangesOnlyFinancialFieldsAndAuditsBeforeAndAfter()
    {
        using var db = Context();
        var service = new FinanzaService(db);
        var created = (await service.RegisterAsync(new("Original", 10m, Day), 1)).Value!;
        var result = await service.UpdateAsync(created.Id, new("Corregido", 12.35m, Day.AddDays(1)), 2);
        Assert.True(result.Succeeded);
        Assert.Equal(created.Id, result.Value!.Id);
        Assert.Equal(1, result.Value.UsuarioAdministrativoId);
        Assert.Equal(EstadoEgreso.Registrado, result.Value.Estado);
        Assert.Equal(12.35m, result.Value.Monto);
        Assert.Equal(Day.AddDays(1), result.Value.Fecha);
        var audit = Assert.Single(db.Auditorias.Where(a => a.Accion == "EGRESO.EDITAR"));
        Assert.Equal(2, audit.UsuarioAdministrativoId);
        Assert.Equal(created.Id, audit.EntidadId);
        Assert.Equal(DateTimeKind.Utc, audit.Fecha.Kind);
        Assert.Equal("Concepto:Original; Monto:10.00; Fecha:2026-09-16; Estado:Registrado", audit.ValorAnterior);
        Assert.Equal("Concepto:Corregido; Monto:12.35; Fecha:2026-09-17; Estado:Registrado", audit.ValorNuevo);
        Assert.Equal(result.Value, await service.GetEgresoByIdAsync(created.Id));
    }

    [Fact]
    public async Task Annul_PreservesRowExcludesTotalsAndRejectsRepeatAndEdit()
    {
        using var db = Context();
        var service = new FinanzaService(db);
        var id = (await service.RegisterAsync(new("Gasto", 10m, Day), 1)).Value!.Id;
        Assert.True((await service.AnnulAsync(id, 2)).Succeeded);
        Assert.Equal(EstadoEgreso.Anulado, Assert.Single(db.Egresos).Estado);
        Assert.Equal(FinanzaOperationError.Conflict, (await service.AnnulAsync(id, 2)).Error);
        Assert.Equal(FinanzaOperationError.Conflict, (await service.UpdateAsync(id, new("Otro", 20m, Day), 2)).Error);
        var audit = Assert.Single(db.Auditorias.Where(a => a.Accion == "EGRESO.ANULAR"));
        Assert.Equal(2, audit.UsuarioAdministrativoId);
        Assert.Contains("Estado:Registrado", audit.ValorAnterior);
        Assert.Contains("Estado:Anulado", audit.ValorNuevo);
        Assert.Equal(2, db.Auditorias.Count());
        Assert.Equal(0m, (await service.GetResumenAsync(null, null)).Value!.EgresosTotales);
        Assert.Empty((await service.GetMovimientosAsync(null, null, null)).Value!);
        Assert.Single((await service.GetEgresosAsync(null, null, EstadoEgreso.Anulado)).Value!);
    }

    [Fact]
    public async Task MissingExpenseAndInvalidUpdates_DoNotWriteAudit()
    {
        using var db = Context();
        var service = new FinanzaService(db);
        Assert.Null(await service.GetEgresoByIdAsync(999));
        Assert.Equal(FinanzaOperationError.NotFound, (await service.UpdateAsync(999, new("Gasto", 1m, Day), 1)).Error);
        Assert.Equal(FinanzaOperationError.NotFound, (await service.AnnulAsync(999, 1)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.UpdateAsync(1, new("Gasto", -1m, Day), 1)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.AnnulAsync(0, 1)).Error);
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task Finance_UsesOnlyRegisteredPaymentsAndExpensesWithDecimalBalance()
    {
        using var db = Context();
        db.Pagos.AddRange(Payment(1, 100.10m), Payment(2, 500m, EstadoPago.Anulado));
        db.Egresos.AddRange(Expense(1, 30.05m), Expense(2, 200m, EstadoEgreso.Anulado));
        db.SaveChanges();
        var service = new FinanzaService(db);
        var summary = (await service.GetResumenAsync(null, null)).Value!;
        Assert.Equal(new ResumenFinancieroDto(100.10m, 30.05m, 70.05m), summary);
        var income = Assert.Single((await service.GetIngresosAsync(null, null)).Value!);
        Assert.Equal(1, income.PagoId);
        Assert.Equal(EstadoPago.Registrado, income.Estado);
        var movements = (await service.GetMovimientosAsync(null, null, null)).Value!;
        Assert.Equal(2, movements.Count);
        Assert.Equal(TipoMovimientoFinanciero.Ingreso, movements[0].Tipo);
        Assert.Equal(TipoMovimientoFinanciero.Egreso, movements[1].Tipo);
        Assert.All(movements, m => Assert.Equal("Registrado", m.Estado));
        Assert.Single((await service.GetMovimientosAsync(null, null, TipoMovimientoFinanciero.Ingreso)).Value!);
        Assert.Single((await service.GetMovimientosAsync(null, null, TipoMovimientoFinanciero.Egreso)).Value!);
        Assert.Empty(db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged));
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task EmptyFinance_ReturnsZeroTotalsAndEmptyLists()
    {
        using var db = Context();
        var service = new FinanzaService(db);
        Assert.Equal(new ResumenFinancieroDto(0m, 0m, 0m), (await service.GetResumenAsync(null, null)).Value);
        Assert.Empty((await service.GetIngresosAsync(null, null)).Value!);
        Assert.Empty((await service.GetMovimientosAsync(null, null, null)).Value!);
    }

    [Fact]
    public async Task DateFilters_UseGuatemalaMidnightForPaymentsAndUnshiftedExpenseDates()
    {
        using var db = Context();
        var start = new DateTime(2026, 9, 16, 6, 0, 0, DateTimeKind.Utc);
        var times = new[] { start.AddTicks(-1), start, start.AddDays(1).AddTicks(-1), start.AddDays(1) };
        for (var index = 0; index < times.Length; index++)
        {
            var pago = Payment(index + 1, 10m);
            pago.Fecha = times[index];
            db.Pagos.Add(pago);
        }
        db.Egresos.AddRange(Expense(1, 3m), Expense(2, 4m));
        db.Egresos.Local.Single(e => e.Id == 2).Fecha = Day.AddDays(1).ToDateTime(TimeOnly.MinValue);
        db.SaveChanges();
        var service = new FinanzaService(db);
        var incomes = (await service.GetIngresosAsync(Day, Day)).Value!;
        Assert.Equal(new[] { 3, 2 }, incomes.Select(p => p.PagoId));
        Assert.All(incomes, p => { Assert.Equal(Day, p.FechaOperativa); Assert.Equal(DateTimeKind.Utc, p.Fecha.Kind); });
        Assert.Single((await service.GetEgresosAsync(Day, Day, null)).Value!);
        Assert.Equal(new ResumenFinancieroDto(20m, 3m, 17m), (await service.GetResumenAsync(Day, Day)).Value);
        Assert.Equal(3, (await service.GetIngresosAsync(Day, null)).Value!.Count);
        Assert.Equal(3, (await service.GetIngresosAsync(null, Day)).Value!.Count);
        var movements = (await service.GetMovimientosAsync(Day, Day, null)).Value!;
        Assert.Equal(new[] { 3, 2, 1 }, movements.Select(m => m.ReferenciaId));
        Assert.All(movements, m => Assert.Equal(Day, m.Fecha));
    }

    [Fact]
    public async Task InvalidRangesAndEnums_AreRejectedAcrossQueries()
    {
        using var db = Context();
        var service = new FinanzaService(db);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.GetEgresosAsync(Day.AddDays(1), Day, null)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.GetIngresosAsync(Day.AddDays(1), Day)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.GetMovimientosAsync(Day.AddDays(1), Day, null)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.GetResumenAsync(Day.AddDays(1), Day)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.GetEgresosAsync(null, null, (EstadoEgreso)99)).Error);
        Assert.Equal(FinanzaOperationError.Invalid, (await service.GetMovimientosAsync(null, null, (TipoMovimientoFinanciero)99)).Error);
    }

    [Fact]
    public async Task MaximumDateFilter_DoesNotOverflow()
    {
        using var db = Context();
        var service = new FinanzaService(db);
        Assert.True((await service.GetResumenAsync(DateOnly.MaxValue, DateOnly.MaxValue)).Succeeded);
        Assert.True((await service.GetEgresosAsync(DateOnly.MinValue, DateOnly.MaxValue, null)).Succeeded);
    }

    [Fact]
    public void SqlServer_DateAndStateQueriesTranslateBeforeMaterialization()
    {
        using var db = new PanyebarDbContext(new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;").Options);
        var service = new FinanzaService(db);
        var payments = (IQueryable<Pago>)typeof(FinanzaService).GetMethod("PagosValidos", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object?[] { Day, Day })!;
        var expenses = (IQueryable<Egreso>)typeof(FinanzaService).GetMethod("Egresos", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object?[] { Day, Day })!;
        var sql = payments.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id).ToQueryString();
        Assert.Contains("06:00:00", sql);
        Assert.Contains("[Estado] = 1", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("00:00:00", expenses.ToQueryString());
        Assert.DoesNotContain("06:00:00", expenses.ToQueryString());
    }

    private static PanyebarDbContext Context()
    {
        var db = new PanyebarDbContext(new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.UsuariosAdministrativos.AddRange(new UsuarioAdministrativo { Id = 1, NombreUsuario = "autor" },
            new UsuarioAdministrativo { Id = 2, NombreUsuario = "editor" });
        db.SaveChanges();
        return db;
    }
    private static Pago Payment(int id, decimal amount, EstadoPago state = EstadoPago.Registrado) => new()
    {
        Id = id, Monto = amount, Estado = state, Concepto = "Pago", UsuarioAdministrativoId = 1,
        Fecha = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc)
    };
    private static Egreso Expense(int id, decimal amount, EstadoEgreso state = EstadoEgreso.Registrado) => new()
    {
        Id = id, Monto = amount, Estado = state, Concepto = "Gasto", UsuarioAdministrativoId = 1,
        Fecha = Day.ToDateTime(TimeOnly.MinValue)
    };
}

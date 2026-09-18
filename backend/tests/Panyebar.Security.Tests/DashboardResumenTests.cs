using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.DashboardReportes;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class DashboardResumenTests
{
    private static readonly DateTime Start = new(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2027, 1, 1, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CivilStart = new(2026, 12, 1);
    private static readonly DateTime CivilEnd = new(2027, 1, 1);

    [Fact]
    public async Task EmptyMonth_ReturnsZeroTotalsWithoutTrackingOrAudit()
    {
        using var db = Context();
        var result = await new DashboardReportesService(db).GetResumenAsync(2026, 12);
        Assert.True(result.Succeeded);
        Assert.Equal(new DashboardResumenDto(2026, 12, 0m, 0m, 0m, 0, 0, 0m), result.Value);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task CurrentStates_ExcludeAnnulledPaymentsExpensesAndNonPendingObligations()
    {
        using var db = Context();
        db.Pagos.AddRange(Payment(1, Start, 100.25m), Payment(2, Start, 20m),
            Payment(3, Start, 500m, EstadoPago.Anulado));
        db.Egresos.AddRange(Expense(1, CivilStart, 30.10m),
            Expense(2, CivilStart, 600m, EstadoEgreso.Anulado));
        db.Obligaciones.AddRange(Obligation(1, Start, 25m),
            Obligation(2, Start, 40m, EstadoObligacion.Pagada),
            Obligation(3, Start, 60m, EstadoObligacion.Anulada));
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var result = await new DashboardReportesService(db).GetResumenAsync(2026, 12);
        Assert.Equal(new DashboardResumenDto(2026, 12, 120.25m, 30.10m, 90.15m, 2, 1, 25m), result.Value);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task MonthlyBoundaries_AreInclusiveExclusiveAndSeparateDecemberJanuary()
    {
        using var db = Context();
        var instants = new[] { Start.AddTicks(-1), Start, End.AddTicks(-1), End };
        var civilDates = new[] { CivilStart.AddDays(-1), CivilStart, CivilEnd.AddDays(-1), CivilEnd };
        for (var i = 0; i < instants.Length; i++)
        {
            var amount = (i + 1) * 10m;
            db.Pagos.Add(Payment(i + 1, instants[i], amount));
            db.Egresos.Add(Expense(i + 1, civilDates[i], amount / 2));
            db.Obligaciones.Add(Obligation(i + 1, instants[i], amount));
        }
        db.SaveChanges();
        var service = new DashboardReportesService(db);
        Assert.Equal(new DashboardResumenDto(2026, 12, 50m, 25m, 25m, 2, 2, 50m),
            (await service.GetResumenAsync(2026, 12)).Value);
        Assert.Equal(new DashboardResumenDto(2027, 1, 40m, 20m, 20m, 1, 1, 40m),
            (await service.GetResumenAsync(2027, 1)).Value);
        Assert.Equal(new DashboardResumenDto(2026, 11, 10m, 5m, 5m, 1, 1, 10m),
            (await service.GetResumenAsync(2026, 11)).Value);
        // El reporte global sigue incluyendo todos los meses.
        Assert.Equal(4, (await service.GetObligacionesPendientesAsync()).Count);
    }

    [Fact]
    public async Task PendingMonth_UsesGenerationNotPeriodDueDateOrArrears()
    {
        using var db = Context();
        var overdue = Obligation(1, Start, 10m);
        overdue.FechaVencimiento = DateTime.UtcNow.AddYears(-1);
        overdue.Periodo = "2025";
        var future = Obligation(2, Start, 20m);
        future.FechaVencimiento = DateTime.UtcNow.AddYears(1);
        var noDueDate = Obligation(3, Start, 30m);
        noDueDate.PersonaId = null;
        noDueDate.SuministroId = 1;
        noDueDate.Origen = OrigenObligacion.CuotaOrdinaria;
        var outside = Obligation(4, Start.AddMonths(-1), 500m);
        outside.Periodo = "2026-12";
        outside.FechaVencimiento = CivilStart;
        db.Obligaciones.AddRange(overdue, future, noDueDate, outside);
        db.SaveChanges();
        var summary = (await new DashboardReportesService(db).GetResumenAsync(2026, 12)).Value!;
        Assert.Equal(3, summary.CantidadObligacionesPendientes);
        Assert.Equal(60m, summary.MontoObligacionesPendientes);
    }

    [Fact]
    public async Task Summary_RecomputesCurrentStatesAfterAnnulment()
    {
        using var db = Context();
        var payment = Payment(1, Start, 20m);
        var obligation = Obligation(1, Start, 20m, EstadoObligacion.Pagada);
        db.Pagos.Add(payment);
        db.Obligaciones.Add(obligation);
        db.Egresos.Add(Expense(1, CivilStart, 5m));
        db.SaveChanges();
        var service = new DashboardReportesService(db);
        Assert.Equal(new DashboardResumenDto(2026, 12, 20m, 5m, 15m, 1, 0, 0m),
            (await service.GetResumenAsync(2026, 12)).Value);
        payment.Estado = EstadoPago.Anulado;
        obligation.Estado = EstadoObligacion.Pendiente;
        db.SaveChanges();
        Assert.Equal(new DashboardResumenDto(2026, 12, 0m, 5m, -5m, 0, 1, 20m),
            (await service.GetResumenAsync(2026, 12)).Value);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(10000, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(9999, 12)]
    public async Task InvalidMonth_ReturnsControlledFailure(int year, int month)
    {
        using var db = Context();
        var result = await new DashboardReportesService(db).GetResumenAsync(year, month);
        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(9999, 11)]
    [InlineData(2028, 2)]
    public async Task RepresentableMonth_DoesNotOverflow(int year, int month)
    {
        using var db = Context();
        Assert.True((await new DashboardReportesService(db).GetResumenAsync(year, month)).Succeeded);
    }

    [Theory]
    [InlineData("PagosResumenQuery", "Fecha")]
    [InlineData("EgresosResumenQuery", "Fecha")]
    [InlineData("ObligacionesResumenQuery", "FechaGeneracion")]
    public void SqlServer_TranslatesCompleteMonthlyAggregations(string method, string dateColumn)
    {
        using var db = new PanyebarDbContext(new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;").Options);
        var civil = method == "EgresosResumenQuery";
        var query = (IQueryable)typeof(DashboardReportesService)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(new DashboardReportesService(db), new object[] { civil ? CivilStart : Start, civil ? CivilEnd : End })!;
        var sql = query.ToQueryString();
        Assert.Contains("COUNT(*)", sql);
        Assert.Contains("SUM(", sql);
        Assert.Contains("[Estado] = 1", sql);
        Assert.Contains($"[{dateColumn}] >=", sql);
        Assert.Contains($"[{dateColumn}] <", sql);
        Assert.Contains(civil ? "00:00:00" : "06:00:00", sql);
    }

    private static PanyebarDbContext Context() => new(new DbContextOptionsBuilder<PanyebarDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Pago Payment(int id, DateTime date, decimal amount, EstadoPago state = EstadoPago.Registrado) =>
        new() { Id = id, Fecha = date, Monto = amount, Estado = state, Concepto = "Pago", UsuarioAdministrativoId = 1 };
    private static Egreso Expense(int id, DateTime date, decimal amount, EstadoEgreso state = EstadoEgreso.Registrado) =>
        new() { Id = id, Fecha = date, Monto = amount, Estado = state, Concepto = "Egreso", UsuarioAdministrativoId = 1 };
    private static Obligacion Obligation(int id, DateTime date, decimal amount, EstadoObligacion state = EstadoObligacion.Pendiente) =>
        new() { Id = id, FechaGeneracion = date, Monto = amount, Estado = state, PersonaId = 1,
            Concepto = "Obligación", Origen = OrigenObligacion.Jornada };
}

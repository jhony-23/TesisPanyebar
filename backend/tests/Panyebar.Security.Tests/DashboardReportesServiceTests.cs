using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.DashboardReportes;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class DashboardReportesServiceTests
{
    private static readonly DateOnly Day = new(2026, 9, 16);
    private static readonly DateTime Start = new(2026, 9, 16, 6, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Reports_EmptyDatabase_ReturnEmptyListsWithoutWrites()
    {
        using var db = Context();
        var service = new DashboardReportesService(db);
        Assert.Empty((await service.GetPagosAsync(null, null)).Value!);
        Assert.Empty(await service.GetObligacionesPendientesAsync());
        Assert.Empty((await service.GetJornadasAsync(null, null)).Value!);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task Payments_HistoryPreservesAnnulledAndDoesNotMultiplyApplications()
    {
        using var db = Context();
        db.Pagos.AddRange(Payment(1, Start, 30m), Payment(2, Start, 30m, EstadoPago.Anulado));
        db.AplicacionesPago.AddRange(
            new AplicacionPago { Id = 1, PagoId = 1, ObligacionId = 1 },
            new AplicacionPago { Id = 2, PagoId = 1, ObligacionId = 2 },
            new AplicacionPago { Id = 3, PagoId = 2, ObligacionId = 1 });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var rows = (await new DashboardReportesService(db).GetPagosAsync(Day, Day)).Value!;
        Assert.Equal(new[] { 2, 1 }, rows.Select(p => p.PagoId));
        Assert.Equal(EstadoPago.Anulado, rows[0].Estado);
        Assert.Equal(new ReportePagoDto(1, Start, "Pago 1", 30m, EstadoPago.Registrado, 1), rows[1]);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task Payments_DateRangeUsesGuatemalaAndInclusiveCivilEnd()
    {
        using var db = Context();
        db.Pagos.AddRange(Payment(1, Start.AddTicks(-1)), Payment(2, Start),
            Payment(3, Start.AddDays(1).AddTicks(-1)), Payment(4, Start.AddDays(1)));
        db.SaveChanges();
        var service = new DashboardReportesService(db);
        Assert.Equal(new[] { 3, 2 }, (await service.GetPagosAsync(Day, Day)).Value!.Select(p => p.PagoId));
        Assert.Equal(new[] { 4, 3, 2 }, (await service.GetPagosAsync(Day, null)).Value!.Select(p => p.PagoId));
        Assert.Equal(new[] { 3, 2, 1 }, (await service.GetPagosAsync(null, Day)).Value!.Select(p => p.PagoId));
    }

    [Fact]
    public async Task Reports_InvalidRangeRejectedAndExtremeDatesDoNotOverflow()
    {
        using var db = Context();
        var service = new DashboardReportesService(db);
        Assert.False((await service.GetPagosAsync(Day.AddDays(1), Day)).Succeeded);
        Assert.False((await service.GetJornadasAsync(Day.AddDays(1), Day)).Succeeded);
        Assert.True((await service.GetPagosAsync(DateOnly.MinValue, DateOnly.MaxValue)).Succeeded);
        Assert.True((await service.GetJornadasAsync(DateOnly.MaxValue, DateOnly.MaxValue)).Succeeded);
    }

    [Fact]
    public async Task Obligations_OnlyPendingRepresentBothOwnersAndCalculatedArrears()
    {
        using var db = Context();
        db.Personas.Add(new Persona { Id = 1, Nombres = "Ana", Apellidos = "Pérez" });
        db.Suministros.Add(new Suministro { Id = 1, Nis = "PAN-000001", CodigoQrToken = "not-for-reports" });
        db.Obligaciones.AddRange(
            new Obligacion { Id = 1, PersonaId = 1, Origen = OrigenObligacion.Jornada,
                Concepto = "Ausencia", Monto = 30m, FechaGeneracion = Start },
            new Obligacion { Id = 2, SuministroId = 1, Origen = OrigenObligacion.CuotaOrdinaria,
                Concepto = "Cuota", Monto = 35m, Periodo = "2026", FechaGeneracion = Start,
                FechaVencimiento = DateTime.UtcNow.AddDays(-1) },
            new Obligacion { Id = 3, PersonaId = 1, Monto = 10m, FechaVencimiento = DateTime.UtcNow.AddDays(1) },
            new Obligacion { Id = 4, PersonaId = 1, Estado = EstadoObligacion.Pagada, Monto = 50m,
                FechaVencimiento = DateTime.UtcNow.AddDays(-1) },
            new Obligacion { Id = 5, PersonaId = 1, Estado = EstadoObligacion.Anulada, Monto = 50m,
                FechaVencimiento = DateTime.UtcNow.AddDays(-1) });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var rows = await new DashboardReportesService(db).GetObligacionesPendientesAsync();
        Assert.Equal(new[] { 2, 1, 3 }, rows.Select(o => o.ObligacionId));
        var person = rows[1];
        Assert.Equal(1, person.PersonaId);
        Assert.Equal("Ana Pérez", person.NombrePersona);
        Assert.Null(person.SuministroId);
        Assert.Null(person.Nis);
        Assert.Equal(OrigenObligacion.Jornada, person.Origen);
        Assert.Equal(30m, person.Monto);
        Assert.False(person.EsMorosa);
        Assert.Null(person.Periodo);
        Assert.Null(person.FechaVencimiento);
        var supply = rows[0];
        Assert.Equal(1, supply.SuministroId);
        Assert.Equal("PAN-000001", supply.Nis);
        Assert.Null(supply.PersonaId);
        Assert.Null(supply.NombrePersona);
        Assert.Equal(35m, supply.Monto);
        Assert.Equal("2026", supply.Periodo);
        Assert.Equal(OrigenObligacion.CuotaOrdinaria, supply.Origen);
        Assert.True(supply.EsMorosa);
        Assert.False(rows[2].EsMorosa);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(db.Auditorias);
    }

    [Theory]
    [InlineData(EstadoJornada.Planificada)]
    [InlineData(EstadoJornada.Cerrada)]
    [InlineData(EstadoJornada.Cancelada)]
    public async Task Jornadas_ReportsActualResultsAndStateWithoutInventingParticipants(EstadoJornada state)
    {
        using var db = Context();
        db.Jornadas.AddRange(new Jornada { Id = 1, Nombre = "Limpieza", Fecha = Day.ToDateTime(TimeOnly.MinValue), Estado = state },
            new Jornada { Id = 2, Nombre = "Sin participantes", Fecha = Day.ToDateTime(TimeOnly.MinValue) });
        foreach (var result in Enum.GetValues<ResultadoParticipacionJornada>())
        {
            var id = (int)result + 1;
            db.Personas.Add(new Persona { Id = id, Nombres = $"Persona {id}", Apellidos = "Apellido" });
            db.ParticipacionesJornada.Add(new ParticipacionJornada {
                Id = id, JornadaId = 1, PersonaId = id, Resultado = result, Observacion = "Registrado" });
        }
        db.Personas.Add(new Persona { Id = 5, Nombres = "Sin participación" });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var rows = (await new DashboardReportesService(db).GetJornadasAsync(Day, Day)).Value!;
        Assert.Equal(4, rows.Count);
        Assert.Equal(Enum.GetValues<ResultadoParticipacionJornada>(), rows.Select(p => p.Resultado));
        Assert.All(rows, row => {
            Assert.Equal(1, row.JornadaId);
            Assert.Equal("Limpieza", row.NombreJornada);
            Assert.Equal(state, row.EstadoJornada);
            Assert.Equal(Day.ToDateTime(TimeOnly.MinValue), row.FechaJornada);
            Assert.Equal($"Persona {row.PersonaId} Apellido", row.NombrePersona);
            Assert.Equal("Registrado", row.Observacion);
        });
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task Jornadas_FiltersCivilDaysWithoutUtcShift()
    {
        using var db = Context();
        db.Personas.Add(new Persona { Id = 1 });
        var midnight = Day.ToDateTime(TimeOnly.MinValue);
        var dates = new[] { midnight.AddTicks(-1), midnight, midnight.AddDays(1).AddTicks(-1), midnight.AddDays(1) };
        for (var i = 0; i < dates.Length; i++)
        {
            db.Jornadas.Add(new Jornada { Id = i + 1, Fecha = dates[i] });
            db.ParticipacionesJornada.Add(new ParticipacionJornada { Id = i + 1, JornadaId = i + 1, PersonaId = 1 });
        }
        db.SaveChanges();
        var service = new DashboardReportesService(db);
        Assert.Equal(new[] { 3, 2 }, (await service.GetJornadasAsync(Day, Day)).Value!.Select(p => p.JornadaId));
        Assert.Equal(3, (await service.GetJornadasAsync(Day, null)).Value!.Count);
        Assert.Equal(3, (await service.GetJornadasAsync(null, Day)).Value!.Count);
    }

    [Fact]
    public void SqlServer_CompleteReportQueriesTranslateFiltersOrderingAndProjections()
    {
        using var db = new PanyebarDbContext(new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;").Options);
        var service = new DashboardReportesService(db);
        var payments = Query<ReportePagoDto>(service, "PagosQuery", Day, Day).ToQueryString();
        Assert.Contains("06:00:00", payments);
        Assert.Contains("ORDER BY", payments);
        Assert.DoesNotContain("AplicacionesPago", payments);
        var obligations = Query<ReporteObligacionPendienteDto>(service, "ObligacionesQuery", Start).ToQueryString();
        Assert.Contains("[Estado] = 1", obligations);
        Assert.Contains("LEFT JOIN", obligations);
        Assert.Contains("FechaVencimiento", obligations);
        Assert.DoesNotContain("CodigoQrToken", obligations);
        var jornadas = Query<ReporteParticipacionJornadaDto>(service, "JornadasQuery", Day, Day).ToQueryString();
        Assert.Contains("00:00:00", jornadas);
        Assert.DoesNotContain("06:00:00", jornadas);
        Assert.Contains("ORDER BY", jornadas);
        Assert.DoesNotContain("Identificacion", jornadas);
    }

    private static IQueryable<T> Query<T>(DashboardReportesService service, string name, params object?[] args) =>
        (IQueryable<T>)typeof(DashboardReportesService).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, args)!;

    [Fact]
    public async Task SectorRevenue_TwoPaymentsInSameSectorAreSummed()
    {
        using var db = SectorContext();
        AddAppliedPayment(db, 1, 1, 12.50m);
        AddAppliedPayment(db, 2, 1, 30m);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var rows = (await new DashboardReportesService(db).GetRecaudacionPorSectorAsync(null, null)).Value!;
        Assert.Equal(new RecaudacionSectorDto(1, "Sector A", 42.50m), Assert.Single(rows));
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Empty(db.Auditorias);
    }

    [Fact]
    public async Task SectorRevenue_DifferentSectorsRemainSeparate()
    {
        using var db = SectorContext();
        AddAppliedPayment(db, 1, 1, 20m);
        AddAppliedPayment(db, 2, 2, 35m);
        db.SaveChanges();
        var rows = (await new DashboardReportesService(db).GetRecaudacionPorSectorAsync(null, null)).Value!;
        Assert.Equal(new[] { new RecaudacionSectorDto(1, "Sector A", 20m),
            new RecaudacionSectorDto(2, "Sector B", 35m) }, rows);
    }

    [Fact]
    public async Task SectorRevenue_MultipleApplicationsDoNotMultiplyPaymentTotal()
    {
        using var db = SectorContext();
        AddAppliedPayment(db, 1, 1, 20m);
        db.Pagos.Local.Single().Monto = 55m;
        db.Obligaciones.Add(new Obligacion { Id = 2, SuministroId = 1, Monto = 35m, Estado = EstadoObligacion.Pagada });
        db.AplicacionesPago.Add(new AplicacionPago { Id = 2, PagoId = 1, ObligacionId = 2 });
        db.SaveChanges();
        var rows = (await new DashboardReportesService(db).GetRecaudacionPorSectorAsync(null, null)).Value!;
        Assert.Equal(55m, Assert.Single(rows).MontoTotal);
    }

    [Fact]
    public async Task SectorRevenue_PersonalObligationHasNoSectorEvenWithRelatedSupply()
    {
        using var db = SectorContext();
        db.Personas.Add(new Persona { Id = 1 });
        db.PersonaSuministros.Add(new PersonaSuministro { Id = 1, PersonaId = 1, SuministroId = 1 });
        AddAppliedPayment(db, 1, null, 30m);
        db.SaveChanges();
        Assert.Empty((await new DashboardReportesService(db).GetRecaudacionPorSectorAsync(null, null)).Value!);
    }

    [Fact]
    public async Task SectorRevenue_AnnulledHistoricalApplicationDoesNotCountAgainAfterRepayment()
    {
        using var db = SectorContext();
        AddAppliedPayment(db, 1, 1, 30m);
        db.Pagos.Local.Single().Estado = EstadoPago.Anulado;
        db.SaveChanges();
        var service = new DashboardReportesService(db);
        Assert.Empty((await service.GetRecaudacionPorSectorAsync(null, null)).Value!);
        db.Pagos.Add(Payment(2, Start, 30m));
        db.AplicacionesPago.Add(new AplicacionPago { Id = 2, PagoId = 2, ObligacionId = 1 });
        db.SaveChanges();
        Assert.Equal(30m, Assert.Single((await service.GetRecaudacionPorSectorAsync(null, null)).Value!).MontoTotal);
        Assert.Equal(2, db.AplicacionesPago.Count());
    }

    [Fact]
    public async Task SectorRevenue_DateRangeUsesPaymentDateAndInclusiveGuatemalaDays()
    {
        using var db = SectorContext();
        var dates = new[] { Start.AddTicks(-1), Start, Start.AddDays(1).AddTicks(-1), Start.AddDays(1) };
        for (var i = 0; i < dates.Length; i++)
            AddAppliedPayment(db, i + 1, 1, (i + 1) * 10m, dates[i]);
        db.SaveChanges();
        var service = new DashboardReportesService(db);
        Assert.Equal(50m, Assert.Single((await service.GetRecaudacionPorSectorAsync(Day, Day)).Value!).MontoTotal);
        Assert.Equal(90m, Assert.Single((await service.GetRecaudacionPorSectorAsync(Day, null)).Value!).MontoTotal);
        Assert.Equal(60m, Assert.Single((await service.GetRecaudacionPorSectorAsync(null, Day)).Value!).MontoTotal);
        Assert.Equal(100m, Assert.Single((await service.GetRecaudacionPorSectorAsync(null, null)).Value!).MontoTotal);
    }

    [Fact]
    public async Task SectorRevenue_EmptyInvalidAndExtremeRangesAreHandled()
    {
        using var db = Context();
        var service = new DashboardReportesService(db);
        Assert.Empty((await service.GetRecaudacionPorSectorAsync(null, null)).Value!);
        Assert.False((await service.GetRecaudacionPorSectorAsync(Day.AddDays(1), Day)).Succeeded);
        Assert.True((await service.GetRecaudacionPorSectorAsync(DateOnly.MinValue, DateOnly.MaxValue)).Succeeded);
    }

    [Fact]
    public void SectorRevenue_SqlServerTranslatesJoinsFiltersAndAggregation()
    {
        using var db = new PanyebarDbContext(new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;").Options);
        var sql = Query<RecaudacionSectorDto>(new DashboardReportesService(db),
            "RecaudacionPorSectorQuery", Day, Day).ToQueryString();
        Assert.Contains("SUM(", sql);
        Assert.Contains("GROUP BY", sql);
        Assert.Contains("JOIN", sql);
        Assert.Contains("[Estado] = 1", sql);
        Assert.Contains("IS NOT NULL", sql);
        Assert.Contains("06:00:00", sql);
        Assert.Contains("[Sectores]", sql);
        Assert.Contains("[AplicacionesPago]", sql);
    }

    private static PanyebarDbContext SectorContext()
    {
        var db = Context();
        db.Sectores.AddRange(new Sector { Id = 1, Nombre = "Sector A" }, new Sector { Id = 2, Nombre = "Sector B" });
        db.Suministros.AddRange(new Suministro { Id = 1, SectorId = 1 }, new Suministro { Id = 2, SectorId = 2 });
        return db;
    }

    private static void AddAppliedPayment(PanyebarDbContext db, int id, int? supply, decimal amount, DateTime? date = null)
    {
        db.Pagos.Add(Payment(id, date ?? Start, amount));
        db.Obligaciones.Add(new Obligacion { Id = id, SuministroId = supply,
            PersonaId = supply.HasValue ? null : 1, Monto = amount, Estado = EstadoObligacion.Pagada,
            Origen = supply.HasValue ? OrigenObligacion.CuotaOrdinaria : OrigenObligacion.Jornada,
            FechaGeneracion = Start.AddMonths(-1) });
        db.AplicacionesPago.Add(new AplicacionPago { Id = id, PagoId = id, ObligacionId = id });
    }

    private static PanyebarDbContext Context() => new(new DbContextOptionsBuilder<PanyebarDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Pago Payment(int id, DateTime date, decimal amount = 10m, EstadoPago state = EstadoPago.Registrado) =>
        new() { Id = id, Fecha = date, Monto = amount, Estado = state, Concepto = $"Pago {id}", UsuarioAdministrativoId = 1 };
}

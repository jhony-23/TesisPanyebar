using Microsoft.EntityFrameworkCore;
using Panyebar.Application.DashboardReportes;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class DashboardReportesService : IDashboardReportesService
{
    // Misma zona operativa y límites [inicio, fin) que FinanzaService.
    private static readonly TimeZoneInfo OperationalZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
    private readonly PanyebarDbContext _dbContext;

    public DashboardReportesService(PanyebarDbContext dbContext) => _dbContext = dbContext;

    public async Task<ConsultaResult<DashboardResumenDto>> GetResumenAsync(
        int anio, int mes, CancellationToken cancellationToken = default)
    {
        // Diciembre de 9999 no admite un límite superior exclusivo representable.
        if (anio < 1 || anio > 9999 || mes < 1 || mes > 12 || anio == 9999 && mes == 12)
            return ConsultaResult<DashboardResumenDto>.Invalid();

        var start = new DateOnly(anio, mes, 1);
        var end = start.AddMonths(1);
        var utcStart = UtcStart(start);
        var utcEnd = UtcStart(end);
        var civilStart = start.ToDateTime(TimeOnly.MinValue);
        var civilEnd = end.ToDateTime(TimeOnly.MinValue);

        var payments = await PagosResumenQuery(utcStart, utcEnd).SingleOrDefaultAsync(cancellationToken);
        var expenses = await EgresosResumenQuery(civilStart, civilEnd).SingleOrDefaultAsync(cancellationToken);
        var obligations = await ObligacionesResumenQuery(utcStart, utcEnd).SingleOrDefaultAsync(cancellationToken);
        var income = payments?.Monto ?? 0m;
        var expense = expenses?.Monto ?? 0m;
        return ConsultaResult<DashboardResumenDto>.Success(new(
            anio, mes, income, expense, income - expense,
            payments?.Cantidad ?? 0, obligations?.Cantidad ?? 0, obligations?.Monto ?? 0m));
    }

    // Agrupar una constante devuelve como máximo una fila y mantiene COUNT/SUM en SQL.
    private IQueryable<TotalesMes> PagosResumenQuery(DateTime start, DateTime end) =>
        _dbContext.Pagos.AsNoTracking()
            .Where(p => p.Estado == EstadoPago.Registrado && p.Fecha >= start && p.Fecha < end)
            .GroupBy(p => 1)
            .Select(g => new TotalesMes(g.Count(), g.Sum(p => p.Monto)));

    private IQueryable<TotalesMes> EgresosResumenQuery(DateTime start, DateTime end) =>
        _dbContext.Egresos.AsNoTracking()
            .Where(e => e.Estado == EstadoEgreso.Registrado && e.Fecha >= start && e.Fecha < end)
            .GroupBy(e => 1)
            .Select(g => new TotalesMes(g.Count(), g.Sum(e => e.Monto)));

    private IQueryable<TotalesMes> ObligacionesResumenQuery(DateTime start, DateTime end) =>
        _dbContext.Obligaciones.AsNoTracking()
            .Where(o => o.Estado == EstadoObligacion.Pendiente &&
                o.FechaGeneracion >= start && o.FechaGeneracion < end)
            .GroupBy(o => 1)
            .Select(g => new TotalesMes(g.Count(), g.Sum(o => o.Monto)));

    private sealed record TotalesMes(int Cantidad, decimal Monto);

    public async Task<ConsultaResult<IReadOnlyList<ReportePagoDto>>> GetPagosAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default)
    {
        if (!ValidRange(fechaDesde, fechaHasta))
            return ConsultaResult<IReadOnlyList<ReportePagoDto>>.Invalid();

        return ConsultaResult<IReadOnlyList<ReportePagoDto>>.Success(
            await PagosQuery(fechaDesde, fechaHasta).ToListAsync(cancellationToken));
    }

    private IQueryable<ReportePagoDto> PagosQuery(DateOnly? from, DateOnly? to)
    {
        var query = _dbContext.Pagos.AsNoTracking();
        if (from.HasValue)
        {
            var start = UtcStart(from.Value);
            query = query.Where(p => p.Fecha >= start);
        }
        if (to.HasValue && to.Value < DateOnly.MaxValue)
        {
            var end = UtcStart(to.Value.AddDays(1));
            query = query.Where(p => p.Fecha < end);
        }
        // Una fila por Pago: las aplicaciones históricas nunca multiplican sus importes.
        return query.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id)
            .Select(p => new ReportePagoDto(p.Id, p.Fecha, p.Concepto, p.Monto, p.Estado,
                p.UsuarioAdministrativoId));
    }

    public async Task<IReadOnlyList<ReporteObligacionPendienteDto>> GetObligacionesPendientesAsync(
        CancellationToken cancellationToken = default) =>
        await ObligacionesQuery(DateTime.UtcNow).ToListAsync(cancellationToken);

    private IQueryable<ReporteObligacionPendienteDto> ObligacionesQuery(DateTime now) =>
        _dbContext.Obligaciones.AsNoTracking()
            .Where(o => o.Estado == EstadoObligacion.Pendiente)
            .OrderByDescending(o => o.FechaGeneracion).ThenByDescending(o => o.Id)
            .Select(o => new ReporteObligacionPendienteDto(
                o.Id, o.PersonaId,
                o.Persona == null ? null : (o.Persona.Nombres + " " + o.Persona.Apellidos).Trim(),
                o.SuministroId, o.Suministro == null ? null : o.Suministro.Nis,
                o.Origen, o.Concepto, o.Monto, o.Periodo, o.FechaGeneracion, o.FechaVencimiento,
                o.FechaVencimiento.HasValue && now > o.FechaVencimiento.Value));

    public async Task<ConsultaResult<IReadOnlyList<ReporteParticipacionJornadaDto>>> GetJornadasAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default)
    {
        if (!ValidRange(fechaDesde, fechaHasta))
            return ConsultaResult<IReadOnlyList<ReporteParticipacionJornadaDto>>.Invalid();

        return ConsultaResult<IReadOnlyList<ReporteParticipacionJornadaDto>>.Success(
            await JornadasQuery(fechaDesde, fechaHasta).ToListAsync(cancellationToken));
    }

    private IQueryable<ReporteParticipacionJornadaDto> JornadasQuery(DateOnly? from, DateOnly? to)
    {
        var query = _dbContext.ParticipacionesJornada.AsNoTracking();
        if (from.HasValue)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.Jornada!.Fecha >= start);
        }
        if (to.HasValue && to.Value < DateOnly.MaxValue)
        {
            var end = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.Jornada!.Fecha < end);
        }
        return query.OrderByDescending(p => p.Jornada!.Fecha).ThenByDescending(p => p.JornadaId)
            .ThenBy(p => p.Persona!.Apellidos).ThenBy(p => p.Persona!.Nombres).ThenBy(p => p.Id)
            .Select(p => new ReporteParticipacionJornadaDto(
                p.Id, p.JornadaId, p.Jornada!.Nombre, p.Jornada.Fecha, p.Jornada.Estado,
                p.PersonaId, (p.Persona!.Nombres + " " + p.Persona.Apellidos).Trim(),
                p.Resultado, p.Observacion));
    }

    private static DateTime UtcStart(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), OperationalZone);

    private static bool ValidRange(DateOnly? from, DateOnly? to) =>
        !from.HasValue || !to.HasValue || from <= to;
}

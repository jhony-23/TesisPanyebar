using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Panyebar.Application.Finanzas;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class FinanzaService : IFinanzaService
{
    private const decimal MaximumAmount = 9999999999999999.99m;
    private static readonly TimeZoneInfo OperationalZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
    private readonly PanyebarDbContext _dbContext;

    public FinanzaService(PanyebarDbContext dbContext) => _dbContext = dbContext;

    public async Task<FinanzaOperationResult<IReadOnlyList<EgresoDto>>> GetEgresosAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, EstadoEgreso? estado, CancellationToken cancellationToken = default)
    {
        if (!ValidRange(fechaDesde, fechaHasta) || estado.HasValue && !Enum.IsDefined(estado.Value))
            return FinanzaOperationResult<IReadOnlyList<EgresoDto>>.Failure(FinanzaOperationError.Invalid);

        var query = Egresos(fechaDesde, fechaHasta);
        if (estado.HasValue) query = query.Where(e => e.Estado == estado.Value);
        var rows = await query.OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id).ToListAsync(cancellationToken);
        return FinanzaOperationResult<IReadOnlyList<EgresoDto>>.Success(rows.Select(ToDto).ToList());
    }

    public async Task<EgresoDto?> GetEgresoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.Egresos.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        return row is null ? null : ToDto(row);
    }

    public async Task<FinanzaOperationResult<EgresoDto>> RegisterAsync(
        EgresoInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default)
    {
        if (!ValidInput(input) || !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
            return FinanzaOperationResult<EgresoDto>.Failure(FinanzaOperationError.Invalid);

        var egreso = new Egreso
        {
            Concepto = input.Concepto!.Trim(), Monto = input.Monto,
            Fecha = input.Fecha.ToDateTime(TimeOnly.MinValue),
            UsuarioAdministrativoId = usuarioAdministrativoId, Estado = EstadoEgreso.Registrado
        };
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        _dbContext.Egresos.Add(egreso);
        await _dbContext.SaveChangesAsync(cancellationToken);
        AddAudit(egreso, usuarioAdministrativoId, "EGRESO.REGISTRAR", null);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return FinanzaOperationResult<EgresoDto>.Success(ToDto(egreso));
    }

    public Task<FinanzaOperationResult<EgresoDto>> UpdateAsync(
        int id, EgresoInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default)
    {
        if (!ValidInput(input))
            return Task.FromResult(FinanzaOperationResult<EgresoDto>.Failure(FinanzaOperationError.Invalid));
        return ChangeAsync(id, input, usuarioAdministrativoId, cancellationToken);
    }

    public Task<FinanzaOperationResult<EgresoDto>> AnnulAsync(
        int id, int usuarioAdministrativoId, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, null, usuarioAdministrativoId, cancellationToken);

    private async Task<FinanzaOperationResult<EgresoDto>> ChangeAsync(
        int id, EgresoInput? input, int userId, CancellationToken cancellationToken)
    {
        if (id <= 0 || !await UserExistsAsync(userId, cancellationToken))
            return FinanzaOperationResult<EgresoDto>.Failure(FinanzaOperationError.Invalid);

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var egreso = await _dbContext.Egresos.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (egreso is null) return FinanzaOperationResult<EgresoDto>.Failure(FinanzaOperationError.NotFound);
        if (egreso.Estado != EstadoEgreso.Registrado)
            return FinanzaOperationResult<EgresoDto>.Failure(FinanzaOperationError.Conflict);

        var previous = Describe(egreso);
        var concept = input?.Concepto!.Trim() ?? egreso.Concepto;
        var amount = input?.Monto ?? egreso.Monto;
        var date = input?.Fecha.ToDateTime(TimeOnly.MinValue) ?? egreso.Fecha;
        var state = input is null ? EstadoEgreso.Anulado : EstadoEgreso.Registrado;
        if (_dbContext.Database.IsRelational())
        {
            // Comparación atómica: una edición concurrente o anulación invalida esta operación.
            // No necesita columnas nuevas y conserva la auditoría junto con el cambio en la transacción.
            var affected = await _dbContext.Egresos
                .Where(e => e.Id == id && e.Estado == EstadoEgreso.Registrado &&
                    e.Concepto == egreso.Concepto && e.Monto == egreso.Monto && e.Fecha == egreso.Fecha)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.Concepto, concept).SetProperty(e => e.Monto, amount)
                    .SetProperty(e => e.Fecha, date).SetProperty(e => e.Estado, state), cancellationToken);
            if (affected != 1) return FinanzaOperationResult<EgresoDto>.Failure(FinanzaOperationError.Conflict);
        }
        else
        {
            // EF InMemory no implementa ExecuteUpdate. El proveedor productivo usa el UPDATE condicional.
            var tracked = await _dbContext.Egresos.SingleAsync(e => e.Id == id, cancellationToken);
            tracked.Concepto = concept; tracked.Monto = amount; tracked.Fecha = date; tracked.Estado = state;
        }
        egreso.Concepto = concept; egreso.Monto = amount; egreso.Fecha = date; egreso.Estado = state;
        AddAudit(egreso, userId, input is null ? "EGRESO.ANULAR" : "EGRESO.EDITAR", previous);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return FinanzaOperationResult<EgresoDto>.Success(ToDto(egreso));
    }

    public async Task<FinanzaOperationResult<IReadOnlyList<IngresoFinancieroDto>>> GetIngresosAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default)
    {
        if (!ValidRange(fechaDesde, fechaHasta))
            return FinanzaOperationResult<IReadOnlyList<IngresoFinancieroDto>>.Failure(FinanzaOperationError.Invalid);
        var rows = await PagosValidos(fechaDesde, fechaHasta)
            .OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id).ToListAsync(cancellationToken);
        return FinanzaOperationResult<IReadOnlyList<IngresoFinancieroDto>>.Success(rows.Select(p =>
            new IngresoFinancieroDto(p.Id, p.Fecha, OperationalDate(p.Fecha), p.Concepto, p.Monto, p.Estado, p.UsuarioAdministrativoId)).ToList());
    }

    public async Task<FinanzaOperationResult<IReadOnlyList<MovimientoFinancieroDto>>> GetMovimientosAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, TipoMovimientoFinanciero? tipo, CancellationToken cancellationToken = default)
    {
        if (!ValidRange(fechaDesde, fechaHasta) || tipo.HasValue && !Enum.IsDefined(tipo.Value))
            return FinanzaOperationResult<IReadOnlyList<MovimientoFinancieroDto>>.Failure(FinanzaOperationError.Invalid);
        var movements = new List<MovimientoFinancieroDto>();
        if (tipo is null or TipoMovimientoFinanciero.Ingreso)
        {
            var payments = await PagosValidos(fechaDesde, fechaHasta).ToListAsync(cancellationToken);
            movements.AddRange(payments.Select(p => new MovimientoFinancieroDto(
                TipoMovimientoFinanciero.Ingreso, p.Id, OperationalDate(p.Fecha), p.Concepto, p.Monto, p.Estado.ToString())));
        }
        if (tipo is null or TipoMovimientoFinanciero.Egreso)
        {
            var expenses = await Egresos(fechaDesde, fechaHasta).Where(e => e.Estado == EstadoEgreso.Registrado).ToListAsync(cancellationToken);
            movements.AddRange(expenses.Select(e => new MovimientoFinancieroDto(
                TipoMovimientoFinanciero.Egreso, e.Id, DateOnly.FromDateTime(e.Fecha), e.Concepto, e.Monto, e.Estado.ToString())));
        }
        // Orden por día civil; dentro del día Tipo e Id resuelven empates sin inventar una hora para Egreso.
        return FinanzaOperationResult<IReadOnlyList<MovimientoFinancieroDto>>.Success(movements
            .OrderByDescending(m => m.Fecha).ThenBy(m => m.Tipo).ThenByDescending(m => m.ReferenciaId).ToList());
    }

    public async Task<FinanzaOperationResult<ResumenFinancieroDto>> GetResumenAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default)
    {
        if (!ValidRange(fechaDesde, fechaHasta))
            return FinanzaOperationResult<ResumenFinancieroDto>.Failure(FinanzaOperationError.Invalid);
        var income = await PagosValidos(fechaDesde, fechaHasta).SumAsync(p => (decimal?)p.Monto, cancellationToken) ?? 0m;
        var expense = await Egresos(fechaDesde, fechaHasta).Where(e => e.Estado == EstadoEgreso.Registrado)
            .SumAsync(e => (decimal?)e.Monto, cancellationToken) ?? 0m;
        return FinanzaOperationResult<ResumenFinancieroDto>.Success(new(income, expense, income - expense));
    }

    private IQueryable<Pago> PagosValidos(DateOnly? from, DateOnly? to)
    {
        var query = _dbContext.Pagos.AsNoTracking().Where(p => p.Estado == EstadoPago.Registrado);
        if (from.HasValue)
        {
            var start = TimeZoneInfo.ConvertTimeToUtc(from.Value.ToDateTime(TimeOnly.MinValue), OperationalZone);
            query = query.Where(p => p.Fecha >= start);
        }
        if (to.HasValue && to.Value < DateOnly.MaxValue)
        {
            var end = TimeZoneInfo.ConvertTimeToUtc(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), OperationalZone);
            query = query.Where(p => p.Fecha < end);
        }
        return query;
    }

    private IQueryable<Egreso> Egresos(DateOnly? from, DateOnly? to)
    {
        var query = _dbContext.Egresos.AsNoTracking();
        if (from.HasValue)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.Fecha >= start);
        }
        if (to.HasValue && to.Value < DateOnly.MaxValue)
        {
            var end = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.Fecha < end);
        }
        return query;
    }

    private static DateOnly OperationalDate(DateTime instant) => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(instant, DateTimeKind.Utc), OperationalZone));
    private static bool ValidRange(DateOnly? from, DateOnly? to) => !from.HasValue || !to.HasValue || from <= to;
    private static bool ValidInput(EgresoInput input) => input is not null &&
        !string.IsNullOrWhiteSpace(input.Concepto) && input.Concepto.Trim().Length <= 200 &&
        input.Monto > 0 && input.Monto <= MaximumAmount && decimal.Round(input.Monto, 2) == input.Monto && input.Fecha != default;
    private Task<bool> UserExistsAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.UsuariosAdministrativos.AsNoTracking().AnyAsync(u => u.Id == id && id > 0, cancellationToken);
    private static EgresoDto ToDto(Egreso e) => new(e.Id, e.Concepto, e.Monto, DateOnly.FromDateTime(e.Fecha), e.UsuarioAdministrativoId, e.Estado);
    private static string Describe(Egreso e) => string.Create(CultureInfo.InvariantCulture,
        $"Concepto:{e.Concepto}; Monto:{e.Monto:0.00}; Fecha:{e.Fecha:yyyy-MM-dd}; Estado:{e.Estado}");
    private void AddAudit(Egreso e, int userId, string action, string? previous) => _dbContext.Auditorias.Add(new Auditoria
    {
        UsuarioAdministrativoId = userId, Accion = action, Entidad = "Egreso", EntidadId = e.Id,
        Fecha = DateTime.UtcNow, ValorAnterior = previous, ValorNuevo = Describe(e)
    });
    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) =>
        _dbContext.Database.IsRelational() ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
}

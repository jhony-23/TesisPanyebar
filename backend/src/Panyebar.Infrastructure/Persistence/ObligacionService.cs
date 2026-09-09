using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Obligaciones;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class ObligacionService : IObligacionService
{
    private readonly PanyebarDbContext _dbContext;

    public ObligacionService(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ObligacionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await ProjectDtos(now)
            .OrderByDescending(o => o.FechaGeneracion)
            .ThenByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<ObligacionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return ProjectDtos(DateTime.UtcNow)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<ObligacionOperationResult<ObligacionDto>> GenerateFromCuotaAsync(
        GenerarObligacionCuotaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (input is null || input.CuotaId <= 0 || input.SuministroId <= 0 ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(ObligacionOperationError.Invalid);
        }

        var cuota = await _dbContext.Cuotas
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == input.CuotaId, cancellationToken);
        var supplyExists = await _dbContext.Suministros
            .AsNoTracking()
            .AnyAsync(s => s.Id == input.SuministroId, cancellationToken);
        if (cuota is null || !supplyExists)
        {
            return Failure(ObligacionOperationError.NotFound);
        }

        var period = NormalizePeriod(input.Periodo, cuota.Periodicidad);
        if (period is null)
        {
            return Failure(ObligacionOperationError.Invalid);
        }

        var duplicate = await _dbContext.Obligaciones
            .AsNoTracking()
            .AnyAsync(
                o => o.CuotaId == cuota.Id &&
                     o.SuministroId == input.SuministroId &&
                     o.Periodo == period &&
                     o.Estado != EstadoObligacion.Anulada,
                cancellationToken);
        if (duplicate)
        {
            return Failure(ObligacionOperationError.Conflict);
        }

        var now = DateTime.UtcNow;
        var obligation = new Obligacion
        {
            PersonaId = null,
            SuministroId = input.SuministroId,
            CuotaId = cuota.Id,
            Origen = OrigenObligacion.CuotaOrdinaria,
            Concepto = cuota.Nombre,
            Monto = cuota.Monto,
            Periodo = period,
            FechaGeneracion = now,
            FechaVencimiento = input.FechaVencimiento,
            Estado = EstadoObligacion.Pendiente
        };

        await using var transaction = await BeginTransactionIfRelationalAsync(cancellationToken);
        _dbContext.Obligaciones.Add(obligation);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _dbContext.Entry(obligation).State = EntityState.Detached;
            return Failure(ObligacionOperationError.Conflict);
        }

        _dbContext.Auditorias.Add(new Auditoria
        {
            UsuarioAdministrativoId = usuarioAdministrativoId,
            Accion = "OBLIGACION.GENERAR.DESDE_CUOTA",
            Entidad = "Obligacion",
            EntidadId = obligation.Id,
            Fecha = now,
            ValorAnterior = null,
            ValorNuevo = $"CuotaId:{cuota.Id}; SuministroId:{input.SuministroId}; Periodo:{period}; " +
                         $"Monto:{obligation.Monto:0.00}; Estado:{obligation.Estado}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return ObligacionOperationResult<ObligacionDto>.Success(ToDto(obligation, DateTime.UtcNow));
    }

    public async Task<ObligacionOperationResult<ObligacionDto>> AnnulAsync(
        int id,
        AnularObligacionInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var reason = input is null || string.IsNullOrWhiteSpace(input.Motivo)
            ? null
            : input.Motivo.Trim();
        if (id <= 0 || reason is null || !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(ObligacionOperationError.Invalid);
        }

        var obligation = await _dbContext.Obligaciones
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (obligation is null)
        {
            return Failure(ObligacionOperationError.NotFound);
        }

        if (obligation.Estado != EstadoObligacion.Pendiente)
        {
            return Failure(ObligacionOperationError.Conflict);
        }

        var now = DateTime.UtcNow;
        obligation.Estado = EstadoObligacion.Anulada;
        _dbContext.Auditorias.Add(new Auditoria
        {
            UsuarioAdministrativoId = usuarioAdministrativoId,
            Accion = "OBLIGACION.ANULAR",
            Entidad = "Obligacion",
            EntidadId = obligation.Id,
            Fecha = now,
            ValorAnterior = EstadoObligacion.Pendiente.ToString(),
            ValorNuevo = $"{EstadoObligacion.Anulada}; Motivo:{reason}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ObligacionOperationResult<ObligacionDto>.Success(ToDto(obligation, now));
    }

    private IQueryable<ObligacionDto> ProjectDtos(DateTime now)
    {
        return _dbContext.Obligaciones
            .AsNoTracking()
            .Select(o => new ObligacionDto(
                o.Id,
                o.PersonaId,
                o.SuministroId,
                o.CuotaId,
                o.Origen,
                o.Concepto,
                o.Monto,
                o.Periodo,
                o.FechaGeneracion,
                o.FechaVencimiento,
                o.Estado,
                o.Estado == EstadoObligacion.Pendiente &&
                o.FechaVencimiento.HasValue &&
                now > o.FechaVencimiento.Value));
    }

    private Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return Task.FromResult(false);
        }

        return _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }

    private static string? NormalizePeriod(string? value, PeriodicidadCuota periodicity)
    {
        var period = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (period is null)
        {
            return null;
        }

        var format = periodicity switch
        {
            PeriodicidadCuota.Anual => "yyyy",
            PeriodicidadCuota.Mensual => "yyyy-MM",
            _ => null
        };
        if (format is null || period.Length != format.Length)
        {
            return null;
        }

        var parseValue = periodicity == PeriodicidadCuota.Anual ? $"{period}-01-01" : $"{period}-01";
        return DateTime.TryParseExact(
            parseValue,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _) ? period : null;
    }

    private static ObligacionDto ToDto(Obligacion obligation, DateTime now) => new(
        obligation.Id,
        obligation.PersonaId,
        obligation.SuministroId,
        obligation.CuotaId,
        obligation.Origen,
        obligation.Concepto,
        obligation.Monto,
        obligation.Periodo,
        obligation.FechaGeneracion,
        obligation.FechaVencimiento,
        obligation.Estado,
        obligation.Estado == EstadoObligacion.Pendiente &&
        obligation.FechaVencimiento.HasValue &&
        now > obligation.FechaVencimiento.Value);

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfRelationalAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                sqlException.Errors.Cast<SqlError>().Any(error => error.Number is 2601 or 2627))
            {
                return true;
            }
        }

        return false;
    }

    private static ObligacionOperationResult<ObligacionDto> Failure(ObligacionOperationError error) =>
        ObligacionOperationResult<ObligacionDto>.Failure(error);
}

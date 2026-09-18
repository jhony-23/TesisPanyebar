using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Abastecimiento;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class AbastecimientoService : IAbastecimientoService
{
    private const int MaxObservationLength = 1000;

    private readonly PanyebarDbContext _dbContext;

    public AbastecimientoService(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AbastecimientoOperationResult<IReadOnlyList<ProgramacionAbastecimientoDto>>> GetAllAsync(
        DateOnly? fechaDesde,
        DateOnly? fechaHasta,
        int? sectorId,
        CancellationToken cancellationToken = default)
    {
        if (fechaDesde.HasValue &&
            fechaHasta.HasValue &&
            fechaDesde.Value > fechaHasta.Value)
        {
            return AbastecimientoOperationResult<IReadOnlyList<ProgramacionAbastecimientoDto>>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        if (sectorId.HasValue && sectorId.Value <= 0)
        {
            return AbastecimientoOperationResult<IReadOnlyList<ProgramacionAbastecimientoDto>>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        var query = _dbContext.ProgramacionesAbastecimiento
            .AsNoTracking()
            .AsQueryable();

        if (fechaDesde.HasValue)
        {
            var from = fechaDesde.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.Fecha >= from);
        }

        if (fechaHasta.HasValue)
        {
            var untilExclusive = fechaHasta.Value
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            query = query.Where(p => p.Fecha < untilExclusive);
        }

        if (sectorId.HasValue)
        {
            query = query.Where(p => p.SectorId == sectorId.Value);
        }

        var values = await query
            .OrderBy(p => p.Fecha)
            .ThenBy(p => p.HoraInicio)
            .ThenBy(p => p.Sector!.Nombre)
            .ThenBy(p => p.Id)
            .Select(p => new ProgramacionAbastecimientoDto(
                p.Id,
                p.SectorId,
                p.Sector!.Nombre,
                p.Fecha,
                p.HoraInicio,
                p.HoraFin,
                p.Estado,
                p.Observacion))
            .ToListAsync(cancellationToken);

        return AbastecimientoOperationResult<IReadOnlyList<ProgramacionAbastecimientoDto>>
            .Success(values);
    }

    public async Task<ProgramacionAbastecimientoDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        return await _dbContext.ProgramacionesAbastecimiento
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProgramacionAbastecimientoDto(
                p.Id,
                p.SectorId,
                p.Sector!.Nombre,
                p.Fecha,
                p.HoraInicio,
                p.HoraFin,
                p.Estado,
                p.Observacion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> CreateAsync(
        ProgramacionAbastecimientoInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);

        if (normalized is null || usuarioAdministrativoId <= 0)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        var sector = await _dbContext.Sectores
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == normalized.Value.SectorId,
                cancellationToken);

        if (sector is null)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.SectorNotFound);
        }

        if (sector.Estado != EstadoRegistro.Activo)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.SectorInactive);
        }

        if (await HasOverlapAsync(
            normalized.Value.SectorId,
            normalized.Value.Fecha,
            normalized.Value.HoraInicio,
            normalized.Value.HoraFin,
            null,
            cancellationToken))
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Conflict);
        }

        var programacion = new ProgramacionAbastecimiento
        {
            SectorId = normalized.Value.SectorId,
            Fecha = normalized.Value.Fecha,
            HoraInicio = normalized.Value.HoraInicio,
            HoraFin = normalized.Value.HoraFin,
            Estado = EstadosProgramacionAbastecimiento.Programado,
            Observacion = normalized.Value.Observacion
        };

        _dbContext.ProgramacionesAbastecimiento.Add(programacion);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "ABASTECIMIENTO.CREAR",
            programacion.Id,
            null,
            Describe(programacion)));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
            .Success(ToDto(programacion, sector.Nombre));
    }

    public async Task<AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>> CreateRecurringAsync(
        ProgramacionRecurrenteAbastecimientoInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (input is null ||
            usuarioAdministrativoId <= 0 ||
            input.SectorId <= 0 ||
            !Enum.IsDefined(typeof(TipoRecurrenciaAbastecimiento), input.Recurrencia) ||
            input.HoraInicio < TimeSpan.Zero ||
            input.HoraInicio >= TimeSpan.FromDays(1) ||
            input.HoraFin <= TimeSpan.Zero ||
            input.HoraFin > TimeSpan.FromDays(1) ||
            input.HoraInicio >= input.HoraFin)
        {
            return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        var observation = NormalizeObservation(input.Observacion);

        if (input.Observacion is not null &&
            observation is null &&
            !string.IsNullOrWhiteSpace(input.Observacion))
        {
            return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        var hasCount = input.CantidadOcurrencias.HasValue;
        var hasEndDate = input.FechaFin.HasValue;

        if (hasCount == hasEndDate)
        {
            return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        if (hasCount &&
            (input.CantidadOcurrencias!.Value < 2 ||
             input.CantidadOcurrencias.Value > 52))
        {
            return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        if (hasEndDate && input.FechaFin!.Value <= input.FechaInicial)
        {
            return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        var sector = await _dbContext.Sectores
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == input.SectorId,
                cancellationToken);

        if (sector is null)
        {
            return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                .Failure(AbastecimientoOperationError.SectorNotFound);
        }

        if (sector.Estado != EstadoRegistro.Activo)
        {
            return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                .Failure(AbastecimientoOperationError.SectorInactive);
        }

        var dates = GenerateRecurringDates(input);

        if (dates.Count < 2 || dates.Count > 52)
        {
            return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        foreach (var date in dates)
        {
            if (await HasOverlapAsync(
                input.SectorId,
                date.ToDateTime(TimeOnly.MinValue),
                input.HoraInicio,
                input.HoraFin,
                null,
                cancellationToken))
            {
                return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
                    .Failure(AbastecimientoOperationError.Conflict);
            }
        }

        await using var transaction =
            _dbContext.Database.IsRelational()
                ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
                : null;

        var entities = dates
            .Select(date => new ProgramacionAbastecimiento
            {
                SectorId = input.SectorId,
                Fecha = date.ToDateTime(TimeOnly.MinValue),
                HoraInicio = input.HoraInicio,
                HoraFin = input.HoraFin,
                Estado = EstadosProgramacionAbastecimiento.Programado,
                Observacion = observation
            })
            .ToList();

        _dbContext.ProgramacionesAbastecimiento.AddRange(entities);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var entity in entities)
        {
            _dbContext.Auditorias.Add(CreateAudit(
                usuarioAdministrativoId,
                "ABASTECIMIENTO.CREAR.RECURRENTE",
                entity.Id,
                null,
                Describe(entity)));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        var result = entities
            .Select(entity => ToDto(entity, sector.Nombre))
            .ToList();

        return AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>
            .Success(new CreacionRecurrenteAbastecimientoDto(
                result.Count,
                result));
    }
    public async Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> UpdateAsync(
        int id,
        ProgramacionAbastecimientoInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);

        if (id <= 0 ||
            normalized is null ||
            usuarioAdministrativoId <= 0)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        var programacion = await _dbContext.ProgramacionesAbastecimiento
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (programacion is null)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.NotFound);
        }

        if (programacion.Estado != EstadosProgramacionAbastecimiento.Programado)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Conflict);
        }

        var sector = await _dbContext.Sectores
            .AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == normalized.Value.SectorId,
                cancellationToken);

        if (sector is null)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.SectorNotFound);
        }

        if (sector.Estado != EstadoRegistro.Activo)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.SectorInactive);
        }

        if (await HasOverlapAsync(
            normalized.Value.SectorId,
            normalized.Value.Fecha,
            normalized.Value.HoraInicio,
            normalized.Value.HoraFin,
            id,
            cancellationToken))
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Conflict);
        }

        var previous = Describe(programacion);

        programacion.SectorId = normalized.Value.SectorId;
        programacion.Fecha = normalized.Value.Fecha;
        programacion.HoraInicio = normalized.Value.HoraInicio;
        programacion.HoraFin = normalized.Value.HoraFin;
        programacion.Observacion = normalized.Value.Observacion;

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "ABASTECIMIENTO.ACTUALIZAR",
            programacion.Id,
            previous,
            Describe(programacion)));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
            .Success(ToDto(programacion, sector.Nombre));
    }

    public Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> CompleteAsync(
        int id,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            EstadosProgramacionAbastecimiento.Completado,
            null,
            usuarioAdministrativoId,
            "ABASTECIMIENTO.COMPLETAR",
            cancellationToken);

    public Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> CancelAsync(
        int id,
        ActualizarEstadoAbastecimientoInput? input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            EstadosProgramacionAbastecimiento.Cancelado,
            input?.Observacion,
            usuarioAdministrativoId,
            "ABASTECIMIENTO.CANCELAR",
            cancellationToken);

    private async Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> TransitionAsync(
        int id,
        string newState,
        string? observation,
        int usuarioAdministrativoId,
        string action,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || usuarioAdministrativoId <= 0)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        var normalizedObservation = NormalizeObservation(observation);

        if (observation is not null &&
            normalizedObservation is null &&
            !string.IsNullOrWhiteSpace(observation))
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Invalid);
        }

        var programacion = await _dbContext.ProgramacionesAbastecimiento
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (programacion is null)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.NotFound);
        }

        if (programacion.Estado != EstadosProgramacionAbastecimiento.Programado)
        {
            return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
                .Failure(AbastecimientoOperationError.Conflict);
        }

        var sectorName = await _dbContext.Sectores
            .AsNoTracking()
            .Where(s => s.Id == programacion.SectorId)
            .Select(s => s.Nombre)
            .SingleAsync(cancellationToken);

        var previous = Describe(programacion);

        programacion.Estado = newState;

        if (newState == EstadosProgramacionAbastecimiento.Cancelado &&
            normalizedObservation is not null)
        {
            programacion.Observacion = normalizedObservation;
        }

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            action,
            programacion.Id,
            previous,
            Describe(programacion)));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AbastecimientoOperationResult<ProgramacionAbastecimientoDto>
            .Success(ToDto(programacion, sectorName));
    }

    private async Task<bool> HasOverlapAsync(
        int sectorId,
        DateTime fecha,
        TimeSpan horaInicio,
        TimeSpan horaFin,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ProgramacionesAbastecimiento
            .AsNoTracking()
            .Where(p =>
                p.SectorId == sectorId &&
                p.Fecha == fecha &&
                p.Estado == EstadosProgramacionAbastecimiento.Programado);

        if (excludedId.HasValue)
        {
            var id = excludedId.Value;
            query = query.Where(p => p.Id != id);
        }

        return await query.AnyAsync(
            p => horaInicio < p.HoraFin &&
                 horaFin > p.HoraInicio,
            cancellationToken);
    }

    private static IReadOnlyList<DateOnly> GenerateRecurringDates(
        ProgramacionRecurrenteAbastecimientoInput input)
    {
        var dates = new List<DateOnly>();
        var current = input.FechaInicial;
        var targetCount = input.CantidadOcurrencias;
        var endDate = input.FechaFin;

        while (dates.Count < 52)
        {
            if (endDate.HasValue && current > endDate.Value)
            {
                break;
            }

            dates.Add(current);

            if (targetCount.HasValue &&
                dates.Count >= targetCount.Value)
            {
                break;
            }

            var next = NextRecurringDate(
                input.FechaInicial,
                current,
                input.Recurrencia);

            if (!next.HasValue)
            {
                break;
            }

            current = next.Value;
        }

        return dates;
    }

    private static DateOnly? NextRecurringDate(
        DateOnly original,
        DateOnly current,
        TipoRecurrenciaAbastecimiento recurrence)
    {
        if (recurrence == TipoRecurrenciaAbastecimiento.Semanal)
        {
            return current.AddDays(7);
        }

        if (recurrence == TipoRecurrenciaAbastecimiento.Mensual)
        {
            var year = current.Year;
            var month = current.Month;

            for (var attempt = 0; attempt < 24; attempt++)
            {
                month++;

                if (month == 13)
                {
                    month = 1;
                    year++;
                }

                if (year > 9999)
                {
                    return null;
                }

                if (original.Day <= DateTime.DaysInMonth(year, month))
                {
                    return new DateOnly(
                        year,
                        month,
                        original.Day);
                }
            }

            return null;
        }

        var nextYear = current.Year + 1;

        while (nextYear <= 9999)
        {
            if (original.Month != 2 ||
                original.Day != 29 ||
                DateTime.IsLeapYear(nextYear))
            {
                return new DateOnly(
                    nextYear,
                    original.Month,
                    original.Day);
            }

            nextYear++;
        }

        return null;
    }
    private static (
        int SectorId,
        DateTime Fecha,
        TimeSpan HoraInicio,
        TimeSpan HoraFin,
        string? Observacion)? Normalize(
            ProgramacionAbastecimientoInput input)
    {
        if (input is null ||
            input.SectorId <= 0 ||
            input.HoraInicio < TimeSpan.Zero ||
            input.HoraInicio >= TimeSpan.FromDays(1) ||
            input.HoraFin <= TimeSpan.Zero ||
            input.HoraFin > TimeSpan.FromDays(1) ||
            input.HoraInicio >= input.HoraFin)
        {
            return null;
        }

        var observation = NormalizeObservation(input.Observacion);

        if (input.Observacion is not null &&
            observation is null &&
            !string.IsNullOrWhiteSpace(input.Observacion))
        {
            return null;
        }

        return (
            input.SectorId,
            input.Fecha.ToDateTime(TimeOnly.MinValue),
            input.HoraInicio,
            input.HoraFin,
            observation);
    }

    private static string? NormalizeObservation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        return normalized.Length <= MaxObservationLength
            ? normalized
            : null;
    }

    private static ProgramacionAbastecimientoDto ToDto(
        ProgramacionAbastecimiento programacion,
        string sectorName) =>
        new(
            programacion.Id,
            programacion.SectorId,
            sectorName,
            programacion.Fecha,
            programacion.HoraInicio,
            programacion.HoraFin,
            programacion.Estado,
            programacion.Observacion);

    private static string Describe(
        ProgramacionAbastecimiento programacion) =>
        $"SectorId:{programacion.SectorId}; " +
        $"Fecha:{programacion.Fecha:yyyy-MM-dd}; " +
        $"Horario:{programacion.HoraInicio:hh\\:mm}-{programacion.HoraFin:hh\\:mm}; " +
        $"Estado:{programacion.Estado}; " +
        $"Observacion:{programacion.Observacion ?? "-"}";

    private static Auditoria CreateAudit(
        int userId,
        string action,
        int entityId,
        string? previousValue,
        string newValue) =>
        new()
        {
            UsuarioAdministrativoId = userId,
            Accion = action,
            Entidad = "ProgramacionAbastecimiento",
            EntidadId = entityId,
            Fecha = DateTime.UtcNow,
            ValorAnterior = previousValue,
            ValorNuevo = newValue
        };
}

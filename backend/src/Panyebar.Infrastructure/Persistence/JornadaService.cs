using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Jornadas;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class JornadaService : IJornadaService
{
    private const decimal MaximumAmount = 9999999999999999.99m;
    private readonly PanyebarDbContext _dbContext;

    public JornadaService(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<JornadaResumenDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jornadas
            .AsNoTracking()
            .OrderByDescending(j => j.Fecha)
            .ThenByDescending(j => j.Id)
            .Select(j => new JornadaResumenDto(
                j.Id,
                j.Nombre,
                j.Descripcion,
                j.Fecha,
                j.HoraInicio,
                j.HoraFin,
                j.Ubicacion,
                j.MontoIncumplimiento,
                j.Estado,
                _dbContext.ParticipacionesJornada.Count(p => p.JornadaId == j.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<JornadaDetalleDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        var jornada = await _dbContext.Jornadas
            .AsNoTracking()
            .Where(j => j.Id == id)
            .Select(j => new
            {
                j.Id,
                j.Nombre,
                j.Descripcion,
                j.Fecha,
                j.HoraInicio,
                j.HoraFin,
                j.Ubicacion,
                j.MontoIncumplimiento,
                j.Estado
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (jornada is null)
        {
            return null;
        }

        var participantes = await _dbContext.ParticipacionesJornada
            .AsNoTracking()
            .Where(p => p.JornadaId == id)
            .Join(
                _dbContext.Personas.AsNoTracking(),
                p => p.PersonaId,
                persona => persona.Id,
                (p, persona) => new
                {
                    Participacion = p,
                    Persona = persona
                })
            .OrderBy(x => x.Persona.Apellidos)
            .ThenBy(x => x.Persona.Nombres)
            .ThenBy(x => x.Participacion.Id)
            .Select(x => new ParticipacionJornadaDto(
                x.Participacion.Id,
                x.Participacion.PersonaId,
                x.Persona.Nombres + " " + x.Persona.Apellidos,
                x.Participacion.Resultado,
                x.Participacion.Observacion,
                _dbContext.ObligacionesJornada
                    .Where(oj =>
                        oj.ParticipacionJornadaId == x.Participacion.Id)
                    .Select(oj => (int?)oj.ObligacionId)
                    .SingleOrDefault(),
                _dbContext.ObligacionesJornada
                    .Where(oj =>
                        oj.ParticipacionJornadaId == x.Participacion.Id)
                    .Select(oj => (EstadoObligacion?)oj.Obligacion!.Estado)
                    .SingleOrDefault()))
            .ToListAsync(cancellationToken);

        return new JornadaDetalleDto(
            jornada.Id,
            jornada.Nombre,
            jornada.Descripcion,
            jornada.Fecha,
            jornada.HoraInicio,
            jornada.HoraFin,
            jornada.Ubicacion,
            jornada.MontoIncumplimiento,
            jornada.Estado,
            participantes);
    }

    public async Task<JornadaOperationResult<JornadaDetalleDto>> CreateAsync(
        JornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);

        if (normalized is null ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var now = DateTime.UtcNow;

        var jornada = new Jornada
        {
            Nombre = normalized.Value.Nombre,
            Descripcion = normalized.Value.Descripcion,
            Fecha = normalized.Value.Fecha,
            HoraInicio = normalized.Value.HoraInicio,
            HoraFin = normalized.Value.HoraFin,
            Ubicacion = normalized.Value.Ubicacion,
            MontoIncumplimiento = normalized.Value.MontoIncumplimiento,
            Estado = EstadoJornada.Planificada
        };

        await using var transaction =
            await BeginTransactionIfRelationalAsync(cancellationToken);

        _dbContext.Jornadas.Add(jornada);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "JORNADA.CREAR",
            "Jornada",
            jornada.Id,
            null,
            Describe(jornada),
            now));

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return JornadaOperationResult<JornadaDetalleDto>.Success(
            (await GetByIdAsync(jornada.Id, cancellationToken))!);
    }

    public async Task<JornadaOperationResult<JornadaDetalleDto>> UpdateAsync(
        int id,
        JornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);

        if (id <= 0 ||
            normalized is null ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var jornada = await _dbContext.Jornadas
            .SingleOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (jornada is null)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        if (jornada.Estado != EstadoJornada.Planificada)
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var previous = Describe(jornada);

        jornada.Nombre = normalized.Value.Nombre;
        jornada.Descripcion = normalized.Value.Descripcion;
        jornada.Fecha = normalized.Value.Fecha;
        jornada.HoraInicio = normalized.Value.HoraInicio;
        jornada.HoraFin = normalized.Value.HoraFin;
        jornada.Ubicacion = normalized.Value.Ubicacion;
        jornada.MontoIncumplimiento = normalized.Value.MontoIncumplimiento;

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "JORNADA.ACTUALIZAR",
            "Jornada",
            jornada.Id,
            previous,
            Describe(jornada),
            DateTime.UtcNow));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return JornadaOperationResult<JornadaDetalleDto>.Success(
            (await GetByIdAsync(jornada.Id, cancellationToken))!);
    }

    public async Task<JornadaOperationResult<JornadaDetalleDto>> CancelAsync(
        int id,
        CancelarJornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var reason = input is null || string.IsNullOrWhiteSpace(input.Motivo)
            ? null
            : input.Motivo.Trim();

        if (id <= 0 ||
            reason is null ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var jornada = await _dbContext.Jornadas
            .SingleOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (jornada is null)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        if (jornada.Estado != EstadoJornada.Planificada)
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var previous = jornada.Estado;
        jornada.Estado = EstadoJornada.Cancelada;

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "JORNADA.CANCELAR",
            "Jornada",
            jornada.Id,
            previous.ToString(),
            $"{jornada.Estado}; Motivo:{reason}",
            DateTime.UtcNow));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return JornadaOperationResult<JornadaDetalleDto>.Success(
            (await GetByIdAsync(jornada.Id, cancellationToken))!);
    }

    public async Task<JornadaOperationResult<JornadaDetalleDto>> AddParticipantsAsync(
        int id,
        AgregarParticipantesJornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0 ||
            input?.PersonaIds is null ||
            input.PersonaIds.Count == 0 ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var personIds = input.PersonaIds.ToArray();

        if (personIds.Any(personId => personId <= 0) ||
            personIds.Distinct().Count() != personIds.Length)
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var jornada = await _dbContext.Jornadas
            .SingleOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (jornada is null)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        if (jornada.Estado != EstadoJornada.Planificada)
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var activePersonIds = await _dbContext.Personas
            .AsNoTracking()
            .Where(p => personIds.Contains(p.Id) && p.Estado == EstadoRegistro.Activo)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (activePersonIds.Count != personIds.Length)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        var existing = await _dbContext.ParticipacionesJornada
            .AsNoTracking()
            .Where(p => p.JornadaId == id && personIds.Contains(p.PersonaId))
            .AnyAsync(cancellationToken);

        if (existing)
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var participations = personIds
            .Select(personId => new ParticipacionJornada
            {
                JornadaId = id,
                PersonaId = personId,
                Resultado = ResultadoParticipacionJornada.Pendiente
            })
            .ToArray();

        await using var transaction =
            await BeginTransactionIfRelationalAsync(cancellationToken);

        _dbContext.ParticipacionesJornada.AddRange(participations);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "PARTICIPACION.REGISTRAR",
            "Jornada",
            jornada.Id,
            null,
            $"PersonaIds:{string.Join(",", personIds.OrderBy(value => value))}",
            DateTime.UtcNow));

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return JornadaOperationResult<JornadaDetalleDto>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<JornadaOperationResult<JornadaDetalleDto>> RemoveParticipantAsync(
        int id,
        int personaId,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0 ||
            personaId <= 0 ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var jornada = await _dbContext.Jornadas
            .SingleOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (jornada is null)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        if (jornada.Estado != EstadoJornada.Planificada)
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var participation = await _dbContext.ParticipacionesJornada
            .SingleOrDefaultAsync(
                p => p.JornadaId == id && p.PersonaId == personaId,
                cancellationToken);

        if (participation is null)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        var hasObligation = await _dbContext.ObligacionesJornada
            .AsNoTracking()
            .AnyAsync(
                oj => oj.ParticipacionJornadaId == participation.Id,
                cancellationToken);

        if (hasObligation)
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var participationId = participation.Id;

        _dbContext.ParticipacionesJornada.Remove(participation);

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "PARTICIPACION.RETIRAR",
            "ParticipacionJornada",
            participationId,
            $"JornadaId:{id}; PersonaId:{personaId}",
            "Retirada",
            DateTime.UtcNow));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return JornadaOperationResult<JornadaDetalleDto>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<JornadaOperationResult<JornadaDetalleDto>> UpdateParticipantAsync(
        int id,
        int personaId,
        ActualizarParticipacionJornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0 ||
            personaId <= 0 ||
            input is null ||
            !Enum.IsDefined(typeof(ResultadoParticipacionJornada), input.Resultado) ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var observation = string.IsNullOrWhiteSpace(input.Observacion)
            ? null
            : input.Observacion.Trim();

        if (observation?.Length > 500)
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var jornada = await _dbContext.Jornadas
            .SingleOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (jornada is null)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        if (jornada.Estado != EstadoJornada.Planificada)
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var participation = await _dbContext.ParticipacionesJornada
            .SingleOrDefaultAsync(
                p => p.JornadaId == id && p.PersonaId == personaId,
                cancellationToken);

        if (participation is null)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        var previous =
            $"Resultado:{participation.Resultado}; Observacion:{participation.Observacion}";

        participation.Resultado = input.Resultado;
        participation.Observacion = observation;

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "PARTICIPACION.ACTUALIZAR",
            "ParticipacionJornada",
            participation.Id,
            previous,
            $"Resultado:{participation.Resultado}; Observacion:{participation.Observacion}",
            DateTime.UtcNow));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return JornadaOperationResult<JornadaDetalleDto>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    public async Task<JornadaOperationResult<JornadaDetalleDto>> CloseAsync(
        int id,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0 ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(JornadaOperationError.Invalid);
        }

        var jornada = await _dbContext.Jornadas
            .SingleOrDefaultAsync(j => j.Id == id, cancellationToken);

        if (jornada is null)
        {
            return Failure(JornadaOperationError.NotFound);
        }

        if (jornada.Estado != EstadoJornada.Planificada)
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var participations = await _dbContext.ParticipacionesJornada
            .Where(p => p.JornadaId == id)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        if (participations.Count == 0 ||
            participations.Any(
                p => p.Resultado == ResultadoParticipacionJornada.Pendiente))
        {
            return Failure(JornadaOperationError.Conflict);
        }

        var penalized = jornada.MontoIncumplimiento.HasValue
            ? participations
                .Where(p => p.Resultado == ResultadoParticipacionJornada.Ausencia)
                .ToArray()
            : Array.Empty<ParticipacionJornada>();

        if (penalized.Length > 0)
        {
            var participationIds = penalized.Select(p => p.Id).ToArray();

            var alreadyLinked = await _dbContext.ObligacionesJornada
                .AsNoTracking()
                .AnyAsync(
                    oj => participationIds.Contains(oj.ParticipacionJornadaId),
                    cancellationToken);

            if (alreadyLinked)
            {
                return Failure(JornadaOperationError.Conflict);
            }
        }

        var now = DateTime.UtcNow;

        await using var transaction =
            await BeginTransactionIfRelationalAsync(cancellationToken);

        foreach (var participation in penalized)
        {
            var obligation = new Obligacion
            {
                PersonaId = participation.PersonaId,
                SuministroId = null,
                CuotaId = null,
                Origen = OrigenObligacion.Jornada,
                Concepto = $"Ausencia a jornada: {jornada.Nombre}",
                Monto = jornada.MontoIncumplimiento!.Value,
                Periodo = null,
                FechaGeneracion = now,
                FechaVencimiento = null,
                Estado = EstadoObligacion.Pendiente
            };

            _dbContext.Obligaciones.Add(obligation);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.ObligacionesJornada.Add(new ObligacionJornada
            {
                ParticipacionJornadaId = participation.Id,
                ObligacionId = obligation.Id
            });

            _dbContext.Auditorias.Add(CreateAudit(
                usuarioAdministrativoId,
                "OBLIGACION.GENERAR.DESDE_JORNADA",
                "Obligacion",
                obligation.Id,
                null,
                $"JornadaId:{jornada.Id}; ParticipacionJornadaId:{participation.Id}; " +
                $"PersonaId:{participation.PersonaId}; Monto:{obligation.Monto:0.00}; " +
                $"Estado:{obligation.Estado}",
                now));
        }

        var previousState = jornada.Estado;
        jornada.Estado = EstadoJornada.Cerrada;

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "JORNADA.CERRAR",
            "Jornada",
            jornada.Id,
            previousState.ToString(),
            $"{jornada.Estado}; ObligacionesGeneradas:{penalized.Length}",
            now));

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return JornadaOperationResult<JornadaDetalleDto>.Success(
            (await GetByIdAsync(id, cancellationToken))!);
    }

    private static (
        string Nombre,
        string? Descripcion,
        DateTime Fecha,
        TimeOnly? HoraInicio,
        TimeOnly? HoraFin,
        string? Ubicacion,
        decimal? MontoIncumplimiento)? Normalize(JornadaInput input)
    {
        if (input is null ||
            string.IsNullOrWhiteSpace(input.Nombre) ||
            input.Fecha == default)
        {
            return null;
        }

        var name = input.Nombre.Trim();
        var description = string.IsNullOrWhiteSpace(input.Descripcion)
            ? null
            : input.Descripcion.Trim();
        var location = string.IsNullOrWhiteSpace(input.Ubicacion)
            ? null
            : input.Ubicacion.Trim();

        if (name.Length > 150 ||
            description?.Length > 500 ||
            location?.Length > 200)
        {
            return null;
        }

        if (input.HoraInicio.HasValue &&
            input.HoraFin.HasValue &&
            input.HoraFin.Value <= input.HoraInicio.Value)
        {
            return null;
        }

        if (input.MontoIncumplimiento.HasValue &&
            (input.MontoIncumplimiento.Value <= 0 ||
             input.MontoIncumplimiento.Value > MaximumAmount ||
             decimal.Round(input.MontoIncumplimiento.Value, 2) !=
             input.MontoIncumplimiento.Value))
        {
            return null;
        }

        return (
            name,
            description,
            input.Fecha,
            input.HoraInicio,
            input.HoraFin,
            location,
            input.MontoIncumplimiento);
    }

    private Task<bool> UserExistsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return Task.FromResult(false);
        }

        return _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }

    private static string Describe(Jornada jornada) =>
        $"Nombre:{jornada.Nombre}; Fecha:{jornada.Fecha:O}; " +
        $"HoraInicio:{jornada.HoraInicio}; HoraFin:{jornada.HoraFin}; " +
        $"Ubicacion:{jornada.Ubicacion}; MontoIncumplimiento:{jornada.MontoIncumplimiento}; " +
        $"Estado:{jornada.Estado}";

    private static Auditoria CreateAudit(
        int userId,
        string action,
        string entity,
        int entityId,
        string? previousValue,
        string newValue,
        DateTime date) => new()
        {
            UsuarioAdministrativoId = userId,
            Accion = action,
            Entidad = entity,
            EntidadId = entityId,
            Fecha = date,
            ValorAnterior = previousValue,
            ValorNuevo = newValue
        };

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?>
        BeginTransactionIfRelationalAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }

    private static JornadaOperationResult<JornadaDetalleDto> Failure(
        JornadaOperationError error) =>
        JornadaOperationResult<JornadaDetalleDto>.Failure(error);
}

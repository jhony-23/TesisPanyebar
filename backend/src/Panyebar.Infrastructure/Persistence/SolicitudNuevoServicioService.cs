using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.SolicitudesNuevoServicio;
using Panyebar.Application.Suministros;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class SolicitudNuevoServicioService : ISolicitudNuevoServicioService
{
    private readonly PanyebarDbContext _dbContext;
    private readonly ISuministroNisGenerator _nisGenerator;
    private readonly ISuministroQrTokenGenerator _qrTokenGenerator;

    public SolicitudNuevoServicioService(
        PanyebarDbContext dbContext,
        ISuministroNisGenerator nisGenerator,
        ISuministroQrTokenGenerator qrTokenGenerator)
    {
        _dbContext = dbContext;
        _nisGenerator = nisGenerator;
        _qrTokenGenerator = qrTokenGenerator;
    }

    public async Task<IReadOnlyList<SolicitudNuevoServicioDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery()
            .OrderBy(s => s.Estado == EstadoSolicitudNuevoServicio.Pendiente ? 0 : 1)
            .ThenByDescending(s => s.FechaSolicitud)
            .ThenByDescending(s => s.Id)
            .Select(ProjectDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<SolicitudNuevoServicioDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery()
            .Where(s => s.Id == id)
            .Select(ProjectDto())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>> CreateAsync(
        SolicitudNuevoServicioInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCreationInput(input);
        if (normalized is null || usuarioAdministrativoId <= 0)
        {
            return Failure(SolicitudNuevoServicioOperationError.Invalid);
        }

        var validReferences = await ValidateReferencesAsync(
            normalized.Value.PersonaSolicitanteId,
            normalized.Value.SectorId,
            cancellationToken);
        if (!validReferences)
        {
            return Failure(SolicitudNuevoServicioOperationError.Invalid);
        }

        if (!await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(SolicitudNuevoServicioOperationError.Invalid);
        }

        var duplicate = await _dbContext.SolicitudesNuevoServicio
            .AsNoTracking()
            .AnyAsync(
                s => s.PersonaSolicitanteId == normalized.Value.PersonaSolicitanteId &&
                     s.Estado == EstadoSolicitudNuevoServicio.Pendiente,
                cancellationToken);
        if (duplicate)
        {
            return Failure(SolicitudNuevoServicioOperationError.Conflict);
        }

        var request = new SolicitudNuevoServicio
        {
            PersonaSolicitanteId = normalized.Value.PersonaSolicitanteId,
            SectorId = normalized.Value.SectorId,
            DireccionReferencia = normalized.Value.DireccionReferencia,
            FechaSolicitud = DateTime.UtcNow,
            Estado = EstadoSolicitudNuevoServicio.Pendiente,
            Observacion = normalized.Value.Observacion
        };

        await using var transaction = await BeginTransactionIfRelationalAsync(cancellationToken);
        _dbContext.SolicitudesNuevoServicio.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "SOLICITUD_NUEVO_SERVICIO.CREAR",
            request.Id,
            "Ninguno",
            EstadoSolicitudNuevoServicio.Pendiente.ToString(),
            request.FechaSolicitud));
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await SuccessAsync(request.Id, cancellationToken);
    }

    public Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>> ApproveAsync(
        int id,
        SolicitudNuevoServicioResolutionInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(
            id,
            input,
            usuarioAdministrativoId,
            EstadoSolicitudNuevoServicio.Aprobada,
            "SOLICITUD_NUEVO_SERVICIO.APROBAR",
            cancellationToken);
    }

    public Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>> RejectAsync(
        int id,
        SolicitudNuevoServicioResolutionInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(
            id,
            input,
            usuarioAdministrativoId,
            EstadoSolicitudNuevoServicio.Rechazada,
            "SOLICITUD_NUEVO_SERVICIO.RECHAZAR",
            cancellationToken);
    }

    private async Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>> ResolveAsync(
        int id,
        SolicitudNuevoServicioResolutionInput input,
        int usuarioAdministrativoId,
        EstadoSolicitudNuevoServicio newState,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var observation = input is null ? null : NormalizeObservation(input.Observacion);
        if (id <= 0 || input is null || observation is null && input.Observacion is not null && input.Observacion.Trim().Length > 1000 || usuarioAdministrativoId <= 0)
        {
            return Failure(SolicitudNuevoServicioOperationError.Invalid);
        }

        var request = await _dbContext.SolicitudesNuevoServicio
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (request is null)
        {
            return Failure(SolicitudNuevoServicioOperationError.NotFound);
        }

        if (request.Estado != EstadoSolicitudNuevoServicio.Pendiente)
        {
            return Failure(SolicitudNuevoServicioOperationError.Conflict);
        }

        if (!await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(SolicitudNuevoServicioOperationError.Invalid);
        }

        if (newState == EstadoSolicitudNuevoServicio.Aprobada &&
            !await ValidateReferencesAsync(request.PersonaSolicitanteId, request.SectorId, cancellationToken))
        {
            return Failure(SolicitudNuevoServicioOperationError.Invalid);
        }

        var now = DateTime.UtcNow;
        await using var transaction = await BeginTransactionIfRelationalAsync(cancellationToken);

        if (newState == EstadoSolicitudNuevoServicio.Aprobada)
        {
            var suministro = new Suministro
            {
                SectorId = request.SectorId,
                DireccionReferencia = request.DireccionReferencia,
                Nis = await _nisGenerator.GenerateAsync(cancellationToken),
                CodigoQrToken = _qrTokenGenerator.Generate(),
                Estado = EstadoSuministro.Activo
            };
            _dbContext.Suministros.Add(suministro);
            _dbContext.PersonaSuministros.Add(new PersonaSuministro
            {
                PersonaId = request.PersonaSolicitanteId,
                Suministro = suministro,
                FechaInicio = now,
                Estado = EstadoRelacionSuministro.Vigente
            });
            request.Suministro = suministro;
        }

        request.Estado = newState;
        request.FechaResolucion = now;
        request.UsuarioResolucionId = usuarioAdministrativoId;
        request.Observacion = observation;

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            auditAction,
            request.Id,
            EstadoSolicitudNuevoServicio.Pendiente.ToString(),
            newState.ToString(),
            now));

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await SuccessAsync(request.Id, cancellationToken);
    }

    private IQueryable<SolicitudNuevoServicio> BuildQuery()
    {
        return _dbContext.SolicitudesNuevoServicio.AsNoTracking();
    }

    private Expression<Func<SolicitudNuevoServicio, SolicitudNuevoServicioDto>> ProjectDto()
    {
        return request => new SolicitudNuevoServicioDto(
            request.Id,
            request.PersonaSolicitanteId,
            request.PersonaSolicitante == null
                ? string.Empty
                : request.PersonaSolicitante.Nombres + " " + request.PersonaSolicitante.Apellidos,
            request.SectorId,
            request.Sector == null ? string.Empty : request.Sector.Nombre,
            request.DireccionReferencia,
            request.FechaSolicitud,
            request.Estado,
            request.FechaResolucion,
            request.UsuarioResolucionId,
            request.UsuarioResolucion == null ? null : request.UsuarioResolucion.NombreUsuario,
            request.Observacion,
            request.SuministroId,
            request.Suministro == null ? null : request.Suministro.Nis);
    }

    private async Task<bool> ValidateReferencesAsync(
        int personaId,
        int sectorId,
        CancellationToken cancellationToken)
    {
        return personaId > 0 && sectorId > 0 &&
            await _dbContext.Personas.AsNoTracking()
                .AnyAsync(p => p.Id == personaId && p.Estado == EstadoRegistro.Activo, cancellationToken) &&
            await _dbContext.Sectores.AsNoTracking()
                .AnyAsync(s => s.Id == sectorId && s.Estado == EstadoRegistro.Activo, cancellationToken);
    }

    private Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken)
    {
        return _dbContext.UsuariosAdministrativos.AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }

    private async Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>> SuccessAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        return dto is null
            ? Failure(SolicitudNuevoServicioOperationError.Conflict)
            : SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>.Success(dto);
    }

    private static SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto> Failure(
        SolicitudNuevoServicioOperationError error) =>
        SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>.Failure(error);

    private static Auditoria CreateAudit(
        int userId,
        string action,
        int requestId,
        string previousValue,
        string newValue,
        DateTime date) => new()
        {
            UsuarioAdministrativoId = userId,
            Accion = action,
            Entidad = "SolicitudNuevoServicio",
            EntidadId = requestId,
            Fecha = date,
            ValorAnterior = previousValue,
            ValorNuevo = newValue
        };

    private static (int PersonaSolicitanteId, int SectorId, string DireccionReferencia, string? Observacion)? NormalizeCreationInput(
        SolicitudNuevoServicioInput input)
    {
        if (input is null || input.PersonaSolicitanteId <= 0 || input.SectorId <= 0 ||
            string.IsNullOrWhiteSpace(input.DireccionReferencia))
        {
            return null;
        }

        var address = input.DireccionReferencia.Trim();
        var observation = NormalizeObservation(input.Observacion);
        if (address.Length > 500 || observation is null && input.Observacion is not null && input.Observacion.Trim().Length > 1000)
        {
            return null;
        }

        return (input.PersonaSolicitanteId, input.SectorId, address, observation);
    }

    private static string? NormalizeObservation(string? observation)
    {
        if (string.IsNullOrWhiteSpace(observation))
        {
            return null;
        }

        var normalized = observation.Trim();
        return normalized.Length <= 1000 ? normalized : null;
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfRelationalAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }
}
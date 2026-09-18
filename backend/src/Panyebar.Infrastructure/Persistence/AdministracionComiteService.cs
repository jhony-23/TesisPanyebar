using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.AdministracionComite;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class AdministracionComiteService : IAdministracionComiteService
{
    private readonly PanyebarDbContext _dbContext;

    public AdministracionComiteService(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AdministracionComiteSummary>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var administraciones = await _dbContext.AdministracionesComite
            .AsNoTracking()
            .OrderByDescending(a => a.FechaInicio)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);

        var ids = administraciones.Select(a => a.Id).ToArray();

        var integrantes = await _dbContext.IntegrantesAdministracion
            .AsNoTracking()
            .Where(i => ids.Contains(i.AdministracionComiteId))
            .Join(
                _dbContext.Personas.AsNoTracking(),
                i => i.PersonaId,
                p => p.Id,
                (i, p) => new
                {
                    Integrante = i,
                    PersonaNombre = p.Nombres + " " + p.Apellidos
                })
            .Join(
                _dbContext.Cargos.AsNoTracking(),
                x => x.Integrante.CargoId,
                c => c.Id,
                (x, c) => new
                {
                    x.Integrante.Id,
                    x.Integrante.AdministracionComiteId,
                    x.Integrante.PersonaId,
                    x.PersonaNombre,
                    CargoId = c.Id,
                    CargoNombre = c.Nombre
                })
            .OrderBy(x => x.CargoNombre)
            .ThenBy(x => x.PersonaNombre)
            .ToListAsync(cancellationToken);

        return administraciones
            .Select(a => new AdministracionComiteSummary(
                a.Id,
                a.Nombre,
                DateOnly.FromDateTime(a.FechaInicio),
                a.FechaFin.HasValue
                    ? DateOnly.FromDateTime(a.FechaFin.Value)
                    : null,
                a.Estado == EstadoRegistro.Activo &&
                a.FechaFin == null,
                integrantes
                    .Where(i => i.AdministracionComiteId == a.Id)
                    .Select(i => new IntegranteAdministracionSummary(
                        i.Id,
                        i.PersonaId,
                        i.PersonaNombre.Trim(),
                        i.CargoId,
                        i.CargoNombre))
                    .ToList()))
            .ToList();
    }

    public async Task<AdministracionComiteSummary?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        return (await GetAllAsync(cancellationToken))
            .SingleOrDefault(a => a.Id == id);
    }

    public async Task<IReadOnlyList<CargoSummary>> GetCargosAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Cargos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CargoSummary(
                c.Id,
                c.Nombre,
                c.Descripcion,
                c.Estado == EstadoRegistro.Activo))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdministracionComiteResult<AdministracionComiteSummary>> CreateAsync(
        CreateAdministracionComiteInput input,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var nombre = input?.Nombre?.Trim();

        if (input is null ||
            actorUsuarioId <= 0 ||
            string.IsNullOrWhiteSpace(nombre) ||
            nombre.Length > 200 ||
            input.FechaInicio == default)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Invalid);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Invalid);
        }

        var hasActive = await _dbContext.AdministracionesComite
            .AsNoTracking()
            .AnyAsync(
                a =>
                    a.Estado == EstadoRegistro.Activo &&
                    a.FechaFin == null,
                cancellationToken);

        if (hasActive)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Conflict);
        }

        var entity = new Domain.Entities.AdministracionComite
        {
            Nombre = nombre,
            FechaInicio = input.FechaInicio.ToDateTime(TimeOnly.MinValue),
            FechaFin = null,
            Estado = EstadoRegistro.Activo
        };

        _dbContext.AdministracionesComite.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "ADMINISTRACION.CREAR",
            "AdministracionComite",
            entity.Id,
            null,
            new
            {
                entity.Id,
                entity.Nombre,
                FechaInicio = input.FechaInicio
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministracionComiteResult<AdministracionComiteSummary>
            .Success((await GetByIdAsync(entity.Id, cancellationToken))!);
    }

    public async Task<AdministracionComiteResult<AdministracionComiteSummary>> SetIntegranteAsync(
        int administracionId,
        SetIntegranteAdministracionInput input,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        if (administracionId <= 0 ||
            input is null ||
            input.PersonaId <= 0 ||
            input.CargoId <= 0 ||
            actorUsuarioId <= 0)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Invalid);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Invalid);
        }

        var administracion = await _dbContext.AdministracionesComite
            .SingleOrDefaultAsync(
                a => a.Id == administracionId,
                cancellationToken);

        if (administracion is null)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.NotFound);
        }

        if (administracion.Estado != EstadoRegistro.Activo ||
            administracion.FechaFin != null)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Conflict);
        }

        var personaExists = await _dbContext.Personas
            .AsNoTracking()
            .AnyAsync(
                p =>
                    p.Id == input.PersonaId &&
                    p.Estado == EstadoRegistro.Activo,
                cancellationToken);

        var cargoExists = await _dbContext.Cargos
            .AsNoTracking()
            .AnyAsync(
                c =>
                    c.Id == input.CargoId &&
                    c.Estado == EstadoRegistro.Activo,
                cancellationToken);

        if (!personaExists || !cargoExists)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Invalid);
        }

        var current = await _dbContext.IntegrantesAdministracion
            .Where(i =>
                i.AdministracionComiteId == administracionId &&
                (i.PersonaId == input.PersonaId ||
                 i.CargoId == input.CargoId))
            .ToListAsync(cancellationToken);

        var exact = current.SingleOrDefault(i =>
            i.PersonaId == input.PersonaId &&
            i.CargoId == input.CargoId);

        if (exact is not null && current.Count == 1)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Success((await GetByIdAsync(
                    administracionId,
                    cancellationToken))!);
        }

        var previous = current
            .Select(i => new
            {
                i.Id,
                i.PersonaId,
                i.CargoId
            })
            .ToArray();

        if (current.Count > 0)
        {
            _dbContext.IntegrantesAdministracion.RemoveRange(current);
        }

        var entity = new IntegranteAdministracion
        {
            AdministracionComiteId = administracionId,
            PersonaId = input.PersonaId,
            CargoId = input.CargoId
        };

        _dbContext.IntegrantesAdministracion.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "ADMINISTRACION.INTEGRANTE.ASIGNAR",
            "AdministracionComite",
            administracionId,
            new { Asignaciones = previous },
            new
            {
                entity.Id,
                entity.PersonaId,
                entity.CargoId
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministracionComiteResult<AdministracionComiteSummary>
            .Success((await GetByIdAsync(
                administracionId,
                cancellationToken))!);
    }

    public async Task<AdministracionComiteResult<AdministracionComiteSummary>> FinishAsync(
        int administracionId,
        DateOnly fechaFin,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        if (administracionId <= 0 ||
            fechaFin == default ||
            actorUsuarioId <= 0)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Invalid);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Invalid);
        }

        var entity = await _dbContext.AdministracionesComite
            .SingleOrDefaultAsync(
                a => a.Id == administracionId,
                cancellationToken);

        if (entity is null)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.NotFound);
        }

        if (entity.Estado != EstadoRegistro.Activo ||
            entity.FechaFin != null)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Conflict);
        }

        var start = DateOnly.FromDateTime(entity.FechaInicio);

        if (fechaFin < start)
        {
            return AdministracionComiteResult<AdministracionComiteSummary>
                .Failure(AdministracionComiteError.Invalid);
        }

        var previous = new
        {
            FechaFin = (DateOnly?)null,
            Estado = entity.Estado.ToString()
        };

        entity.FechaFin = fechaFin.ToDateTime(TimeOnly.MinValue);
        entity.Estado = EstadoRegistro.Inactivo;

        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "ADMINISTRACION.FINALIZAR",
            "AdministracionComite",
            entity.Id,
            previous,
            new
            {
                FechaFin = fechaFin,
                Estado = entity.Estado.ToString()
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministracionComiteResult<AdministracionComiteSummary>
            .Success((await GetByIdAsync(
                administracionId,
                cancellationToken))!);
    }

    private async Task<bool> ActorExistsAsync(
        int actorUsuarioId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.Id == actorUsuarioId &&
                    u.Estado == EstadoRegistro.Activo,
                cancellationToken);
    }

    private void AddAudit(
        int actorUsuarioId,
        string action,
        string entity,
        int entityId,
        object? previous,
        object? current)
    {
        _dbContext.Auditorias.Add(new Auditoria
        {
            UsuarioAdministrativoId = actorUsuarioId,
            Accion = action,
            Entidad = entity,
            EntidadId = entityId,
            Fecha = DateTime.UtcNow,
            ValorAnterior = previous is null
                ? null
                : JsonSerializer.Serialize(previous),
            ValorNuevo = current is null
                ? null
                : JsonSerializer.Serialize(current)
        });
    }
}

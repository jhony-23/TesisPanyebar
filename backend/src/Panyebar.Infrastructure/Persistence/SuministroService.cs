using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Panyebar.Application.Suministros;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class SuministroService : ISuministroService
{
    private readonly PanyebarDbContext _dbContext;
    private readonly ISuministroNisGenerator _nisGenerator;
    private readonly ISuministroQrTokenGenerator _qrTokenGenerator;

    public SuministroService(
        PanyebarDbContext dbContext,
        ISuministroNisGenerator nisGenerator,
        ISuministroQrTokenGenerator qrTokenGenerator)
    {
        _dbContext = dbContext;
        _nisGenerator = nisGenerator;
        _qrTokenGenerator = qrTokenGenerator;
    }

    public async Task<IReadOnlyList<SuministroDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = SupplyQuery()
            .OrderBy(s => s.Nis)
            .ThenBy(s => s.Id);

        return await ProjectDto(query)
            .ToListAsync(cancellationToken);
    }

    public Task<SuministroDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = SupplyQuery()
            .Where(s => s.Id == id);

        return ProjectDto(query)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<SuministroDto?> GetByNisAsync(string nis, CancellationToken cancellationToken = default)
    {
        var normalizedNis = string.IsNullOrWhiteSpace(nis) ? null : nis.Trim();
        if (normalizedNis is null)
        {
            return Task.FromResult<SuministroDto?>(null);
        }

        var query = SupplyQuery()
            .Where(s => s.Nis == normalizedNis);

        return ProjectDto(query)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SuministroQrDto?> GetQrAsync(
        int suministroId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suministros
            .AsNoTracking()
            .Where(s => s.Id == suministroId)
            .Select(s => new SuministroQrDto(
                s.Id,
                s.Nis,
                $"/api/suministros/qr/{s.CodigoQrToken}"))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<SuministroDto?> GetByQrTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var normalizedToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        if (normalizedToken is null)
        {
            return Task.FromResult<SuministroDto?>(null);
        }

        var query = SupplyQuery()
            .Where(s => s.CodigoQrToken == normalizedToken);

        return ProjectDto(query)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResponsableHistorialDto>?> GetResponsablesAsync(
        int suministroId,
        CancellationToken cancellationToken = default)
    {
        var supplyExists = await _dbContext.Suministros
            .AsNoTracking()
            .AnyAsync(s => s.Id == suministroId, cancellationToken);
        if (!supplyExists)
        {
            return null;
        }

        var relationships = _dbContext.PersonaSuministros
            .AsNoTracking()
            .Where(ps => ps.SuministroId == suministroId)
            .Join(
                _dbContext.Personas.AsNoTracking(),
                ps => ps.PersonaId,
                person => person.Id,
                (ps, person) => new { Relationship = ps, Person = person })
            .OrderByDescending(item => item.Relationship.Estado == EstadoRelacionSuministro.Vigente)
            .ThenByDescending(item => item.Relationship.FechaInicio)
            .Select(item => new ResponsableHistorialDto(
                item.Relationship.Id,
                item.Person.Id,
                item.Person.Nombres,
                item.Person.Apellidos,
                item.Relationship.FechaInicio,
                item.Relationship.FechaFin,
                item.Relationship.Estado));

        return await relationships.ToListAsync(cancellationToken);
    }

    public async Task<SuministroOperationResult<SuministroDto>> CreateAsync(
        SuministroInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = await NormalizeAsync(input, cancellationToken);
        if (normalized is null)
        {
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.Invalid);
        }

        var suministro = new Suministro
        {
            SectorId = normalized.Value.SectorId,
            Nis = await _nisGenerator.GenerateAsync(cancellationToken),
            CodigoQrToken = _qrTokenGenerator.Generate(),
            DireccionReferencia = normalized.Value.DireccionReferencia,
            Estado = EstadoSuministro.Activo
        };

        _dbContext.Suministros.Add(suministro);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var query = SupplyQuery()
            .Where(s => s.Id == suministro.Id);

        return SuministroOperationResult<SuministroDto>.Success(
            await ProjectDto(query).SingleAsync(cancellationToken));
    }

    public async Task<SuministroOperationResult<SuministroDto>> UpdateAsync(
        int id,
        SuministroInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = await NormalizeAsync(input, cancellationToken);
        if (normalized is null)
        {
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.Invalid);
        }

        var suministro = await _dbContext.Suministros
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (suministro is null)
        {
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.NotFound);
        }

        suministro.SectorId = normalized.Value.SectorId;
        suministro.DireccionReferencia = normalized.Value.DireccionReferencia;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var query = SupplyQuery()
            .Where(s => s.Id == id);

        return SuministroOperationResult<SuministroDto>.Success(
            await ProjectDto(query).SingleAsync(cancellationToken));
    }

    public async Task<SuministroOperationResult<SuministroDto>> SetEstadoAsync(
        int id,
        EstadoSuministro estado,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(EstadoSuministro), estado))
        {
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.Invalid);
        }

        var suministro = await _dbContext.Suministros
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (suministro is null)
        {
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.NotFound);
        }

        suministro.Estado = estado;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var query = SupplyQuery()
            .Where(s => s.Id == id);

        return SuministroOperationResult<SuministroDto>.Success(
            await ProjectDto(query).SingleAsync(cancellationToken));
    }

    public async Task<SuministroOperationResult<SuministroDto>> SetResponsableAsync(
        int suministroId,
        SetResponsableInput input,
        CancellationToken cancellationToken = default)
    {
        if (suministroId <= 0 || input is null || input.PersonaId <= 0)
        {
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.Invalid);
        }

        var suministroExists = await _dbContext.Suministros
            .AsNoTracking()
            .AnyAsync(s => s.Id == suministroId, cancellationToken);
        if (!suministroExists)
        {
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.NotFound);
        }

        var personaIsActive = await _dbContext.Personas
            .AsNoTracking()
            .AnyAsync(p => p.Id == input.PersonaId && p.Estado == EstadoRegistro.Activo, cancellationToken);
        if (!personaIsActive)
        {
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.Invalid);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var current = await _dbContext.PersonaSuministros
            .SingleOrDefaultAsync(
                ps => ps.SuministroId == suministroId && ps.Estado == EstadoRelacionSuministro.Vigente,
                cancellationToken);

        if (current?.PersonaId == input.PersonaId)
        {
            await transaction.CommitAsync(cancellationToken);
            return await GetSupplyResultAsync(suministroId, cancellationToken);
        }

        if (current is not null)
        {
            current.FechaFin = now;
            current.Estado = EstadoRelacionSuministro.Finalizada;
        }

        _dbContext.PersonaSuministros.Add(new PersonaSuministro
        {
            PersonaId = input.PersonaId,
            SuministroId = suministroId,
            FechaInicio = now,
            Estado = EstadoRelacionSuministro.Vigente
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return SuministroOperationResult<SuministroDto>.Failure(SuministroOperationError.Conflict);
        }

        return await GetSupplyResultAsync(suministroId, cancellationToken);
    }

    private IQueryable<Suministro> SupplyQuery()
    {
        return _dbContext.Suministros.AsNoTracking();
    }

    private IQueryable<SuministroDto> ProjectDto(IQueryable<Suministro> supplies)
    {
        var activeRelationships = _dbContext.PersonaSuministros
            .AsNoTracking()
            .Where(ps => ps.Estado == EstadoRelacionSuministro.Vigente);

        return from s in supplies
               join sector in _dbContext.Sectores.AsNoTracking() on s.SectorId equals sector.Id
               join relationship in activeRelationships on s.Id equals relationship.SuministroId into relationships
               from relationship in relationships.DefaultIfEmpty()
               join person in _dbContext.Personas.AsNoTracking()
                   on (relationship == null ? 0 : relationship.PersonaId) equals person.Id into people
               from person in people.DefaultIfEmpty()
               select new SuministroDto(
                   s.Id,
                   s.Nis,
                   s.SectorId,
                   sector.Nombre,
                   s.DireccionReferencia,
                   s.Estado,
                person == null ? null : new ResponsableActualDto(person.Id, person.Nombres, person.Apellidos));
    }

    private async Task<SuministroOperationResult<SuministroDto>> GetSupplyResultAsync(
        int suministroId,
        CancellationToken cancellationToken)
    {
        var query = SupplyQuery().Where(s => s.Id == suministroId);
        var dto = await ProjectDto(query).SingleAsync(cancellationToken);
        return SuministroOperationResult<SuministroDto>.Success(dto);
    }

    private async Task<(int SectorId, string DireccionReferencia)?> NormalizeAsync(
        SuministroInput input,
        CancellationToken cancellationToken)
    {
        if (input is null || input.SectorId <= 0 || string.IsNullOrWhiteSpace(input.DireccionReferencia))
        {
            return null;
        }

        var direccion = input.DireccionReferencia.Trim();
        if (direccion.Length > 500)
        {
            return null;
        }

        var sectorIsActive = await _dbContext.Sectores
            .AsNoTracking()
            .AnyAsync(s => s.Id == input.SectorId && s.Estado == EstadoRegistro.Activo, cancellationToken);

        return sectorIsActive ? (input.SectorId, direccion) : null;
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
}
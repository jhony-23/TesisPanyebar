using Microsoft.EntityFrameworkCore;
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
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Panyebar.Application.Sectores;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class SectorService : ISectorService
{
    private readonly PanyebarDbContext _dbContext;

    public SectorService(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SectorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sectores
            .AsNoTracking()
            .OrderBy(s => s.Nombre)
            .ThenBy(s => s.Id)
            .Select(s => new SectorDto(s.Id, s.Nombre, s.Descripcion, s.Estado))
            .ToListAsync(cancellationToken);
    }

    public async Task<SectorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sectores
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SectorDto(s.Id, s.Nombre, s.Descripcion, s.Estado))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SectorOperationResult<SectorDto>> CreateAsync(
        SectorInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        if (normalized is null)
        {
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.Invalid);
        }

        if (await NameExistsAsync(normalized.Value.Nombre, null, cancellationToken))
        {
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.Duplicate);
        }

        var sector = new Sector
        {
            Nombre = normalized.Value.Nombre,
            Descripcion = normalized.Value.Descripcion,
            Estado = EstadoRegistro.Activo
        };

        _dbContext.Sectores.Add(sector);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.Entry(sector).State = EntityState.Detached;
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.Duplicate);
        }

        return SectorOperationResult<SectorDto>.Success(ToDto(sector));
    }

    public async Task<SectorOperationResult<SectorDto>> UpdateAsync(
        int id,
        SectorInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        if (normalized is null)
        {
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.Invalid);
        }

        var sector = await _dbContext.Sectores
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sector is null)
        {
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.NotFound);
        }

        if (await NameExistsAsync(normalized.Value.Nombre, id, cancellationToken))
        {
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.Duplicate);
        }

        sector.Nombre = normalized.Value.Nombre;
        sector.Descripcion = normalized.Value.Descripcion;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.Entry(sector).State = EntityState.Detached;
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.Duplicate);
        }

        return SectorOperationResult<SectorDto>.Success(ToDto(sector));
    }

    public async Task<SectorOperationResult<SectorDto>> SetEstadoAsync(
        int id,
        EstadoRegistro estado,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(EstadoRegistro), estado))
        {
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.Invalid);
        }

        var sector = await _dbContext.Sectores
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sector is null)
        {
            return SectorOperationResult<SectorDto>.Failure(SectorOperationError.NotFound);
        }

        sector.Estado = estado;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return SectorOperationResult<SectorDto>.Success(ToDto(sector));
    }

    private async Task<bool> NameExistsAsync(
        string nombre,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Sectores
            .AsNoTracking()
            .Where(s => excludedId == null || s.Id != excludedId.Value)
            .AnyAsync(s => EF.Functions.Collate(s.Nombre, "Latin1_General_100_CI_AS") == nombre, cancellationToken);
    }

    private static (string Nombre, string? Descripcion)? Normalize(SectorInput input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Nombre) || input.Nombre.Trim().Length > 100)
        {
            return null;
        }

        var descripcion = string.IsNullOrWhiteSpace(input.Descripcion)
            ? null
            : input.Descripcion.Trim();
        if (descripcion?.Length > 500)
        {
            return null;
        }

        return (input.Nombre.Trim(), descripcion);
    }

    private static SectorDto ToDto(Sector sector) =>
        new(sector.Id, sector.Nombre, sector.Descripcion, sector.Estado);

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

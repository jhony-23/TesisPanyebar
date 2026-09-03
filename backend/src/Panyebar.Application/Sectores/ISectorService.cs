using Panyebar.Domain.Enums;

namespace Panyebar.Application.Sectores;

public interface ISectorService
{
    Task<IReadOnlyList<SectorDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SectorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SectorOperationResult<SectorDto>> CreateAsync(SectorInput input, CancellationToken cancellationToken = default);
    Task<SectorOperationResult<SectorDto>> UpdateAsync(int id, SectorInput input, CancellationToken cancellationToken = default);
    Task<SectorOperationResult<SectorDto>> SetEstadoAsync(int id, EstadoRegistro estado, CancellationToken cancellationToken = default);
}

public sealed record SectorDto(
    int Id,
    string Nombre,
    string? Descripcion,
    EstadoRegistro Estado);

public sealed record SectorInput(string? Nombre, string? Descripcion);

public enum SectorOperationError
{
    None,
    Invalid,
    NotFound,
    Duplicate
}

public sealed record SectorOperationResult<T>(T? Value, SectorOperationError Error)
{
    public bool Succeeded => Error == SectorOperationError.None;

    public static SectorOperationResult<T> Success(T value) => new(value, SectorOperationError.None);
    public static SectorOperationResult<T> Failure(SectorOperationError error) => new(default, error);
}

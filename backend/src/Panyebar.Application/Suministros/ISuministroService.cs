using Panyebar.Domain.Enums;

namespace Panyebar.Application.Suministros;

public interface ISuministroService
{
    Task<IReadOnlyList<SuministroDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SuministroDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SuministroDto?> GetByNisAsync(string nis, CancellationToken cancellationToken = default);
    Task<SuministroOperationResult<SuministroDto>> CreateAsync(SuministroInput input, CancellationToken cancellationToken = default);
    Task<SuministroOperationResult<SuministroDto>> UpdateAsync(int id, SuministroInput input, CancellationToken cancellationToken = default);
    Task<SuministroOperationResult<SuministroDto>> SetEstadoAsync(int id, EstadoSuministro estado, CancellationToken cancellationToken = default);
}

public sealed record SuministroDto(
    int Id,
    string Nis,
    int SectorId,
    string SectorNombre,
    string DireccionReferencia,
    EstadoSuministro Estado,
    ResponsableActualDto? ResponsableActual);

public sealed record ResponsableActualDto(
    int PersonaId,
    string Nombres,
    string Apellidos);

public sealed record SuministroInput(
    int SectorId,
    string? DireccionReferencia);

public enum SuministroOperationError
{
    None,
    Invalid,
    NotFound
}

public sealed record SuministroOperationResult<T>(T? Value, SuministroOperationError Error)
{
    public bool Succeeded => Error == SuministroOperationError.None;

    public static SuministroOperationResult<T> Success(T value) => new(value, SuministroOperationError.None);
    public static SuministroOperationResult<T> Failure(SuministroOperationError error) => new(default, error);
}
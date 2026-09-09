using Panyebar.Domain.Enums;

namespace Panyebar.Application.Cuotas;

public interface ICuotaService
{
    Task<IReadOnlyList<CuotaDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CuotaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CuotaOperationResult<CuotaDto>> CreateAsync(
        CuotaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
    Task<CuotaOperationResult<CuotaDto>> UpdateAsync(
        int id,
        CuotaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
    Task<CuotaOperationResult<CuotaDto>> SetEstadoAsync(
        int id,
        EstadoRegistro estado,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
}

public sealed record CuotaDto(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal Monto,
    PeriodicidadCuota Periodicidad,
    DateTime FechaInicioVigencia,
    DateTime? FechaFinVigencia,
    EstadoRegistro Estado);

public sealed record CuotaInput(
    string? Nombre,
    string? Descripcion,
    decimal Monto,
    PeriodicidadCuota Periodicidad,
    DateTime FechaInicioVigencia,
    DateTime? FechaFinVigencia);

public enum CuotaOperationError
{
    None,
    Invalid,
    NotFound
}

public sealed record CuotaOperationResult<T>(T? Value, CuotaOperationError Error)
{
    public bool Succeeded => Error == CuotaOperationError.None;

    public static CuotaOperationResult<T> Success(T value) => new(value, CuotaOperationError.None);
    public static CuotaOperationResult<T> Failure(CuotaOperationError error) => new(default, error);
}

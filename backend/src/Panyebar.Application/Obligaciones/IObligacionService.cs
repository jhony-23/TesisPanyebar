using Panyebar.Domain.Enums;

namespace Panyebar.Application.Obligaciones;

public interface IObligacionService
{
    Task<IReadOnlyList<ObligacionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ObligacionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ObligacionOperationResult<ObligacionDto>> GenerateFromCuotaAsync(
        GenerarObligacionCuotaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
    Task<ObligacionOperationResult<ObligacionDto>> AnnulAsync(
        int id,
        AnularObligacionInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
}

public sealed record ObligacionDto(
    int Id,
    int? PersonaId,
    int? SuministroId,
    int? CuotaId,
    OrigenObligacion Origen,
    string Concepto,
    decimal Monto,
    string? Periodo,
    DateTime FechaGeneracion,
    DateTime? FechaVencimiento,
    EstadoObligacion Estado,
    bool EsMorosa);

public sealed record GenerarObligacionCuotaInput(
    int CuotaId,
    int SuministroId,
    string? Periodo,
    DateTime? FechaVencimiento);

public sealed record AnularObligacionInput(string? Motivo);

public enum ObligacionOperationError
{
    None,
    Invalid,
    NotFound,
    Conflict
}

public sealed record ObligacionOperationResult<T>(T? Value, ObligacionOperationError Error)
{
    public bool Succeeded => Error == ObligacionOperationError.None;

    public static ObligacionOperationResult<T> Success(T value) => new(value, ObligacionOperationError.None);
    public static ObligacionOperationResult<T> Failure(ObligacionOperationError error) => new(default, error);
}

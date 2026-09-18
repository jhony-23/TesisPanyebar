namespace Panyebar.Application.Abastecimiento;

public interface IAbastecimientoService
{
    Task<AbastecimientoOperationResult<IReadOnlyList<ProgramacionAbastecimientoDto>>> GetAllAsync(
        DateOnly? fechaDesde,
        DateOnly? fechaHasta,
        int? sectorId,
        CancellationToken cancellationToken = default);

    Task<ProgramacionAbastecimientoDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> CreateAsync(
        ProgramacionAbastecimientoInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<AbastecimientoOperationResult<CreacionRecurrenteAbastecimientoDto>> CreateRecurringAsync(
        ProgramacionRecurrenteAbastecimientoInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> UpdateAsync(
        int id,
        ProgramacionAbastecimientoInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> CompleteAsync(
        int id,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<AbastecimientoOperationResult<ProgramacionAbastecimientoDto>> CancelAsync(
        int id,
        ActualizarEstadoAbastecimientoInput? input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
}

public static class EstadosProgramacionAbastecimiento
{
    public const string Programado = "Programado";
    public const string Completado = "Completado";
    public const string Cancelado = "Cancelado";
}

public enum TipoRecurrenciaAbastecimiento
{
    Semanal = 1,
    Mensual = 2,
    Anual = 3
}

public sealed record ProgramacionAbastecimientoDto(
    int Id,
    int SectorId,
    string SectorNombre,
    DateTime Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    string Estado,
    string? Observacion);

public sealed record ProgramacionAbastecimientoInput(
    int SectorId,
    DateOnly Fecha,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    string? Observacion);

public sealed record ProgramacionRecurrenteAbastecimientoInput(
    int SectorId,
    DateOnly FechaInicial,
    TimeSpan HoraInicio,
    TimeSpan HoraFin,
    string? Observacion,
    TipoRecurrenciaAbastecimiento Recurrencia,
    int? CantidadOcurrencias,
    DateOnly? FechaFin);

public sealed record CreacionRecurrenteAbastecimientoDto(
    int CantidadCreada,
    IReadOnlyList<ProgramacionAbastecimientoDto> Programaciones);

public sealed record ActualizarEstadoAbastecimientoInput(
    string? Observacion);

public enum AbastecimientoOperationError
{
    None,
    Invalid,
    NotFound,
    Conflict,
    SectorNotFound,
    SectorInactive
}

public sealed record AbastecimientoOperationResult<T>(
    T? Value,
    AbastecimientoOperationError Error)
{
    public bool Succeeded =>
        Error == AbastecimientoOperationError.None;

    public static AbastecimientoOperationResult<T> Success(T value) =>
        new(value, AbastecimientoOperationError.None);

    public static AbastecimientoOperationResult<T> Failure(
        AbastecimientoOperationError error) =>
        new(default, error);
}

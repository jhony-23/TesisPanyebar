using Panyebar.Domain.Enums;

namespace Panyebar.Application.Suministros;

public interface ISuministroService
{
    Task<IReadOnlyList<SuministroDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SuministroDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SuministroDto?> GetByNisAsync(string nis, CancellationToken cancellationToken = default);
    Task<SuministroQrDto?> GetQrAsync(int suministroId, CancellationToken cancellationToken = default);
    Task<SuministroDto?> GetByQrTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResponsableHistorialDto>?> GetResponsablesAsync(int suministroId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcesoSuministroDto>?> GetProcesosAsync(int suministroId, CancellationToken cancellationToken = default);
    Task<SuministroOperationResult<SuministroDto>> CreateAsync(SuministroInput input, CancellationToken cancellationToken = default);
    Task<SuministroOperationResult<SuministroDto>> UpdateAsync(int id, SuministroInput input, CancellationToken cancellationToken = default);
    Task<SuministroOperationResult<SuministroDto>> CancelAsync(int suministroId, SuministroProcesoInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default);
    Task<SuministroOperationResult<SuministroDto>> ReconnectAsync(int suministroId, SuministroProcesoInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default);
    Task<SuministroOperationResult<SuministroDto>> SetResponsableAsync(int suministroId, SetResponsableInput input, CancellationToken cancellationToken = default);
}

public sealed record SuministroDto(
    int Id,
    string Nis,
    int SectorId,
    string SectorNombre,
    string DireccionReferencia,
    EstadoSuministro Estado,
    ResponsableActualDto? ResponsableActual);

public sealed record SuministroQrDto(
    int SuministroId,
    string Nis,
    string QrValue);

public sealed record ResponsableActualDto(
    int PersonaId,
    string Nombres,
    string Apellidos);

public sealed record ResponsableHistorialDto(
    int PersonaSuministroId,
    int PersonaId,
    string Nombres,
    string Apellidos,
    DateTime FechaInicio,
    DateTime? FechaFin,
    EstadoRelacionSuministro Estado);

public sealed record SetResponsableInput(int PersonaId);

public sealed record SuministroProcesoInput(string? Motivo, string? Observacion);

public sealed record ProcesoSuministroDto(
    int ProcesoSuministroId,
    TipoProcesoSuministro TipoProceso,
    EstadoSuministro EstadoAnterior,
    EstadoSuministro EstadoNuevo,
    DateTime Fecha,
    int UsuarioAdministrativoId,
    string Usuario,
    string Motivo,
    string? Observacion);

public sealed record SuministroInput(
    int SectorId,
    string? DireccionReferencia);

public enum SuministroOperationError
{
    None,
    Invalid,
    NotFound,
    Conflict
}

public sealed record SuministroOperationResult<T>(T? Value, SuministroOperationError Error)
{
    public bool Succeeded => Error == SuministroOperationError.None;

    public static SuministroOperationResult<T> Success(T value) => new(value, SuministroOperationError.None);
    public static SuministroOperationResult<T> Failure(SuministroOperationError error) => new(default, error);
}
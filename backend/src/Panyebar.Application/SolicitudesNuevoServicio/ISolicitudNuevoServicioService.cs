using Panyebar.Domain.Enums;

namespace Panyebar.Application.SolicitudesNuevoServicio;

public interface ISolicitudNuevoServicioService
{
    Task<IReadOnlyList<SolicitudNuevoServicioDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SolicitudNuevoServicioDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>> CreateAsync(
        SolicitudNuevoServicioInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
    Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>> ApproveAsync(
        int id,
        SolicitudNuevoServicioResolutionInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
    Task<SolicitudNuevoServicioOperationResult<SolicitudNuevoServicioDto>> RejectAsync(
        int id,
        SolicitudNuevoServicioResolutionInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
}

public sealed record SolicitudNuevoServicioInput(
    int PersonaSolicitanteId,
    int SectorId,
    string? DireccionReferencia,
    string? Observacion);

public sealed record SolicitudNuevoServicioResolutionInput(string? Observacion);

public sealed record SolicitudNuevoServicioDto(
    int SolicitudNuevoServicioId,
    int PersonaSolicitanteId,
    string PersonaSolicitante,
    int SectorId,
    string SectorNombre,
    string DireccionReferencia,
    DateTime FechaSolicitud,
    EstadoSolicitudNuevoServicio Estado,
    DateTime? FechaResolucion,
    int? UsuarioResolucionId,
    string? UsuarioResolucion,
    string? Observacion,
    int? SuministroId,
    string? Nis);

public enum SolicitudNuevoServicioOperationError
{
    None,
    Invalid,
    NotFound,
    Conflict
}

public sealed record SolicitudNuevoServicioOperationResult<T>(
    T? Value,
    SolicitudNuevoServicioOperationError Error)
{
    public bool Succeeded => Error == SolicitudNuevoServicioOperationError.None;

    public static SolicitudNuevoServicioOperationResult<T> Success(T value) =>
        new(value, SolicitudNuevoServicioOperationError.None);

    public static SolicitudNuevoServicioOperationResult<T> Failure(
        SolicitudNuevoServicioOperationError error) =>
        new(default, error);
}
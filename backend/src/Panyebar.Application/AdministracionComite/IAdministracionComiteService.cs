namespace Panyebar.Application.AdministracionComite;

public interface IAdministracionComiteService
{
    Task<IReadOnlyList<AdministracionComiteSummary>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<AdministracionComiteSummary?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CargoSummary>> GetCargosAsync(
        CancellationToken cancellationToken = default);

    Task<AdministracionComiteResult<AdministracionComiteSummary>> CreateAsync(
        CreateAdministracionComiteInput input,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<AdministracionComiteResult<AdministracionComiteSummary>> SetIntegranteAsync(
        int administracionId,
        SetIntegranteAdministracionInput input,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<AdministracionComiteResult<AdministracionComiteSummary>> FinishAsync(
        int administracionId,
        DateOnly fechaFin,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateAdministracionComiteInput(
    string Nombre,
    DateOnly FechaInicio);

public sealed record SetIntegranteAdministracionInput(
    int PersonaId,
    int CargoId);

public sealed record AdministracionComiteSummary(
    int Id,
    string Nombre,
    DateOnly FechaInicio,
    DateOnly? FechaFin,
    bool Activa,
    IReadOnlyList<IntegranteAdministracionSummary> Integrantes);

public sealed record IntegranteAdministracionSummary(
    int Id,
    int PersonaId,
    string PersonaNombre,
    int CargoId,
    string CargoNombre);

public sealed record CargoSummary(
    int Id,
    string Nombre,
    string? Descripcion,
    bool Activo);

public enum AdministracionComiteError
{
    None,
    Invalid,
    NotFound,
    Conflict
}

public sealed record AdministracionComiteResult<T>(
    T? Value,
    AdministracionComiteError Error)
{
    public bool Succeeded => Error == AdministracionComiteError.None;

    public static AdministracionComiteResult<T> Success(T value) =>
        new(value, AdministracionComiteError.None);

    public static AdministracionComiteResult<T> Failure(
        AdministracionComiteError error) =>
        new(default, error);
}

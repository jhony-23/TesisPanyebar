using Panyebar.Domain.Enums;

namespace Panyebar.Application.Security;

public interface IAdministrativeAccessService
{
    Task<IReadOnlyList<UsuarioAdministrativoAccessSummary>> GetUsuariosAsync(
        CancellationToken cancellationToken = default);

    Task<UsuarioAdministrativoAccessSummary?> GetUsuarioByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>> CreateUsuarioAsync(
        string nombreUsuario,
        string password,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>> SetRolesForUsuarioAsync(
        int usuarioAdministrativoId,
        IEnumerable<int> rolIds,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>> SetUsuarioEstadoAsync(
        int usuarioAdministrativoId,
        EstadoRegistro estado,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>> ResetUsuarioPasswordAsync(
        int usuarioAdministrativoId,
        string nuevaPassword,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RolAccessSummary>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    Task<RolAccessSummary?> GetRolByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<AdministrativeAccessResult<RolAccessSummary>> CreateRolAsync(
        string nombre,
        string? descripcion,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<AdministrativeAccessResult<RolAccessSummary>> SetPermisosForRolAsync(
        int rolId,
        IEnumerable<int> permisoIds,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<AdministrativeAccessResult<RolAccessSummary>> SetRolEstadoAsync(
        int rolId,
        EstadoRegistro estado,
        int actorUsuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermisoSummary>> GetPermisosAsync(
        CancellationToken cancellationToken = default);

    Task<PermisoSummary?> GetPermisoByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditoriaAdministrativaDto>> GetAuditoriaAsync(
        DateTime? fechaDesdeUtc,
        DateTime? fechaHastaUtcExclusive,
        int? usuarioId,
        string? accion,
        string? entidad,
        int limite = 200,
        CancellationToken cancellationToken = default);
}

public sealed record UsuarioAdministrativoAccessSummary(
    int Id,
    string NombreUsuario,
    EstadoRegistro Estado,
    IReadOnlyList<RolSummary> Roles);

public sealed record RolSummary(
    int Id,
    string Nombre,
    string? Descripcion,
    EstadoRegistro Estado);

public sealed record RolAccessSummary(
    int Id,
    string Nombre,
    string? Descripcion,
    EstadoRegistro Estado,
    IReadOnlyList<PermisoSummary> Permisos);

public sealed record PermisoSummary(
    int Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    EstadoRegistro Estado);

public enum AdministrativeAccessError
{
    None,
    Invalid,
    NotFound,
    Duplicate,
    Conflict
}

public sealed record AdministrativeAccessResult<T>(
    T? Value,
    AdministrativeAccessError Error)
{
    public bool Succeeded => Error == AdministrativeAccessError.None;

    public static AdministrativeAccessResult<T> Success(T value) =>
        new(value, AdministrativeAccessError.None);

    public static AdministrativeAccessResult<T> Failure(
        AdministrativeAccessError error) =>
        new(default, error);
}
public sealed record AuditoriaAdministrativaDto(
    int Id,
    DateTime Fecha,
    int UsuarioAdministrativoId,
    string NombreUsuario,
    string Accion,
    string Entidad,
    int EntidadId,
    string? ValorAnterior,
    string? ValorNuevo);

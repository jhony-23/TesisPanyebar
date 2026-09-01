using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Application.Security;

public interface IAdministrativeAccessService
{
    Task<IReadOnlyList<UsuarioAdministrativoAccessSummary>> GetUsuariosAsync(CancellationToken cancellationToken = default);
    Task<UsuarioAdministrativoAccessSummary?> GetUsuarioByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UsuarioAdministrativoAccessSummary?> CreateUsuarioAsync(string nombreUsuario, string password, CancellationToken cancellationToken = default);
    Task<bool> SetRolesForUsuarioAsync(int usuarioAdministrativoId, IEnumerable<int> rolIds, CancellationToken cancellationToken = default);
    Task<bool> SetUsuarioEstadoAsync(int usuarioAdministrativoId, EstadoRegistro estado, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RolAccessSummary>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<RolAccessSummary?> GetRolByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RolAccessSummary?> CreateRolAsync(string nombre, string? descripcion, CancellationToken cancellationToken = default);
    Task<bool> SetPermisosForRolAsync(int rolId, IEnumerable<int> permisoIds, CancellationToken cancellationToken = default);
    Task<bool> SetRolEstadoAsync(int rolId, EstadoRegistro estado, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermisoSummary>> GetPermisosAsync(CancellationToken cancellationToken = default);
    Task<PermisoSummary?> GetPermisoByIdAsync(int id, CancellationToken cancellationToken = default);
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

namespace Panyebar.Application.Security;

public interface IUsuarioPermissionRepository
{
    Task<bool> HasPermissionAsync(
        int usuarioAdministrativoId,
        string permisoCodigo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
}

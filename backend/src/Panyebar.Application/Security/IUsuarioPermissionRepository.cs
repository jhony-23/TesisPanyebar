namespace Panyebar.Application.Security;

public interface IUsuarioPermissionRepository
{
    Task<bool> HasPermissionAsync(
        int usuarioAdministrativoId,
        string permisoCodigo,
        CancellationToken cancellationToken = default);
}

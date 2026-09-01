namespace Panyebar.Application.Security;

public interface IUsuarioAdministrativoAuthenticationService
{
    Task<UsuarioAdministrativoAuthenticationResult> AuthenticateAsync(
        string nombreUsuario,
        string password,
        CancellationToken cancellationToken = default);
}

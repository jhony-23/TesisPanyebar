namespace Panyebar.Application.Security;

public interface IAccessTokenService
{
    AccessTokenResult Generate(UsuarioAdministrativoAuthenticationResult usuario);
}

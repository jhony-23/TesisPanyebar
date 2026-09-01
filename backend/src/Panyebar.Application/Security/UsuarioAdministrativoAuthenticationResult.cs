namespace Panyebar.Application.Security;

public sealed class UsuarioAdministrativoAuthenticationResult
{
    public bool IsAuthenticated { get; private init; }
    public int? Id { get; private init; }
    public string? NombreUsuario { get; private init; }

    public static UsuarioAdministrativoAuthenticationResult Success(int id, string nombreUsuario)
    {
        return new UsuarioAdministrativoAuthenticationResult
        {
            IsAuthenticated = true,
            Id = id,
            NombreUsuario = nombreUsuario
        };
    }

    public static UsuarioAdministrativoAuthenticationResult Failure()
    {
        return new UsuarioAdministrativoAuthenticationResult
        {
            IsAuthenticated = false,
            Id = null,
            NombreUsuario = null
        };
    }
}

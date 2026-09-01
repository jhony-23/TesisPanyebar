using Panyebar.Domain.Entities;

namespace Panyebar.Application.Security;

public sealed class UsuarioAdministrativoAuthenticationService : IUsuarioAdministrativoAuthenticationService
{
    private readonly IUsuarioAdministrativoAuthenticationRepository _repository;
    private readonly IPasswordHashService _passwordHashService;

    public UsuarioAdministrativoAuthenticationService(
        IUsuarioAdministrativoAuthenticationRepository repository,
        IPasswordHashService passwordHashService)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
    }

    public async Task<UsuarioAdministrativoAuthenticationResult> AuthenticateAsync(
        string nombreUsuario,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
        {
            return UsuarioAdministrativoAuthenticationResult.Failure();
        }

        var normalizedNombreUsuario = nombreUsuario.Trim();
        var user = await _repository.GetActiveByNombreUsuarioAsync(normalizedNombreUsuario, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return UsuarioAdministrativoAuthenticationResult.Failure();
        }

        var isValid = _passwordHashService.Verify(user.PasswordHash, password);

        if (!isValid)
        {
            return UsuarioAdministrativoAuthenticationResult.Failure();
        }

        return UsuarioAdministrativoAuthenticationResult.Success(user.Id, user.NombreUsuario);
    }
}

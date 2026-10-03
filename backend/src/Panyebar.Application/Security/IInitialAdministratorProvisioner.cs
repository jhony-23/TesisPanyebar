namespace Panyebar.Application.Security;

public interface IInitialAdministratorProvisioner
{
    Task<InitialAdministratorProvisioningResult> ProvisionAsync(
        string nombreUsuario,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record InitialAdministratorProvisioningResult(
    bool Succeeded,
    string Message,
    int? UsuarioAdministrativoId = null);
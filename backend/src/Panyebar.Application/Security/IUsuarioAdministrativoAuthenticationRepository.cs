using Panyebar.Domain.Entities;

namespace Panyebar.Application.Security;

public interface IUsuarioAdministrativoAuthenticationRepository
{
    Task<UsuarioAdministrativo?> GetActiveByNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default);
}

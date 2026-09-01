using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Infrastructure.Security;

public sealed class UsuarioAdministrativoAuthenticationRepository : IUsuarioAdministrativoAuthenticationRepository
{
    private readonly PanyebarDbContext _dbContext;

    public UsuarioAdministrativoAuthenticationRepository(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UsuarioAdministrativo?> GetActiveByNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NombreUsuario == nombreUsuario &&
                       user.Estado == EstadoRegistro.Activo,
                cancellationToken);
    }
}

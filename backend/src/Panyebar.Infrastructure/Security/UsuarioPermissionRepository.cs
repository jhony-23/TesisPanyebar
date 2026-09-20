using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Security;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Infrastructure.Security;

public sealed class UsuarioPermissionRepository : IUsuarioPermissionRepository
{
    private readonly PanyebarDbContext _dbContext;

    public UsuarioPermissionRepository(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasPermissionAsync(
        int usuarioAdministrativoId,
        string permisoCodigo,
        CancellationToken cancellationToken = default)
    {
        if (usuarioAdministrativoId <= 0 || string.IsNullOrWhiteSpace(permisoCodigo))
        {
            return false;
        }

        var isUserActive = await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(u => u.Id == usuarioAdministrativoId && u.Estado == EstadoRegistro.Activo, cancellationToken);

        if (!isUserActive)
        {
            return false;
        }

        return await _dbContext.UsuarioRoles
            .AsNoTracking()
            .Where(ur => ur.UsuarioAdministrativoId == usuarioAdministrativoId)
            .Join(
                _dbContext.Roles.AsNoTracking(),
                ur => ur.RolId,
                r => r.Id,
                (ur, r) => r)
            .Where(r => r.Estado == EstadoRegistro.Activo)
            .Join(
                _dbContext.RolPermisos.AsNoTracking(),
                r => r.Id,
                rp => rp.RolId,
                (r, rp) => rp)
            .Join(
                _dbContext.Permisos.AsNoTracking(),
                rp => rp.PermisoId,
                p => p.Id,
                (rp, p) => p)
            .AnyAsync(p => p.Codigo == permisoCodigo && p.Estado == EstadoRegistro.Activo, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioAdministrativoId <= 0)
        {
            return Array.Empty<string>();
        }

        var isUserActive = await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(u => u.Id == usuarioAdministrativoId && u.Estado == EstadoRegistro.Activo, cancellationToken);

        if (!isUserActive)
        {
            return Array.Empty<string>();
        }

        return await _dbContext.UsuarioRoles
            .AsNoTracking()
            .Where(ur => ur.UsuarioAdministrativoId == usuarioAdministrativoId)
            .Join(
                _dbContext.Roles.AsNoTracking(),
                ur => ur.RolId,
                r => r.Id,
                (ur, r) => r)
            .Where(r => r.Estado == EstadoRegistro.Activo)
            .Join(
                _dbContext.RolPermisos.AsNoTracking(),
                r => r.Id,
                rp => rp.RolId,
                (r, rp) => rp)
            .Join(
                _dbContext.Permisos.AsNoTracking(),
                rp => rp.PermisoId,
                p => p.Id,
                (rp, p) => p)
            .Where(p => p.Estado == EstadoRegistro.Activo && !string.IsNullOrWhiteSpace(p.Codigo))
            .Select(p => p.Codigo)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}

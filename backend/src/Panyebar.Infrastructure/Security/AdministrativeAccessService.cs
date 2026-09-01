using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Infrastructure.Security;

public sealed class AdministrativeAccessService : IAdministrativeAccessService
{
    private readonly PanyebarDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;

    public AdministrativeAccessService(PanyebarDbContext dbContext, IPasswordHashService passwordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
    }

    public async Task<IReadOnlyList<UsuarioAdministrativoAccessSummary>> GetUsuariosAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UsuarioAdministrativoAccessSummary(
                u.Id,
                u.NombreUsuario,
                u.Estado,
                _dbContext.UsuarioRoles
                    .AsNoTracking()
                    .Where(ur => ur.UsuarioAdministrativoId == u.Id)
                    .Join(
                        _dbContext.Roles.AsNoTracking(),
                        ur => ur.RolId,
                        r => r.Id,
                        (ur, r) => new RolSummary(
                            r.Id,
                            r.Nombre,
                            r.Descripcion,
                            r.Estado))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<UsuarioAdministrativoAccessSummary?> GetUsuarioByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioAdministrativoAccessSummary(
                u.Id,
                u.NombreUsuario,
                u.Estado,
                _dbContext.UsuarioRoles
                    .AsNoTracking()
                    .Where(ur => ur.UsuarioAdministrativoId == u.Id)
                    .Join(
                        _dbContext.Roles.AsNoTracking(),
                        ur => ur.RolId,
                        r => r.Id,
                        (ur, r) => new RolSummary(
                            r.Id,
                            r.Nombre,
                            r.Descripcion,
                            r.Estado))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UsuarioAdministrativoAccessSummary?> CreateUsuarioAsync(
        string nombreUsuario,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var normalizedNombreUsuario = nombreUsuario.Trim();

        var userExists = await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(u => u.NombreUsuario == normalizedNombreUsuario, cancellationToken);

        if (userExists)
        {
            return null;
        }

        var entity = new UsuarioAdministrativo
        {
            NombreUsuario = normalizedNombreUsuario,
            PasswordHash = _passwordHashService.Hash(password),
            Estado = EstadoRegistro.Activo
        };

        _dbContext.UsuariosAdministrativos.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetUsuarioByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<bool> SetRolesForUsuarioAsync(
        int usuarioAdministrativoId,
        IEnumerable<int> rolIds,
        CancellationToken cancellationToken = default)
    {
        if (usuarioAdministrativoId <= 0)
        {
            return false;
        }

        var requestedRoles = rolIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var usuario = await _dbContext.UsuariosAdministrativos
            .SingleOrDefaultAsync(u => u.Id == usuarioAdministrativoId, cancellationToken);

        if (usuario is null)
        {
            return false;
        }

        var validRoleIds = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Estado == EstadoRegistro.Activo)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var requestedValidRoles = requestedRoles
            .Where(id => validRoleIds.Contains(id))
            .ToList();

        if (requestedRoles.Count != requestedValidRoles.Count)
        {
            return false;
        }

        var currentAssignments = await _dbContext.UsuarioRoles
            .Where(ur => ur.UsuarioAdministrativoId == usuario.Id)
            .ToListAsync(cancellationToken);

        var currentRoleIds = currentAssignments.Select(ur => ur.RolId).ToHashSet();

        foreach (var usuarioRol in currentAssignments.Where(ur => !requestedValidRoles.Contains(ur.RolId)).ToList())
        {
            _dbContext.UsuarioRoles.Remove(usuarioRol);
        }

        foreach (var rolId in requestedValidRoles.Where(id => !currentRoleIds.Contains(id)))
        {
            _dbContext.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioAdministrativoId = usuario.Id,
                RolId = rolId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetUsuarioEstadoAsync(
        int usuarioAdministrativoId,
        EstadoRegistro estado,
        CancellationToken cancellationToken = default)
    {
        if (usuarioAdministrativoId <= 0 || !Enum.IsDefined(typeof(EstadoRegistro), estado))
        {
            return false;
        }

        var usuario = await _dbContext.UsuariosAdministrativos
            .SingleOrDefaultAsync(u => u.Id == usuarioAdministrativoId, cancellationToken);

        if (usuario is null)
        {
            return false;
        }

        usuario.Estado = estado;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<RolAccessSummary>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RolAccessSummary(
                r.Id,
                r.Nombre,
                r.Descripcion,
                r.Estado,
                _dbContext.RolPermisos
                    .AsNoTracking()
                    .Where(rp => rp.RolId == r.Id)
                    .Join(
                        _dbContext.Permisos.AsNoTracking(),
                        rp => rp.PermisoId,
                        p => p.Id,
                        (rp, p) => new PermisoSummary(
                            p.Id,
                            p.Codigo,
                            p.Nombre,
                            p.Descripcion,
                            p.Estado))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<RolAccessSummary?> GetRolByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RolAccessSummary(
                r.Id,
                r.Nombre,
                r.Descripcion,
                r.Estado,
                _dbContext.RolPermisos
                    .AsNoTracking()
                    .Where(rp => rp.RolId == r.Id)
                    .Join(
                        _dbContext.Permisos.AsNoTracking(),
                        rp => rp.PermisoId,
                        p => p.Id,
                        (rp, p) => new PermisoSummary(
                            p.Id,
                            p.Codigo,
                            p.Nombre,
                            p.Descripcion,
                            p.Estado))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<RolAccessSummary?> CreateRolAsync(
        string nombre,
        string? descripcion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        var normalizedNombre = nombre.Trim();

        var exists = await _dbContext.Roles
            .AsNoTracking()
            .AnyAsync(r => r.Nombre == normalizedNombre, cancellationToken);

        if (exists)
        {
            return null;
        }

        var entity = new Rol
        {
            Nombre = normalizedNombre,
            Descripcion = descripcion,
            Estado = EstadoRegistro.Activo
        };

        _dbContext.Roles.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRolByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<bool> SetPermisosForRolAsync(
        int rolId,
        IEnumerable<int> permisoIds,
        CancellationToken cancellationToken = default)
    {
        if (rolId <= 0)
        {
            return false;
        }

        var requestedPermisos = permisoIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var rol = await _dbContext.Roles
            .SingleOrDefaultAsync(r => r.Id == rolId, cancellationToken);

        if (rol is null)
        {
            return false;
        }

        var validPermisoIds = await _dbContext.Permisos
            .AsNoTracking()
            .Where(p => p.Estado == EstadoRegistro.Activo)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var requestedValidPermisos = requestedPermisos
            .Where(id => validPermisoIds.Contains(id))
            .ToList();

        if (requestedPermisos.Count != requestedValidPermisos.Count)
        {
            return false;
        }

        var currentAssignments = await _dbContext.RolPermisos
            .Where(rp => rp.RolId == rol.Id)
            .ToListAsync(cancellationToken);

        var currentPermisoIds = currentAssignments.Select(rp => rp.PermisoId).ToHashSet();

        foreach (var rolPermiso in currentAssignments.Where(rp => !requestedValidPermisos.Contains(rp.PermisoId)).ToList())
        {
            _dbContext.RolPermisos.Remove(rolPermiso);
        }

        foreach (var permisoId in requestedValidPermisos.Where(id => !currentPermisoIds.Contains(id)))
        {
            _dbContext.RolPermisos.Add(new RolPermiso
            {
                RolId = rol.Id,
                PermisoId = permisoId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetRolEstadoAsync(
        int rolId,
        EstadoRegistro estado,
        CancellationToken cancellationToken = default)
    {
        if (rolId <= 0 || !Enum.IsDefined(typeof(EstadoRegistro), estado))
        {
            return false;
        }

        var rol = await _dbContext.Roles
            .SingleOrDefaultAsync(r => r.Id == rolId, cancellationToken);

        if (rol is null)
        {
            return false;
        }

        rol.Estado = estado;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PermisoSummary>> GetPermisosAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Permisos
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PermisoSummary(
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Descripcion,
                p.Estado))
            .ToListAsync(cancellationToken);
    }

    public async Task<PermisoSummary?> GetPermisoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Permisos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PermisoSummary(
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Descripcion,
                p.Estado))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

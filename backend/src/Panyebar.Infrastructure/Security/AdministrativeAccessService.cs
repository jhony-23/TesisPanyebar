using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Infrastructure.Security;

public sealed class AdministrativeAccessService : IAdministrativeAccessService
{
    private static readonly string[] CriticalAdministrativePermissions =
    {
        AdministrativePermissionCodes.UsuariosGestionar,
        AdministrativePermissionCodes.RolesGestionar,
        AdministrativePermissionCodes.PermisosAsignar
    };

    private readonly PanyebarDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;

    public AdministrativeAccessService(
        PanyebarDbContext dbContext,
        IPasswordHashService passwordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
    }

    public async Task<IReadOnlyList<UsuarioAdministrativoAccessSummary>> GetUsuariosAsync(
        CancellationToken cancellationToken = default)
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
                    .OrderBy(r => r.Id)
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<UsuarioAdministrativoAccessSummary?> GetUsuarioByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

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
                    .OrderBy(r => r.Id)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>> CreateUsuarioAsync(
        string nombreUsuario,
        string password,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = nombreUsuario?.Trim();

        if (actorUsuarioId <= 0 ||
            string.IsNullOrWhiteSpace(normalizedName) ||
            normalizedName.Length > 100 ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 8)
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        if (await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(
                u => u.NombreUsuario == normalizedName,
                cancellationToken))
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Duplicate);
        }

        var entity = new UsuarioAdministrativo
        {
            NombreUsuario = normalizedName,
            PasswordHash = _passwordHashService.Hash(password),
            Estado = EstadoRegistro.Activo
        };

        _dbContext.UsuariosAdministrativos.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "SEGURIDAD.USUARIO.CREAR",
            "UsuarioAdministrativo",
            entity.Id,
            null,
            new
            {
                entity.Id,
                entity.NombreUsuario,
                Estado = entity.Estado.ToString()
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
            .Success((await GetUsuarioByIdAsync(entity.Id, cancellationToken))!);
    }

    public async Task<AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>> SetRolesForUsuarioAsync(
        int usuarioAdministrativoId,
        IEnumerable<int> rolIds,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioAdministrativoId <= 0 ||
            actorUsuarioId <= 0 ||
            rolIds is null)
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var usuario = await _dbContext.UsuariosAdministrativos
            .SingleOrDefaultAsync(
                u => u.Id == usuarioAdministrativoId,
                cancellationToken);

        if (usuario is null)
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.NotFound);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var requested = rolIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var valid = await _dbContext.Roles
            .AsNoTracking()
            .Where(r =>
                requested.Contains(r.Id) &&
                r.Estado == EstadoRegistro.Activo)
            .Select(r => r.Id)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        if (requested.Count != valid.Count)
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var current = await _dbContext.UsuarioRoles
            .Where(x => x.UsuarioAdministrativoId == usuarioAdministrativoId)
            .ToListAsync(cancellationToken);

        var previousIds = current
            .Select(x => x.RolId)
            .OrderBy(id => id)
            .ToArray();

        foreach (var assignment in current.Where(x => !valid.Contains(x.RolId)))
        {
            _dbContext.UsuarioRoles.Remove(assignment);
        }

        var currentIds = current.Select(x => x.RolId).ToHashSet();

        foreach (var rolId in valid.Where(id => !currentIds.Contains(id)))
        {
            _dbContext.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioAdministrativoId = usuarioAdministrativoId,
                RolId = rolId
            });
        }

        if (!await WouldKeepAdministratorAfterUserRolesAsync(
            usuarioAdministrativoId,
            valid,
            cancellationToken))
        {
            foreach (var entry in _dbContext.ChangeTracker
                .Entries<UsuarioRol>()
                .Where(e =>
                    e.Entity.UsuarioAdministrativoId ==
                    usuarioAdministrativoId))
            {
                entry.State = EntityState.Detached;
            }

            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Conflict);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "SEGURIDAD.USUARIO.ROLES",
            "UsuarioAdministrativo",
            usuarioAdministrativoId,
            new { RolIds = previousIds },
            new { RolIds = valid });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
            .Success((await GetUsuarioByIdAsync(
                usuarioAdministrativoId,
                cancellationToken))!);
    }

    public async Task<AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>> SetUsuarioEstadoAsync(
        int usuarioAdministrativoId,
        EstadoRegistro estado,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioAdministrativoId <= 0 ||
            actorUsuarioId <= 0 ||
            !Enum.IsDefined(typeof(EstadoRegistro), estado))
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var usuario = await _dbContext.UsuariosAdministrativos
            .SingleOrDefaultAsync(
                u => u.Id == usuarioAdministrativoId,
                cancellationToken);

        if (usuario is null)
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.NotFound);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        if (usuarioAdministrativoId == actorUsuarioId &&
            estado == EstadoRegistro.Inactivo)
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Conflict);
        }

        var previous = usuario.Estado;

        if (!await WouldKeepAdministratorAfterUserStateAsync(
            usuarioAdministrativoId,
            estado,
            cancellationToken))
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Conflict);
        }

        usuario.Estado = estado;
        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "SEGURIDAD.USUARIO.ESTADO",
            "UsuarioAdministrativo",
            usuarioAdministrativoId,
            new { Estado = previous.ToString() },
            new { Estado = estado.ToString() });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
            .Success((await GetUsuarioByIdAsync(
                usuarioAdministrativoId,
                cancellationToken))!);
    }

    public async Task<AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>> ResetUsuarioPasswordAsync(
        int usuarioAdministrativoId,
        string nuevaPassword,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioAdministrativoId <= 0 ||
            actorUsuarioId <= 0 ||
            string.IsNullOrWhiteSpace(nuevaPassword) ||
            nuevaPassword.Length < 8)
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var usuario = await _dbContext.UsuariosAdministrativos
            .SingleOrDefaultAsync(
                u => u.Id == usuarioAdministrativoId,
                cancellationToken);

        if (usuario is null)
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.NotFound);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        usuario.PasswordHash = _passwordHashService.Hash(nuevaPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "SEGURIDAD.USUARIO.PASSWORD.RESTABLECER",
            "UsuarioAdministrativo",
            usuarioAdministrativoId,
            null,
            new { Restablecida = true });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministrativeAccessResult<UsuarioAdministrativoAccessSummary>
            .Success((await GetUsuarioByIdAsync(
                usuarioAdministrativoId,
                cancellationToken))!);
    }

    public async Task<IReadOnlyList<RolAccessSummary>> GetRolesAsync(
        CancellationToken cancellationToken = default)
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
                    .OrderBy(p => p.Id)
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<RolAccessSummary?> GetRolByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

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
                    .OrderBy(p => p.Id)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AdministrativeAccessResult<RolAccessSummary>> CreateRolAsync(
        string nombre,
        string? descripcion,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = nombre?.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(descripcion)
            ? null
            : descripcion.Trim();

        if (actorUsuarioId <= 0 ||
            string.IsNullOrWhiteSpace(normalizedName) ||
            normalizedName.Length > 100 ||
            normalizedDescription?.Length > 500)
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        if (await _dbContext.Roles
            .AsNoTracking()
            .AnyAsync(r => r.Nombre == normalizedName, cancellationToken))
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Duplicate);
        }

        var entity = new Rol
        {
            Nombre = normalizedName,
            Descripcion = normalizedDescription,
            Estado = EstadoRegistro.Activo
        };

        _dbContext.Roles.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "SEGURIDAD.ROL.CREAR",
            "Rol",
            entity.Id,
            null,
            new
            {
                entity.Id,
                entity.Nombre,
                entity.Descripcion,
                Estado = entity.Estado.ToString()
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministrativeAccessResult<RolAccessSummary>
            .Success((await GetRolByIdAsync(entity.Id, cancellationToken))!);
    }

    public async Task<AdministrativeAccessResult<RolAccessSummary>> SetPermisosForRolAsync(
        int rolId,
        IEnumerable<int> permisoIds,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        if (rolId <= 0 ||
            actorUsuarioId <= 0 ||
            permisoIds is null)
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var rol = await _dbContext.Roles
            .SingleOrDefaultAsync(r => r.Id == rolId, cancellationToken);

        if (rol is null)
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.NotFound);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var requested = permisoIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var valid = await _dbContext.Permisos
            .AsNoTracking()
            .Where(p =>
                requested.Contains(p.Id) &&
                p.Estado == EstadoRegistro.Activo)
            .Select(p => p.Id)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        if (requested.Count != valid.Count)
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var current = await _dbContext.RolPermisos
            .Where(x => x.RolId == rolId)
            .ToListAsync(cancellationToken);

        var previousIds = current
            .Select(x => x.PermisoId)
            .OrderBy(id => id)
            .ToArray();

        foreach (var assignment in current.Where(x => !valid.Contains(x.PermisoId)))
        {
            _dbContext.RolPermisos.Remove(assignment);
        }

        var currentIds = current.Select(x => x.PermisoId).ToHashSet();

        foreach (var permisoId in valid.Where(id => !currentIds.Contains(id)))
        {
            _dbContext.RolPermisos.Add(new RolPermiso
            {
                RolId = rolId,
                PermisoId = permisoId
            });
        }

        if (!await WouldKeepAdministratorAfterRolePermissionsAsync(
            rolId,
            valid,
            cancellationToken))
        {
            foreach (var entry in _dbContext.ChangeTracker
                .Entries<RolPermiso>()
                .Where(e => e.Entity.RolId == rolId))
            {
                entry.State = EntityState.Detached;
            }

            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Conflict);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "SEGURIDAD.ROL.PERMISOS",
            "Rol",
            rolId,
            new { PermisoIds = previousIds },
            new { PermisoIds = valid });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministrativeAccessResult<RolAccessSummary>
            .Success((await GetRolByIdAsync(rolId, cancellationToken))!);
    }

    public async Task<AdministrativeAccessResult<RolAccessSummary>> SetRolEstadoAsync(
        int rolId,
        EstadoRegistro estado,
        int actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        if (rolId <= 0 ||
            actorUsuarioId <= 0 ||
            !Enum.IsDefined(typeof(EstadoRegistro), estado))
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var rol = await _dbContext.Roles
            .SingleOrDefaultAsync(r => r.Id == rolId, cancellationToken);

        if (rol is null)
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.NotFound);
        }

        if (!await ActorExistsAsync(actorUsuarioId, cancellationToken))
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Invalid);
        }

        var previous = rol.Estado;

        if (!await WouldKeepAdministratorAfterRoleStateAsync(
            rolId,
            estado,
            cancellationToken))
        {
            return AdministrativeAccessResult<RolAccessSummary>
                .Failure(AdministrativeAccessError.Conflict);
        }

        rol.Estado = estado;
        await _dbContext.SaveChangesAsync(cancellationToken);

        AddAudit(
            actorUsuarioId,
            "SEGURIDAD.ROL.ESTADO",
            "Rol",
            rolId,
            new { Estado = previous.ToString() },
            new { Estado = estado.ToString() });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AdministrativeAccessResult<RolAccessSummary>
            .Success((await GetRolByIdAsync(rolId, cancellationToken))!);
    }

    public async Task<IReadOnlyList<PermisoSummary>> GetPermisosAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Permisos
            .AsNoTracking()
            .OrderBy(p => p.Codigo)
            .Select(p => new PermisoSummary(
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Descripcion,
                p.Estado))
            .ToListAsync(cancellationToken);
    }

    public async Task<PermisoSummary?> GetPermisoByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

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

    private async Task<bool> ActorExistsAsync(
        int actorUsuarioId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.Id == actorUsuarioId &&
                    u.Estado == EstadoRegistro.Activo,
                cancellationToken);
    }

    private async Task<bool> WouldKeepAdministratorAfterUserRolesAsync(
        int targetUserId,
        IReadOnlyCollection<int> futureRoleIds,
        CancellationToken cancellationToken)
    {
        return await HasFunctionalAdministratorAsync(
            targetUserId,
            futureRoleIds,
            null,
            null,
            null,
            null,
            cancellationToken);
    }

    private async Task<bool> WouldKeepAdministratorAfterUserStateAsync(
        int targetUserId,
        EstadoRegistro futureState,
        CancellationToken cancellationToken)
    {
        return await HasFunctionalAdministratorAsync(
            targetUserId,
            null,
            futureState,
            null,
            null,
            null,
            cancellationToken);
    }

    private async Task<bool> WouldKeepAdministratorAfterRolePermissionsAsync(
        int targetRoleId,
        IReadOnlyCollection<int> futurePermissionIds,
        CancellationToken cancellationToken)
    {
        return await HasFunctionalAdministratorAsync(
            null,
            null,
            null,
            targetRoleId,
            futurePermissionIds,
            null,
            cancellationToken);
    }

    private async Task<bool> WouldKeepAdministratorAfterRoleStateAsync(
        int targetRoleId,
        EstadoRegistro futureState,
        CancellationToken cancellationToken)
    {
        return await HasFunctionalAdministratorAsync(
            null,
            null,
            null,
            targetRoleId,
            null,
            futureState,
            cancellationToken);
    }

    private async Task<bool> HasFunctionalAdministratorAsync(
        int? overriddenUserId,
        IReadOnlyCollection<int>? overriddenUserRoleIds,
        EstadoRegistro? overriddenUserState,
        int? overriddenRoleId,
        IReadOnlyCollection<int>? overriddenRolePermissionIds,
        EstadoRegistro? overriddenRoleState,
        CancellationToken cancellationToken)
    {
        var criticalPermissions = await _dbContext.Permisos
            .AsNoTracking()
            .Where(p =>
                p.Estado == EstadoRegistro.Activo &&
                CriticalAdministrativePermissions.Contains(p.Codigo))
            .Select(p => new
            {
                p.Id,
                p.Codigo
            })
            .ToListAsync(cancellationToken);

        if (criticalPermissions
            .Select(p => p.Codigo)
            .Distinct()
            .Count() != CriticalAdministrativePermissions.Length)
        {
            return false;
        }

        var criticalIds = criticalPermissions
            .Select(p => p.Id)
            .ToHashSet();

        var users = await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.Estado
            })
            .ToListAsync(cancellationToken);

        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Select(r => new
            {
                r.Id,
                r.Estado
            })
            .ToListAsync(cancellationToken);

        var userRoles = await _dbContext.UsuarioRoles
            .AsNoTracking()
            .Select(ur => new
            {
                ur.UsuarioAdministrativoId,
                ur.RolId
            })
            .ToListAsync(cancellationToken);

        var rolePermissions = await _dbContext.RolPermisos
            .AsNoTracking()
            .Select(rp => new
            {
                rp.RolId,
                rp.PermisoId
            })
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            var userState =
                overriddenUserId == user.Id &&
                overriddenUserState.HasValue
                    ? overriddenUserState.Value
                    : user.Estado;

            if (userState != EstadoRegistro.Activo)
            {
                continue;
            }

            IReadOnlyCollection<int> roleIds;

            if (overriddenUserId == user.Id &&
                overriddenUserRoleIds is not null)
            {
                roleIds = overriddenUserRoleIds;
            }
            else
            {
                roleIds = userRoles
                    .Where(x =>
                        x.UsuarioAdministrativoId == user.Id)
                    .Select(x => x.RolId)
                    .Distinct()
                    .ToArray();
            }

            var grantedCriticalIds = new HashSet<int>();

            foreach (var roleId in roleIds)
            {
                var persistedRole = roles
                    .SingleOrDefault(r => r.Id == roleId);

                if (persistedRole is null)
                {
                    continue;
                }

                var roleState =
                    overriddenRoleId == roleId &&
                    overriddenRoleState.HasValue
                        ? overriddenRoleState.Value
                        : persistedRole.Estado;

                if (roleState != EstadoRegistro.Activo)
                {
                    continue;
                }

                IReadOnlyCollection<int> permissionIds;

                if (overriddenRoleId == roleId &&
                    overriddenRolePermissionIds is not null)
                {
                    permissionIds =
                        overriddenRolePermissionIds;
                }
                else
                {
                    permissionIds = rolePermissions
                        .Where(x => x.RolId == roleId)
                        .Select(x => x.PermisoId)
                        .Distinct()
                        .ToArray();
                }

                foreach (var permissionId in permissionIds)
                {
                    if (criticalIds.Contains(permissionId))
                    {
                        grantedCriticalIds.Add(permissionId);
                    }
                }
            }

            if (criticalIds.All(grantedCriticalIds.Contains))
            {
                return true;
            }
        }

        return false;
    }
    private void AddAudit(
        int actorUsuarioId,
        string action,
        string entity,
        int entityId,
        object? previous,
        object? current)
    {
        _dbContext.Auditorias.Add(new Auditoria
        {
            UsuarioAdministrativoId = actorUsuarioId,
            Accion = action,
            Entidad = entity,
            EntidadId = entityId,
            Fecha = DateTime.UtcNow,
            ValorAnterior = previous is null
                ? null
                : JsonSerializer.Serialize(previous),
            ValorNuevo = current is null
                ? null
                : JsonSerializer.Serialize(current)
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Infrastructure.Security;

public sealed class InitialAdministratorProvisioner : IInitialAdministratorProvisioner
{
    private static readonly (string Code, string Name)[] RequiredPermissions =
    {
        (AdministrativePermissionCodes.UsuariosVer, "Ver usuarios"),
        (AdministrativePermissionCodes.UsuariosGestionar, "Gestionar usuarios"),
        (AdministrativePermissionCodes.RolesVer, "Ver roles"),
        (AdministrativePermissionCodes.RolesGestionar, "Gestionar roles"),
        (AdministrativePermissionCodes.PermisosVer, "Ver permisos"),
        (AdministrativePermissionCodes.PermisosAsignar, "Asignar permisos")
    };

    private readonly PanyebarDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;

    public InitialAdministratorProvisioner(
        PanyebarDbContext dbContext,
        IPasswordHashService passwordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
    }

    public async Task<InitialAdministratorProvisioningResult> ProvisionAsync(
        string nombreUsuario,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = nombreUsuario?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName) ||
            normalizedName.Length > 100 ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 8)
        {
            return new(false, "El usuario y la contraseña no cumplen los requisitos mínimos.");
        }

        if (await _dbContext.UsuariosAdministrativos.AnyAsync(cancellationToken))
        {
            return new(false, "El aprovisionamiento rechazado: ya existe una cuenta administrativa.");
        }

        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var permissions = new List<Permiso>();
        foreach (var requiredPermission in RequiredPermissions)
        {
            var permission = await _dbContext.Permisos
                .SingleOrDefaultAsync(p => p.Codigo == requiredPermission.Code, cancellationToken);

            if (permission is null)
            {
                permission = new Permiso
                {
                    Codigo = requiredPermission.Code,
                    Nombre = requiredPermission.Name,
                    Estado = EstadoRegistro.Activo
                };
                _dbContext.Permisos.Add(permission);
            }
            else if (permission.Estado != EstadoRegistro.Activo)
            {
                return new(false, $"El permiso requerido '{requiredPermission.Code}' no está activo.");
            }

            permissions.Add(permission);
        }

        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(r => r.Nombre == "Administrador inicial", cancellationToken);

        if (role is null)
        {
            role = new Rol
            {
                Nombre = "Administrador inicial",
                Descripcion = "Rol técnico creado durante el aprovisionamiento inicial.",
                Estado = EstadoRegistro.Activo
            };
            _dbContext.Roles.Add(role);
        }
        else if (role.Estado != EstadoRegistro.Activo)
        {
            return new(false, "El rol técnico del administrador inicial no está activo.");
        }

        var user = new UsuarioAdministrativo
        {
            NombreUsuario = normalizedName,
            PasswordHash = _passwordHashService.Hash(password),
            Estado = EstadoRegistro.Activo
        };
        _dbContext.UsuariosAdministrativos.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            if (!await _dbContext.RolPermisos.AnyAsync(
                    rp => rp.RolId == role.Id && rp.PermisoId == permission.Id,
                    cancellationToken))
            {
                _dbContext.RolPermisos.Add(new RolPermiso
                {
                    RolId = role.Id,
                    PermisoId = permission.Id
                });
            }
        }

        _dbContext.UsuarioRoles.Add(new UsuarioRol
        {
            UsuarioAdministrativoId = user.Id,
            RolId = role.Id
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new(true, $"Administrador inicial '{user.NombreUsuario}' aprovisionado.", user.Id);
    }
}
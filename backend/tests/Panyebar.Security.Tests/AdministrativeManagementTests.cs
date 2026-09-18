using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public sealed class AdministrativeManagementTests
{
    [Fact]
    public async Task CreateUser_hashes_password_and_audits_without_secret()
    {
        await using var db = CreateContext();
        var actor = await SeedFunctionalAdministrator(db);
        var service = CreateService(db);

        var result = await service.CreateUsuarioAsync(
            "nuevo.admin",
            "Password123!",
            actor.Id);

        Assert.True(result.Succeeded);

        var persisted = await db.UsuariosAdministrativos
            .SingleAsync(u => u.NombreUsuario == "nuevo.admin");

        Assert.NotEqual("Password123!", persisted.PasswordHash);
        Assert.True(new PasswordHasherAdapter()
            .Verify(persisted.PasswordHash, "Password123!"));

        var audit = await db.Auditorias
            .SingleAsync(a => a.Accion == "SEGURIDAD.USUARIO.CREAR");

        Assert.DoesNotContain("Password123!", audit.ValorNuevo ?? "");
        Assert.DoesNotContain(persisted.PasswordHash, audit.ValorNuevo ?? "");
    }

    [Fact]
    public async Task ResetPassword_replaces_hash_and_never_audits_password()
    {
        await using var db = CreateContext();
        var actor = await SeedFunctionalAdministrator(db);

        var target = new UsuarioAdministrativo
        {
            NombreUsuario = "target",
            PasswordHash = new PasswordHasherAdapter().Hash("OldPassword123!"),
            Estado = EstadoRegistro.Activo
        };

        db.UsuariosAdministrativos.Add(target);
        await db.SaveChangesAsync();

        var previousHash = target.PasswordHash;
        var service = CreateService(db);

        var result = await service.ResetUsuarioPasswordAsync(
            target.Id,
            "NewPassword123!",
            actor.Id);

        Assert.True(result.Succeeded);

        await db.Entry(target).ReloadAsync();

        Assert.NotEqual(previousHash, target.PasswordHash);
        Assert.True(new PasswordHasherAdapter()
            .Verify(target.PasswordHash, "NewPassword123!"));

        var audit = await db.Auditorias
            .SingleAsync(a =>
                a.Accion == "SEGURIDAD.USUARIO.PASSWORD.RESTABLECER");

        Assert.DoesNotContain("NewPassword123!", audit.ValorNuevo ?? "");
        Assert.DoesNotContain(target.PasswordHash, audit.ValorNuevo ?? "");
    }

    [Fact]
    public async Task User_cannot_deactivate_self()
    {
        await using var db = CreateContext();
        var actor = await SeedFunctionalAdministrator(db);
        var service = CreateService(db);

        var result = await service.SetUsuarioEstadoAsync(
            actor.Id,
            EstadoRegistro.Inactivo,
            actor.Id);

        Assert.Equal(AdministrativeAccessError.Conflict, result.Error);

        Assert.Equal(
            EstadoRegistro.Activo,
            (await db.UsuariosAdministrativos.FindAsync(actor.Id))!.Estado);
    }

    [Fact]
    public async Task Last_functional_administrator_cannot_lose_roles()
    {
        await using var db = CreateContext();
        var actor = await SeedFunctionalAdministrator(db);
        var service = CreateService(db);

        var result = await service.SetRolesForUsuarioAsync(
            actor.Id,
            Array.Empty<int>(),
            actor.Id);

        Assert.Equal(AdministrativeAccessError.Conflict, result.Error);

        Assert.NotEmpty(await db.UsuarioRoles
            .Where(x => x.UsuarioAdministrativoId == actor.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task Last_functional_administrator_role_cannot_lose_critical_permissions()
    {
        await using var db = CreateContext();
        var actor = await SeedFunctionalAdministrator(db);

        var roleId = await db.UsuarioRoles
            .Where(x => x.UsuarioAdministrativoId == actor.Id)
            .Select(x => x.RolId)
            .SingleAsync();

        var service = CreateService(db);

        var result = await service.SetPermisosForRolAsync(
            roleId,
            Array.Empty<int>(),
            actor.Id);

        Assert.Equal(AdministrativeAccessError.Conflict, result.Error);

        Assert.Equal(
            3,
            await db.RolPermisos.CountAsync(x => x.RolId == roleId));
    }

    [Fact]
    public async Task Last_functional_administrator_role_cannot_be_deactivated()
    {
        await using var db = CreateContext();
        var actor = await SeedFunctionalAdministrator(db);

        var roleId = await db.UsuarioRoles
            .Where(x => x.UsuarioAdministrativoId == actor.Id)
            .Select(x => x.RolId)
            .SingleAsync();

        var service = CreateService(db);

        var result = await service.SetRolEstadoAsync(
            roleId,
            EstadoRegistro.Inactivo,
            actor.Id);

        Assert.Equal(AdministrativeAccessError.Conflict, result.Error);

        Assert.Equal(
            EstadoRegistro.Activo,
            (await db.Roles.FindAsync(roleId))!.Estado);
    }

    [Fact]
    public async Task User_can_lose_roles_when_another_functional_admin_remains()
    {
        await using var db = CreateContext();

        var first = await SeedFunctionalAdministrator(db);
        var second = new UsuarioAdministrativo
        {
            NombreUsuario = "second.admin",
            PasswordHash = new PasswordHasherAdapter().Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        db.UsuariosAdministrativos.Add(second);
        await db.SaveChangesAsync();

        var roleId = await db.UsuarioRoles
            .Where(x => x.UsuarioAdministrativoId == first.Id)
            .Select(x => x.RolId)
            .SingleAsync();

        db.UsuarioRoles.Add(new UsuarioRol
        {
            UsuarioAdministrativoId = second.Id,
            RolId = roleId
        });

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.SetRolesForUsuarioAsync(
            second.Id,
            Array.Empty<int>(),
            first.Id);

        Assert.True(result.Succeeded);

        Assert.Empty(await db.UsuarioRoles
            .Where(x => x.UsuarioAdministrativoId == second.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task Role_assignment_is_audited()
    {
        await using var db = CreateContext();
        var actor = await SeedFunctionalAdministrator(db);

        var extraRole = new Rol
        {
            Nombre = "Operador",
            Estado = EstadoRegistro.Activo
        };

        db.Roles.Add(extraRole);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.SetRolesForUsuarioAsync(
            actor.Id,
            new[]
            {
                (await db.UsuarioRoles
                    .Where(x => x.UsuarioAdministrativoId == actor.Id)
                    .Select(x => x.RolId)
                    .SingleAsync()),
                extraRole.Id
            },
            actor.Id);

        Assert.True(result.Succeeded);

        Assert.Contains(
            await db.Auditorias.ToListAsync(),
            a => a.Accion == "SEGURIDAD.USUARIO.ROLES");
    }

    private static AdministrativeAccessService CreateService(
        PanyebarDbContext db) =>
        new(db, new PasswordHasherAdapter());

    private static async Task<UsuarioAdministrativo> SeedFunctionalAdministrator(
        PanyebarDbContext db)
    {
        var user = new UsuarioAdministrativo
        {
            NombreUsuario = "demo.admin",
            PasswordHash = new PasswordHasherAdapter().Hash("Password123!"),
            Estado = EstadoRegistro.Activo
        };

        var role = new Rol
        {
            Nombre = "Administrador",
            Estado = EstadoRegistro.Activo
        };

        var permissions = new[]
        {
            new Permiso
            {
                Codigo = AdministrativePermissionCodes.UsuariosGestionar,
                Nombre = "Usuarios gestionar",
                Estado = EstadoRegistro.Activo
            },
            new Permiso
            {
                Codigo = AdministrativePermissionCodes.RolesGestionar,
                Nombre = "Roles gestionar",
                Estado = EstadoRegistro.Activo
            },
            new Permiso
            {
                Codigo = AdministrativePermissionCodes.PermisosAsignar,
                Nombre = "Permisos asignar",
                Estado = EstadoRegistro.Activo
            }
        };

        db.UsuariosAdministrativos.Add(user);
        db.Roles.Add(role);
        db.Permisos.AddRange(permissions);

        await db.SaveChangesAsync();

        db.UsuarioRoles.Add(new UsuarioRol
        {
            UsuarioAdministrativoId = user.Id,
            RolId = role.Id
        });

        foreach (var permission in permissions)
        {
            db.RolPermisos.Add(new RolPermiso
            {
                RolId = role.Id,
                PermisoId = permission.Id
            });
        }

        await db.SaveChangesAsync();

        return user;
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PanyebarDbContext(options);
    }
}

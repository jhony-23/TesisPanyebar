using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public sealed class InitialAdministratorProvisioningTests
{
    [Fact]
    public async Task Provision_creates_hashed_initial_admin_with_only_critical_permissions()
    {
        await using var db = CreateContext();
        var service = new InitialAdministratorProvisioner(db, new PasswordHasherAdapter());

        var result = await service.ProvisionAsync("operador.inicial", "UnaPasswordSegura!23");

        Assert.True(result.Succeeded);
        var user = await db.UsuariosAdministrativos.SingleAsync();
        Assert.NotEqual("UnaPasswordSegura!23", user.PasswordHash);
        Assert.True(new PasswordHasherAdapter().Verify(user.PasswordHash, "UnaPasswordSegura!23"));

        var assignedCodes = await (
            from userRole in db.UsuarioRoles
            join rolePermission in db.RolPermisos on userRole.RolId equals rolePermission.RolId
            join permission in db.Permisos on rolePermission.PermisoId equals permission.Id
            where userRole.UsuarioAdministrativoId == user.Id
            select permission.Codigo).ToListAsync();

        Assert.Equal(
            new[]
            {
                AdministrativePermissionCodes.PermisosAsignar,
                AdministrativePermissionCodes.PermisosVer,
                AdministrativePermissionCodes.RolesGestionar,
                AdministrativePermissionCodes.RolesVer,
                AdministrativePermissionCodes.UsuariosGestionar,
                AdministrativePermissionCodes.UsuariosVer
            },
            assignedCodes.OrderBy(code => code));
    }

    [Fact]
    public async Task Provision_rejects_second_execution_without_replacing_initial_account()
    {
        await using var db = CreateContext();
        var service = new InitialAdministratorProvisioner(db, new PasswordHasherAdapter());

        var first = await service.ProvisionAsync("primer.admin", "UnaPasswordSegura!23");
        var originalHash = await db.UsuariosAdministrativos.Select(user => user.PasswordHash).SingleAsync();
        var second = await service.ProvisionAsync("otro.admin", "OtraPasswordSegura!23");

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(originalHash, await db.UsuariosAdministrativos.Select(user => user.PasswordHash).SingleAsync());
        Assert.Equal(1, await db.UsuariosAdministrativos.CountAsync());
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PanyebarDbContext(options);
    }
}
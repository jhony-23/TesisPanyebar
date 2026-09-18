using Microsoft.AspNetCore.Authorization;
using Panyebar.Api.Controllers;
using Panyebar.Application.Security;

namespace Panyebar.Security.Tests;

public sealed class AbastecimientoControllerSecurityTests
{
    [Theory]
    [InlineData(
        nameof(AbastecimientoController.GetAll),
        AdministrativePermissionCodes.AbastecimientoVer)]
    [InlineData(
        nameof(AbastecimientoController.GetById),
        AdministrativePermissionCodes.AbastecimientoVer)]
    [InlineData(
        nameof(AbastecimientoController.Create),
        AdministrativePermissionCodes.AbastecimientoGestionar)]
    [InlineData(
        nameof(AbastecimientoController.CreateRecurring),
        AdministrativePermissionCodes.AbastecimientoGestionar)]
    [InlineData(
        nameof(AbastecimientoController.Update),
        AdministrativePermissionCodes.AbastecimientoGestionar)]
    [InlineData(
        nameof(AbastecimientoController.Complete),
        AdministrativePermissionCodes.AbastecimientoGestionar)]
    [InlineData(
        nameof(AbastecimientoController.Cancel),
        AdministrativePermissionCodes.AbastecimientoGestionar)]
    public void Actions_require_expected_permission(
        string methodName,
        string permission)
    {
        var method = typeof(AbastecimientoController)
            .GetMethod(methodName);

        Assert.NotNull(method);

        var authorize = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            $"Permission:{permission}",
            authorize.Policy);
    }

    [Fact]
    public void Permission_codes_are_stable()
    {
        Assert.Equal(
            "ABASTECIMIENTO.VER",
            AdministrativePermissionCodes.AbastecimientoVer);

        Assert.Equal(
            "ABASTECIMIENTO.GESTIONAR",
            AdministrativePermissionCodes.AbastecimientoGestionar);
    }
}

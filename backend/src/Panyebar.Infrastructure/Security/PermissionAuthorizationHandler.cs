using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Panyebar.Application.Security;

namespace Panyebar.Infrastructure.Security;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUsuarioPermissionRepository _permissionRepository;

    public PermissionAuthorizationHandler(IUsuarioPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out var usuarioAdministrativoId))
        {
            return;
        }

        var hasPermission = await _permissionRepository.HasPermissionAsync(
            usuarioAdministrativoId,
            requirement.PermisoCodigo,
            CancellationToken.None);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}

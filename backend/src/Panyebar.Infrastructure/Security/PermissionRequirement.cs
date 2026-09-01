using Microsoft.AspNetCore.Authorization;

namespace Panyebar.Infrastructure.Security;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permisoCodigo)
    {
        if (string.IsNullOrWhiteSpace(permisoCodigo))
        {
            throw new ArgumentException("El código de permiso es requerido.", nameof(permisoCodigo));
        }

        PermisoCodigo = permisoCodigo;
    }

    public string PermisoCodigo { get; }
}

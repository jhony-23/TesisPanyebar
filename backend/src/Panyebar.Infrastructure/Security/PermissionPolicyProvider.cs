using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Panyebar.Infrastructure.Security;

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            var permissionCode = policyName["Permission:".Length..];

            if (string.IsNullOrWhiteSpace(permissionCode))
            {
                return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = new AuthorizationPolicyBuilder();
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(permissionCode));

            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EcommerceLaptop.API.Authorization;

/// <summary>
/// Dynamic authorization policy provider for admin permissions.
/// This allows creating policies at runtime for admin permission requirements.
/// </summary>
public class AdminPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly IAuthorizationPolicyProvider _fallbackPolicyProvider;

    public AdminPermissionPolicyProvider(IOptionsMonitor<AuthorizationOptions> options)
    {
        // Fallback to the default policy provider for other policies
        // Convert IOptionsMonitor to IOptions for DefaultAuthorizationPolicyProvider
        var optionsValue = Microsoft.Extensions.Options.Options.Create(options.CurrentValue);
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(optionsValue);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if this is an admin permission policy
        if (policyName.StartsWith("AdminPermission:", StringComparison.OrdinalIgnoreCase) ||
            policyName.StartsWith("RequirePermission:", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the permission from the policy name
            // Format: "AdminPermission:permissions:read" -> "permissions:read"
            // Format: "RequirePermission:roles:read" -> "roles:read"
            var prefixLength = policyName.StartsWith("AdminPermission:", StringComparison.OrdinalIgnoreCase) 
                ? "AdminPermission:".Length 
                : "RequirePermission:".Length;
            var permission = policyName.Substring(prefixLength);

            // Create a dynamic policy with the AdminPermissionRequirement
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new AdminPermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Fall back to the default policy provider for other policies
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }
}
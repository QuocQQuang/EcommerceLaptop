using Microsoft.AspNetCore.Authorization;

namespace EcommerceLaptop.API.Authorization;

/// <summary>
/// Authorization requirement for admin permissions
/// </summary>
public class AdminPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public AdminPermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EcommerceLaptop.API.Authorization;

/// <summary>
/// Attribute to require admin authentication
/// </summary>
public class RequireAdminAttribute : AuthorizeAttribute
{
    public RequireAdminAttribute()
    {
        Policy = "AdminOnly";
    }
}

/// <summary>
/// Attribute to require specific admin permission
/// </summary>
public class RequireAdminPermissionAttribute : AuthorizeAttribute
{
    public RequireAdminPermissionAttribute(string permission)
    {
        Policy = $"AdminPermission:{permission}";
    }
}
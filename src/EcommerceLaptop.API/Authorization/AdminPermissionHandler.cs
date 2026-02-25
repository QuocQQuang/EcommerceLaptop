using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EcommerceLaptop.API.Authorization;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Authorization;

public class AdminPermissionHandler : AuthorizationHandler<AdminPermissionRequirement>
{
    private readonly ILogger<AdminPermissionHandler> _logger;

    public AdminPermissionHandler(ILogger<AdminPermissionHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminPermissionRequirement requirement)
    {
        _logger.LogInformation(" AUTHORIZATION CHECK - Required Permission: {Permission} | User: {User}",
            requirement.Permission, context.User.Identity?.Name ?? "Unknown");

        // Check if user is authenticated
        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning(" AUTHORIZATION FAILED - User not authenticated");
            context.Fail();
            return Task.CompletedTask;
        }

        _logger.LogInformation(" USER AUTHENTICATED - Identity: {Identity} | IsAdmin: {IsAdmin}",
            context.User.Identity.Name, context.User.Identity.AuthenticationType);

        // Log all user claims for debugging
        _logger.LogInformation(" USER CLAIMS DEBUG - Total Claims: {ClaimCount}", context.User.Claims.Count());

        // Check for admin claim - check both standard and custom claims
        // Check for admin claim - check both standard and custom claims
        var isAdmin = context.User.HasClaim(c => c.Type == "is_admin" && c.Value == "true") ||
                      context.User.IsInRole("Admin") ||
                      context.User.IsInRole("SystemAdmin") ||
                      context.User.IsInRole("SuperAdmin");

        _logger.LogInformation(" ADMIN CHECK - IsAdmin: {IsAdmin}", isAdmin);

        if (!isAdmin)
        {
            _logger.LogWarning(" AUTHORIZATION FAILED - User is not admin");
            context.Fail();
            return Task.CompletedTask;
        }

        // Extract permissions from JWT claims - check multiple claim types
        var permissions = context.User.Claims
            .Where(c => c.Type == "permission" ||
                       c.Type == "permissions" ||
                       c.Type == "admin_permission" ||
                       c.Type == "admin_permissions")
            .Select(c => c.Value)
            .ToList();

        _logger.LogInformation(" PERMISSIONS EXTRACTED - Count: {PermissionCount} | Permissions: [{Permissions}]",
            permissions.Count, string.Join(", ", permissions));

        _logger.LogInformation(" PERMISSION CHECK - Required: {Required} | Available: [{Available}]",
            requirement.Permission, string.Join(", ", permissions));

        // Check if user has required permission or super admin privileges
        var hasPermission = permissions.Any(p => p == requirement.Permission) ||
                           permissions.Contains("system:super-admin") ||
                           permissions.Contains("admin:*") ||
                           permissions.Contains("*");

        _logger.LogInformation(" PERMISSION RESULT - HasPermission: {HasPermission} | Required: {Required}",
            hasPermission, requirement.Permission);

        if (hasPermission)
        {
            _logger.LogInformation(" AUTHORIZATION SUCCESS - Permission granted: {Permission}", requirement.Permission);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(" AUTHORIZATION FAILED - Permission denied: {Permission} | Available: [{Available}]",
                requirement.Permission, string.Join(", ", permissions));
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
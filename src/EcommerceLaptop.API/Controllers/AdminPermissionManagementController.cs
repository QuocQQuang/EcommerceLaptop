using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.API.Authorization;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Dynamic permission management controller for runtime permission configuration
/// Replaces hardcoded permissions with database-driven dynamic system
/// </summary>
[ApiController]
[Route("api/admin/permissions")]
[Authorize]
public class AdminPermissionManagementController(IAuthService authService, ILogger<AdminPermissionManagementController> logger) : BaseApiController(logger)
{
    private readonly IAuthService _authService = authService;

    /// <summary>
    /// Extract admin user ID from JWT token claims
    /// </summary>
    /// <returns>Admin user ID or null</returns>
    private int? GetCurrentAdminUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            // Verify this is an admin token - check multiple ways
            var isAdmin = User.IsInRole("Admin") ||
                         User.IsInRole("SystemAdmin") ||
                         User.IsInRole("ProductAdmin") ||
                         User.IsInRole("SalesAdmin") ||
                         User.IsInRole("PromotionManager") ||
                         User.IsInRole("SuperAdmin") ||
                         User.HasClaim(c => c.Type == "user_type" && c.Value == "Admin") ||
                         User.HasClaim(c => c.Type == "is_admin" && c.Value == "true");

            if (isAdmin)
            {
                return userId;
            }
        }
        return null;
    }

    /// <summary>
    /// Get all available permissions in the system
    /// Used by frontend to load dynamic permissions instead of hardcoded constants
    /// </summary>
    [HttpGet]
    [RequireAdminPermission("security:read")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _authService.GetAllPermissionsAsync();
        return Ok(new { data = permissions, message = "Permissions retrieved successfully" });
    }

    /// <summary>
    /// Get all available permissions in the system (alternative route for frontend compatibility)
    /// Used by frontend to load dynamic permissions instead of hardcoded constants
    /// </summary>
    [HttpGet("all")]
    [RequireAdminPermission("security:read")]
    public async Task<IActionResult> GetAllPermissionsAlternate()
    {
        var permissions = await _authService.GetAllPermissionsAsync();
        return Ok(new { data = permissions, message = "Permissions retrieved successfully" });
    }

    /// <summary>
    /// Get permissions grouped by module for better organization
    /// </summary>
    [HttpGet("grouped")]
    [RequireAdminPermission("security:read")]
    public async Task<IActionResult> GetPermissionsGrouped()
    {
        var permissions = await _authService.GetAllPermissionsAsync();
        var grouped = permissions.GroupBy(p => p.Name.Split(':')[0])
                               .ToDictionary(g => g.Key, g => g.AsEnumerable());

        return Ok(new { data = grouped, message = "Grouped permissions retrieved successfully" });
    }

    /// <summary>
    /// Get current user's permissions (real-time)
    /// Used by frontend for dynamic permission checking
    /// </summary>
    [HttpGet("my-permissions")]
    [RequireAdminPermission("security:read")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var adminUserId = GetCurrentAdminUserId();

        if (adminUserId == null)
        {
            _logger.LogWarning(" MY-PERMISSIONS - Unable to identify admin user");
            return Unauthorized(new { error = "Unable to identify admin user" });
        }

        var permissions = await _authService.GetUserPermissionsAsync(adminUserId.Value);

        return Ok(new { data = permissions, message = "User permissions retrieved successfully" });
    }

    /// <summary>
    /// Check if current user has specific permission (real-time check)
    /// </summary>
    [HttpGet("check/{permission}")]
    public async Task<IActionResult> CheckPermission(string permission)
    {
        var adminUserId = GetCurrentAdminUserId();
        if (adminUserId == null)
        {
            return Unauthorized(new { error = "Unable to identify admin user" });
        }

        var hasPermission = await _authService.HasPermissionAsync(adminUserId.Value, permission);

        return Ok(new { data = hasPermission, message = hasPermission ? "Permission granted" : "Permission denied" });
    }

    /// <summary>
    /// Get role-permission mappings for all roles
    /// Used to replace hardcoded ROLE_PERMISSIONS constant
    /// </summary>
    [HttpGet("role-mappings")]
    [RequireAdminPermission("roles:read")]
    public async Task<IActionResult> GetRolePermissionMappings()
    {
        var mappings = await _authService.GetRolePermissionMappingsAsync();
        return Ok(new { data = mappings, message = "Role permission mappings retrieved successfully" });
    }

    /// <summary>
    /// Sync permissions between database and application
    /// Ensures frontend and backend have consistent permission data
    /// </summary>
    [HttpPost("sync")]
    [RequireAdminPermission("security:manage")]
    public async Task<IActionResult> SyncPermissions()
    {
        await _authService.SyncPermissionsAsync();
        return Ok(new { data = new { synced = true }, message = "Permissions synchronized successfully" });
    }

    /// <summary>
    /// Get permission validation schema for frontend
    /// Returns permission format, validation rules, and constraints
    /// </summary>
    [HttpGet("schema")]
    [RequireAdminPermission("security:read")]
    public IActionResult GetPermissionSchema()
    {
        var schema = new
        {
            Format = "module:action",
            ValidModules = new[] { "dashboard", "users", "roles", "products", "orders", "promotions", "settings", "logs", "security" },
            ValidActions = new[] { "read", "write", "delete", "manage", "view" },
            Examples = new[] { "users:read", "products:write", "settings:manage" },
            SuperAdminPermissions = new[] { "system:super-admin", "admin:*", "*" },
            Constraints = new
            {
                MaxLength = 50,
                Pattern = @"^[a-z]+:[a-z\*]+$",
                CaseSensitive = true
            }
        };

        return Ok(new { data = schema, message = "Permission schema retrieved successfully" });
    }
}
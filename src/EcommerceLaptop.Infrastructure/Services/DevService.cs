using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services;

public class DevService : IDevService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DevService> _logger;

    public DevService(ApplicationDbContext context, ILogger<DevService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedPermissionsAsync()
    {
        // Logic moved from PermissionSeeder
        var permissions = new[]
        {
            new { Name = "dashboard:read", Description = "View dashboard", Module = "dashboard", Action = "read" },
            new { Name = "users:read", Description = "View users", Module = "users", Action = "read" },
            new { Name = "users:write", Description = "Create and edit users", Module = "users", Action = "write" },
            new { Name = "users:delete", Description = "Delete users", Module = "users", Action = "delete" },
            new { Name = "users:manage", Description = "Full user management", Module = "users", Action = "manage" },
            new { Name = "roles:read", Description = "View roles", Module = "roles", Action = "read" },
            new { Name = "roles:write", Description = "Create and edit roles", Module = "roles", Action = "write" },
            new { Name = "roles:delete", Description = "Delete roles", Module = "roles", Action = "delete" },
            new { Name = "roles:manage", Description = "Full role management", Module = "roles", Action = "manage" },
            new { Name = "products:read", Description = "View products", Module = "products", Action = "read" },
            new { Name = "products:write", Description = "Create and edit products", Module = "products", Action = "write" },
            new { Name = "products:delete", Description = "Delete products", Module = "products", Action = "delete" },
            new { Name = "products:manage", Description = "Full product management", Module = "products", Action = "manage" },
            new { Name = "orders:read", Description = "View orders", Module = "orders", Action = "read" },
            new { Name = "orders:write", Description = "Create and edit orders", Module = "orders", Action = "write" },
            new { Name = "orders:delete", Description = "Delete orders", Module = "orders", Action = "delete" },
            new { Name = "orders:manage", Description = "Full order management", Module = "orders", Action = "manage" },
            new { Name = "permissions:read", Description = "View permissions", Module = "permissions", Action = "read" },
            new { Name = "permissions:write", Description = "Create and edit permissions", Module = "permissions", Action = "write" },
            new { Name = "permissions:manage", Description = "Full permission management", Module = "permissions", Action = "manage" },
            new { Name = "settings:read", Description = "View settings", Module = "settings", Action = "read" },
            new { Name = "settings:write", Description = "Edit settings", Module = "settings", Action = "write" },
            new { Name = "settings:manage", Description = "Full settings management", Module = "settings", Action = "manage" },
            new { Name = "security:read", Description = "View security settings", Module = "security", Action = "read" },
            new { Name = "security:write", Description = "Edit security settings", Module = "security", Action = "write" },
            new { Name = "security:manage", Description = "Full security management", Module = "security", Action = "manage" },
            new { Name = "logs:read", Description = "View logs", Module = "logs", Action = "read" },
            new { Name = "logs:manage", Description = "Full log management", Module = "logs", Action = "manage" }
        };

        foreach (var permissionData in permissions)
        {
            var existingPermission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permissionData.Name);

            if (existingPermission == null)
            {
                var permission = new Permission
                {
                    Name = permissionData.Name,
                    Description = permissionData.Description,
                    Module = permissionData.Module,
                    Action = permissionData.Action,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Permissions.Add(permission);
                _logger.LogInformation("Added permission: {PermissionName}", permission.Name);
            }
        }

        await _context.SaveChangesAsync();

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole != null)
        {
            var allPermissions = await _context.Permissions.ToListAsync();
            foreach (var permission in allPermissions)
            {
                var existingRolePermission = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id);

                if (existingRolePermission == null)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id
                    });
                     _logger.LogInformation("Assigned permission '{PermissionName}' to Admin role", permission.Name);
                }
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<DevDebugAdminDto>> GetDebugAdminInfoAsync()
    {
        var adminUsers = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Where(u => u.UserRoles.Any(ur => ur.Role.IsAdminRole))
            .ToListAsync();

        return adminUsers.Select(u => new DevDebugAdminDto
        {
            AdminUser = new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                AdminRoles = u.UserRoles.Where(ur => ur.Role.IsAdminRole).Select(ur => ur.Role.Name).ToList()
            },
            Roles = u.UserRoles.Where(ur => ur.Role.IsAdminRole).Select(ur => new DevDebugRoleDto
            {
                Id = ur.Role.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description,
                IsAdminRole = ur.Role.IsAdminRole,
                PermissionCount = ur.Role.RolePermissions?.Count ?? 0,
                Permissions = ur.Role.RolePermissions?.Select(rp => new DevDebugPermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Module = rp.Permission.Module,
                    Action = rp.Permission.Action
                }).ToList() ?? new List<DevDebugPermissionDto>()
            }).ToList()
        }).ToList();
    }
}

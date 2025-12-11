using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(ApplicationDbContext context, ILogger<PermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        try
        {
            return await _context.Users
                .Where(u => u.Id == userId && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RolePermissions)
                .AnyAsync(rp => rp.Permission.Name == permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user: {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<IEnumerable<AdminPermissionDto>> GetUserPermissionsAsync(int userId)
    {
        try
        {
            var permissions = await _context.Users
                .Where(u => u.Id == userId && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => new AdminPermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module,
                    Action = rp.Permission.Action
                })
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user: {UserId}", userId);
            return Enumerable.Empty<AdminPermissionDto>();
        }
    }

    public async Task<IEnumerable<AdminPermissionDto>> GetAllPermissionsAsync()
    {
        try
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Name)
                .Select(p => new AdminPermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Module = p.Module,
                    Action = p.Action
                })
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all permissions");
            throw;
        }
    }

    public async Task<Dictionary<string, IEnumerable<AdminPermissionDto>>> GetRolePermissionMappingsAsync()
    {
        try
        {
            var rolePermissions = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToDictionaryAsync(
                    role => role.Name,
                    role => role.RolePermissions
                        .Select(rp => new AdminPermissionDto
                        {
                            Id = rp.Permission.Id,
                            Name = rp.Permission.Name,
                            Description = rp.Permission.Description,
                            Module = rp.Permission.Module,
                            Action = rp.Permission.Action
                        }).AsEnumerable()
                );

            return rolePermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role permission mappings");
            throw;
        }
    }

    public async Task SyncPermissionsAsync()
    {
        var basicPermissions = new[]
        {
            new { Name = "dashboard:read", Description = "View dashboard", Module = "dashboard", Action = "read" },
            new { Name = "users:read", Description = "View users", Module = "users", Action = "read" },
            new { Name = "users:write", Description = "Create/update users", Module = "users", Action = "write" },
            new { Name = "users:delete", Description = "Delete users", Module = "users", Action = "delete" },
            new { Name = "products:read", Description = "View products", Module = "products", Action = "read" },
            new { Name = "products:write", Description = "Create/update products", Module = "products", Action = "write" },
            new { Name = "orders:read", Description = "View orders", Module = "orders", Action = "read" },
            new { Name = "orders:manage", Description = "Manage orders", Module = "orders", Action = "manage" }
        };

        foreach (var perm in basicPermissions)
        {
            var existingPermission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == perm.Name);

            if (existingPermission == null)
            {
                var permission = new Permission
                {
                    Name = perm.Name,
                    Description = perm.Description,
                    Module = perm.Module,
                    Action = perm.Action,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Permissions.Add(permission);
            }
        }

        await _context.SaveChangesAsync();
    }
}

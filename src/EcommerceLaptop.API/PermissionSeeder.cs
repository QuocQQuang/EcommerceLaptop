using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.API;

public static class PermissionSeeder
{
    public static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        Console.WriteLine(" Starting permission seeding...");

        // Define all permissions
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

        // Insert permissions if they don't exist
        foreach (var permissionData in permissions)
        {
            var existingPermission = await context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permissionData.Name);

            if (existingPermission == null)
            {
                var permission = new EcommerceLaptop.Core.Entities.Permission
                {
                    Name = permissionData.Name,
                    Description = permissionData.Description,
                    Module = permissionData.Module,
                    Action = permissionData.Action,
                    CreatedAt = DateTime.UtcNow
                };

                context.Permissions.Add(permission);
                Console.WriteLine($" Added permission: {permission.Name}");
            }
            else
            {
                Console.WriteLine($" Permission already exists: {permissionData.Name}");
            }
        }

        await context.SaveChangesAsync();

        // Find Admin role (assuming it exists with Id = 2)
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            Console.WriteLine(" Admin role not found!");
            return;
        }

        Console.WriteLine($" Found Admin role with ID: {adminRole.Id}");

        // Get all permissions
        var allPermissions = await context.Permissions.ToListAsync();
        Console.WriteLine($" Total permissions in database: {allPermissions.Count}");

        // Assign all permissions to Admin role
        foreach (var permission in allPermissions)
        {
            var existingRolePermission = await context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id);

            if (existingRolePermission == null)
            {
                var rolePermission = new EcommerceLaptop.Core.Entities.RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                };

                context.RolePermissions.Add(rolePermission);
                Console.WriteLine($" Assigned permission '{permission.Name}' to Admin role");
            }
            else
            {
                Console.WriteLine($" Permission '{permission.Name}' already assigned to Admin role");
            }
        }

        await context.SaveChangesAsync();

        // Verify results
        var adminPermissionCount = await context.RolePermissions
            .CountAsync(rp => rp.RoleId == adminRole.Id);

        Console.WriteLine($" Permission seeding completed!");
        Console.WriteLine($" Admin role now has {adminPermissionCount} permissions assigned");
    }
}
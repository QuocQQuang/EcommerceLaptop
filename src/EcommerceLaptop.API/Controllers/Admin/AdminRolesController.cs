using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.DTOs.Admin;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers.Admin;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = "AdminPolicy")]
public class AdminRolesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminRolesController> _logger;

    public AdminRolesController(ApplicationDbContext context, ILogger<AdminRolesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles with permissions
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequirePermission:roles:read")]
    public async Task<ActionResult<List<RoleWithPermissionsDto>>> GetRoles()
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Select(r => new RoleWithPermissionsDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Permissions = r.RolePermissions.Select(rp => new AdminPermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module,
                    Action = rp.Permission.Action
                }).ToList(),
                UsersCount = r.UserRoles.Count(ur => ur.User != null && ur.User.IsAdminRole)
            })
            .ToListAsync();

        return Ok(roles);
    }

    /// <summary>
    /// Get role by ID with permissions
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequirePermission:roles:read")]
    public async Task<ActionResult<RoleWithPermissionsDto>> GetRole(int id)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        var roleDto = new RoleWithPermissionsDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
            Permissions = role.RolePermissions.Select(rp => new AdminPermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Module = rp.Permission.Module,
                Action = rp.Permission.Action
            }).ToList(),
            UsersCount = role.UserRoles?.Count(ur => ur.User != null && ur.User.IsAdminRole) ?? 0
        };

        return Ok(roleDto);
    }

    /// <summary>
    /// Create new role
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:roles:write")]
    public async Task<ActionResult<RoleWithPermissionsDto>> CreateRole([FromBody] CreateRoleRequestDto request)
    {
        // Check if role name already exists
        if (await _context.Roles.AnyAsync(r => r.Name == request.Name))
        {
            return BadRequest(new { message = "Role name already exists" });
        }

        // Verify all permission IDs exist
        var existingPermissionIds = await _context.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var invalidPermissionIds = request.PermissionIds.Except(existingPermissionIds).ToList();
        if (invalidPermissionIds.Any())
        {
            return BadRequest(new { message = $"Invalid permission IDs: {string.Join(", ", invalidPermissionIds)}" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var role = new Role
            {
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Add role permissions
            var rolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            }).ToList();

            _context.RolePermissions.AddRange(rolePermissions);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Load permissions for response
            await _context.Entry(role)
                .Collection(r => r.RolePermissions)
                .Query()
                .Include(rp => rp.Permission)
                .LoadAsync();

            var roleDto = new RoleWithPermissionsDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                Permissions = role.RolePermissions.Select(rp => new AdminPermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module,
                    Action = rp.Permission.Action
                }).ToList(),
                UsersCount = 0
            };

            _logger.LogInformation("Role created: {RoleName} by {AdminId}", role.Name, User.FindFirstValue(ClaimTypes.NameIdentifier));

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, roleDto);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePermission:roles:write")]
    public async Task<ActionResult<RoleWithPermissionsDto>> UpdateRole(int id, [FromBody] UpdateRoleRequestDto request)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        // Check if new name already exists for other roles
        if (!string.IsNullOrEmpty(request.Name) &&
            await _context.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id))
        {
            return BadRequest(new { message = "Role name already exists" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update role properties
            if (!string.IsNullOrEmpty(request.Name))
                role.Name = request.Name;
            if (request.Description != null)
                role.Description = request.Description;
            role.UpdatedAt = DateTime.UtcNow;

            // Update permissions if provided
            if (request.PermissionIds != null)
            {
                // Verify all permission IDs exist
                var existingPermissionIds = await _context.Permissions
                    .Where(p => request.PermissionIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                var invalidPermissionIds = request.PermissionIds.Except(existingPermissionIds).ToList();
                if (invalidPermissionIds.Any())
                {
                    return BadRequest(new { message = $"Invalid permission IDs: {string.Join(", ", invalidPermissionIds)}" });
                }

                // Remove existing role permissions
                _context.RolePermissions.RemoveRange(role.RolePermissions);

                // Add new role permissions
                var newRolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId
                }).ToList();

                _context.RolePermissions.AddRange(newRolePermissions);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Reload permissions
            await _context.Entry(role)
                .Collection(r => r.RolePermissions)
                .Query()
                .Include(rp => rp.Permission)
                .LoadAsync();

            var roleDto = new RoleWithPermissionsDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                Permissions = role.RolePermissions.Select(rp => new AdminPermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module,
                    Action = rp.Permission.Action
                }).ToList(),
                UsersCount = role.UserRoles?.Count(ur => ur.User != null && ur.User.IsAdminRole) ?? 0
            };

            _logger.LogInformation("Role updated: {RoleName} by {AdminId}", role.Name, User.FindFirstValue(ClaimTypes.NameIdentifier));

            return Ok(roleDto);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequirePermission:roles:delete")]
    public async Task<ActionResult> DeleteRole(int id)
    {
        var role = await _context.Roles
            .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        // Check if role is in use by admin users
        if (role.UserRoles.Any(ur => ur.User != null && ur.User.IsAdminRole))
        {
            return BadRequest(new { message = "Cannot delete role that is assigned to users" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove role permissions first
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // Remove the role
            _context.Roles.Remove(role);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Role deleted: {RoleName} by {AdminId}", role.Name, User.FindFirstValue(ClaimTypes.NameIdentifier));

            return NoContent();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Assign permissions to role
    /// </summary>
    [HttpPatch("{roleId}/permissions")]
    [Authorize(Policy = "RequirePermission:roles:write")]
    public async Task<ActionResult<RoleWithPermissionsDto>> AssignRolePermissions(int roleId, [FromBody] AssignPermissionsRequestDto request)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        // Verify all permission IDs exist
        var existingPermissionIds = await _context.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        var invalidPermissionIds = request.PermissionIds.Except(existingPermissionIds).ToList();
        if (invalidPermissionIds.Any())
        {
            return BadRequest(new { message = $"Invalid permission IDs: {string.Join(", ", invalidPermissionIds)}" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove existing role permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // Add new role permissions
            var newRolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            }).ToList();

            _context.RolePermissions.AddRange(newRolePermissions);

            role.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Reload permissions
            await _context.Entry(role)
                .Collection(r => r.RolePermissions)
                .Query()
                .Include(rp => rp.Permission)
                .LoadAsync();

            var roleDto = new RoleWithPermissionsDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                Permissions = role.RolePermissions.Select(rp => new AdminPermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module,
                    Action = rp.Permission.Action
                }).ToList(),
                UsersCount = role.UserRoles?.Count(ur => ur.User != null && ur.User.IsAdminRole) ?? 0
            };

            _logger.LogInformation("Role permissions updated: {RoleName} by {AdminId}", role.Name, User.FindFirstValue(ClaimTypes.NameIdentifier));

            return Ok(roleDto);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

/// <summary>
/// Assign permissions request DTO
/// </summary>
public class AssignPermissionsRequestDto
{
    public List<int> PermissionIds { get; set; } = new();
}
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Specifications;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services;

public class AdminRoleService : IAdminRoleService
{
    private readonly IAsyncRepository<Role> _roleRepository;
    private readonly IAsyncRepository<Permission> _permissionRepository;
    private readonly IAsyncRepository<RolePermission> _rolePermissionRepository;
    private readonly ILogger<AdminRoleService> _logger;

    public AdminRoleService(
        IAsyncRepository<Role> roleRepository,
        IAsyncRepository<Permission> permissionRepository,
        IAsyncRepository<RolePermission> rolePermissionRepository,
        ILogger<AdminRoleService> logger)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _logger = logger;
    }

    public async Task<List<RoleWithPermissionsDto>> GetAllRolesAsync()
    {
        var spec = new RoleWithPermissionsSpecification();
        var roles = await _roleRepository.GetAsync(spec);

        return roles.Select(MapToDto).ToList();
    }

    public async Task<RoleWithPermissionsDto?> GetRoleByIdAsync(int id)
    {
        var spec = new RoleWithPermissionsSpecification(id);
        var role = await _roleRepository.GetEntityWithSpec(spec);

        if (role == null) return null;

        return MapToDto(role);
    }

    public async Task<RoleWithPermissionsDto> CreateRoleAsync(CreateRoleRequestDto request, string adminId)
    {
        // Check if name exists
        // Note: Simple check without loading everything. 
        // We'd ideally have a spec for GetByName or AnyAsync support in Repo.
        // For now, let's just get all or filter.
        // Optimization: Add AnyAsync(predicate) to Repo later. For now, using GetAsync(predicate).
        var existingRoles = await _roleRepository.GetAsync(r => r.Name == request.Name);
        if (existingRoles.Any())
        {
            throw new ArgumentException("Role name already exists");
        }

        // Verify permissions
        await ValidatePermissions(request.PermissionIds);

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _roleRepository.AddAsync(role);

        // Add permissions
        foreach (var permId in request.PermissionIds)
        {
            await _rolePermissionRepository.AddAsync(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permId
            });
        }

        _logger.LogInformation("Role created: {RoleName} by {AdminId}", role.Name, adminId);

        // Reload to return full DTO
        return (await GetRoleByIdAsync(role.Id))!;
    }

    public async Task<RoleWithPermissionsDto> UpdateRoleAsync(int id, UpdateRoleRequestDto request, string adminId)
    {
        var spec = new RoleWithPermissionsSpecification(id);
        var role = await _roleRepository.GetEntityWithSpec(spec);
        if (role == null) throw new KeyNotFoundException("Role not found");

        if (!string.IsNullOrEmpty(request.Name))
        {
            var existingRoles = await _roleRepository.GetAsync(r => r.Name == request.Name && r.Id != id);
            if (existingRoles.Any())
            {
                throw new ArgumentException("Role name already exists");
            }
            role.Name = request.Name;
        }

        if (request.Description != null)
        {
            role.Description = request.Description;
        }

        role.UpdatedAt = DateTime.UtcNow;
        await _roleRepository.UpdateAsync(role);

        if (request.PermissionIds != null)
        {
            await ValidatePermissions(request.PermissionIds);

            // Remove existing
            // Note: Repository.DeleteAsync takes entity. We need to fetch or remove by range.
            // Our basic Repo lacks RemoveRange.
            // This is where "Sanitization" gets tricky without full Repo power.
            // I will iterate delete for now (inefficient but safe for Phase 1).
            foreach (var rp in role.RolePermissions.ToList())
            {
                await _rolePermissionRepository.DeleteAsync(rp);
            }

            // Add new
            foreach (var permId in request.PermissionIds)
            {
                await _rolePermissionRepository.AddAsync(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
        }

        _logger.LogInformation("Role updated: {RoleName} by {AdminId}", role.Name, adminId);
        
        // Re-fetch to be safe and clean
        return (await GetRoleByIdAsync(role.Id))!;
    }

    public async Task DeleteRoleAsync(int id, string adminId)
    {
        var spec = new RoleWithPermissionsSpecification(id);
        var role = await _roleRepository.GetEntityWithSpec(spec);
        if (role == null) throw new KeyNotFoundException("Role not found");

        // Check usage
        if (role.UserRoles != null && role.UserRoles.Any(ur => ur.User != null && ur.User.IsAdminRole))
        {
             throw new InvalidOperationException("Cannot delete role that is assigned to users");   
        }
        
        foreach (var rp in role.RolePermissions.ToList())
        {
            await _rolePermissionRepository.DeleteAsync(rp);
        }

        await _roleRepository.DeleteAsync(role);
        _logger.LogInformation("Role deleted: {RoleName} by {AdminId}", role.Name, adminId);
    }

    public async Task<RoleWithPermissionsDto> AssignPermissionsAsync(int roleId, List<int> permissionIds, string adminId)
    {
        var updateRequest = new UpdateRoleRequestDto { PermissionIds = permissionIds };
        return await UpdateRoleAsync(roleId, updateRequest, adminId);
    }

    private async Task ValidatePermissions(List<int> permissionIds)
    {
        var existing = await _permissionRepository.GetAsync(p => permissionIds.Contains(p.Id));
        if (existing.Count != permissionIds.Count)
        {
            throw new ArgumentException("Invalid permission IDs");
        }
    }

    private static RoleWithPermissionsDto MapToDto(Role role)
    {
        return new RoleWithPermissionsDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
            Permissions = role.RolePermissions.Select(rp => new AdminPermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name!,
                Description = rp.Permission.Description,
                Module = rp.Permission.Module,
                Action = rp.Permission.Action
            }).ToList(),
            // Logic matches AdminRolesController
            UsersCount = role.UserRoles?.Count(ur => ur.User != null && ur.User.IsAdminRole) ?? 0
        };
    }
}

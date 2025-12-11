using EcommerceLaptop.Core.DTOs.Admin;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Service suitable for Permission and Role-Permission mapping management
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks if user has specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permission);

    /// <summary>
    /// Gets all permissions for user
    /// </summary>
    Task<IEnumerable<AdminPermissionDto>> GetUserPermissionsAsync(int userId);

    /// <summary>
    /// Gets all available permissions in system
    /// </summary>
    Task<IEnumerable<AdminPermissionDto>> GetAllPermissionsAsync();

    /// <summary>
    /// Gets role-permission mappings
    /// </summary>
    Task<Dictionary<string, IEnumerable<AdminPermissionDto>>> GetRolePermissionMappingsAsync();

    /// <summary>
    /// Synchronizes permissions between database and application
    /// </summary>
    Task SyncPermissionsAsync();
}

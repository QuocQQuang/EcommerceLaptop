using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Service suitable for User Account management (Profile, Roles, User Creation)
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Gets user profile by ID
    /// </summary>
    Task<UnifiedUserDto?> GetUserProfileAsync(int userId);

    /// <summary>
    /// Updates user profile information
    /// </summary>
    Task<UnifiedUserDto?> UpdateUserProfileAsync(int userId, UpdateProfileRequest updateRequest);

    /// <summary>
    /// Changes user password with security validation
    /// </summary>
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    /// <summary>
    /// Changes password for authenticated user (via request object)
    /// </summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(UnifiedChangePasswordRequest request);

    /// <summary>
    /// Gets user roles
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(int userId);

    /// <summary>
    /// Assigns role to user
    /// </summary>
    Task<bool> AssignRoleAsync(int userId, string roleName);

    /// <summary>
    /// Removes role from user
    /// </summary>
    Task<bool> RemoveRoleAsync(int userId, string roleName);

    /// <summary>
    /// Creates new user account (Admin context)
    /// </summary>
    Task<UnifiedUserDto?> CreateUserAsync(string email, string password, string firstName, string lastName,
        UserType userType, IEnumerable<string> roles, string? phoneNumber = null);

    /// <summary>
    /// Deactivates user account (soft delete)
    /// </summary>
    Task<bool> DeactivateUserAsync(int userId);

    /// <summary>
    /// Maps legacy AdminUser ID to unified User ID
    /// </summary>
    Task<int?> MapAdminUserIdToUnifiedUserIdAsync(int adminUserId);
}

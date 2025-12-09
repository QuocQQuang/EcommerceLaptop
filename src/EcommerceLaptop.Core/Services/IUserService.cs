using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// User management service interface with role-based access control
/// Implements enterprise user management patterns
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets user by ID with roles
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User with roles if found</returns>
    Task<User?> GetByIdAsync(int userId);

    /// <summary>
    /// Gets user by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>User if found</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Creates a new user account
    /// </summary>
    /// <param name="user">User entity to create</param>
    /// <param name="password">Plain text password</param>
    /// <param name="roles">Initial roles to assign</param>
    /// <returns>Created user</returns>
    Task<User> CreateUserAsync(User user, string password, IEnumerable<string> roles);

    /// <summary>
    /// Updates existing user information
    /// </summary>
    /// <param name="user">User entity with updates</param>
    /// <returns>Updated user</returns>
    Task<User> UpdateUserAsync(User user);

    /// <summary>
    /// Deactivates user account (soft delete)
    /// </summary>
    /// <param name="userId">User ID to deactivate</param>
    /// <returns>True if deactivated successfully</returns>
    Task<bool> DeactivateUserAsync(int userId);

    /// <summary>
    /// Gets user roles by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of role names</returns>
    Task<IEnumerable<string>> GetUserRolesAsync(int userId);

    /// <summary>
    /// Assigns role to user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name to assign</param>
    /// <returns>True if role assigned successfully</returns>
    Task<bool> AssignRoleAsync(int userId, string roleName);

    /// <summary>
    /// Removes role from user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name to remove</param>
    /// <returns>True if role removed successfully</returns>
    Task<bool> RemoveRoleAsync(int userId, string roleName);

    /// <summary>
    /// Verifies user password
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="password">Plain text password to verify</param>
    /// <returns>True if password is correct</returns>
    Task<bool> VerifyPasswordAsync(User user, string password);

    /// <summary>
    /// Updates user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newPassword">New plain text password</param>
    /// <returns>True if password updated successfully</returns>
    Task<bool> UpdatePasswordAsync(int userId, string newPassword);

    /// <summary>
    /// Checks if user has specific permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permission">Permission to check</param>
    /// <returns>True if user has permission</returns>
    Task<bool> HasPermissionAsync(int userId, string permission);

    /// <summary>
    /// Gets paginated list of users
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <returns>Paginated user list</returns>
    Task<PagedResult<User>> GetUsersAsync(int page, int pageSize, string? searchTerm = null);

    /// <summary>
    /// Gets all addresses for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user addresses</returns>
    Task<IEnumerable<Address>> GetUserAddressesAsync(int userId);

    /// <summary>
    /// Creates a new address for a user
    /// </summary>
    /// <param name="address">Address to create</param>
    /// <returns>Created address</returns>
    Task<Address> CreateUserAddressAsync(Address address);

    /// <summary>
    /// Updates an existing address
    /// </summary>
    /// <param name="address">Address to update</param>
    /// <returns>Updated address or null if not found</returns>
    Task<Address?> UpdateUserAddressAsync(Address address);

    /// <summary>
    /// Deletes an address
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="addressId">Address ID</param>
    /// <returns>True if address was deleted</returns>
    Task<bool> DeleteUserAddressAsync(int userId, int addressId);

    /// <summary>
    /// Sets an address as the default address for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="addressId">Address ID</param>
    /// <returns>True if address was set as default</returns>
    Task<bool> SetDefaultAddressAsync(int userId, int addressId);
}

using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Authentication service interface supporting context-aware authentication
/// Handles both customer and admin authentication through unified interface
/// </summary>
public interface IAuthService
{
    // =====================================================
    // Core Authentication Methods
    // =====================================================

    /// <summary>
    /// Authenticates user with context-aware security
    /// Replaces both AuthenticateAsync methods from customer and admin services
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <param name="context">Authentication context (Customer, Admin, etc.)</param>
    /// <param name="ipAddress">Client IP address for security logging</param>
    /// <param name="userAgent">Client user agent for audit trail</param>
    /// <returns>Unified authentication result</returns>
    Task<UnifiedAuthResult?> AuthenticateAsync(string email, string password, AuthContext context, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Refreshes authentication token for any user type
    /// Unifies customer and admin token refresh logic
    /// </summary>
    /// <param name="refreshToken">Existing refresh token</param>
    /// <param name="context">Authentication context</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <returns>New token pair</returns>
    Task<UnifiedRefreshResult?> RefreshTokenAsync(string refreshToken, AuthContext context, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Logs out user and revokes tokens
    /// Works for all user types
    /// </summary>
    /// <param name="userId">User ID from unified Users table</param>
    /// <param name="context">Authentication context</param>
    /// <param name="refreshToken">Optional refresh token to revoke</param>
    /// <returns>Success status</returns>
    Task<bool> LogoutAsync(int userId, AuthContext context, string? refreshToken = null);

    // =====================================================
    // Permission Management (Preserves IAdminAuthService methods)
    // =====================================================

    /// <summary>
    /// Checks if user has specific permission
    /// Unifies permission checking across all user types
    /// </summary>
    /// <param name="userId">User ID from unified Users table</param>
    /// <param name="permission">Permission to check (e.g., "users:read")</param>
    /// <returns>True if user has permission</returns>
    Task<bool> HasPermissionAsync(int userId, string permission);

    /// <summary>
    /// Gets all permissions for user (primarily for admin users)
    /// Maintains compatibility with existing admin permission system
    /// </summary>
    /// <param name="userId">User ID from unified Users table</param>
    /// <returns>List of user permissions</returns>
    Task<IEnumerable<AdminPermissionDto>> GetUserPermissionsAsync(int userId);

    /// <summary>
    /// Gets all available permissions in system
    /// Maintains compatibility with existing dynamic permission management
    /// </summary>
    /// <returns>All available permissions</returns>
    Task<IEnumerable<AdminPermissionDto>> GetAllPermissionsAsync();

    /// <summary>
    /// Gets role-permission mappings
    /// Maintains compatibility with existing role management
    /// </summary>
    /// <returns>Dictionary mapping role names to permissions</returns>
    Task<Dictionary<string, IEnumerable<AdminPermissionDto>>> GetRolePermissionMappingsAsync();

    // =====================================================
    // User Profile Management
    // =====================================================

    /// <summary>
    /// Gets user profile by ID
    /// Works for all user types, returns appropriate data based on UserType
    /// </summary>
    /// <param name="userId">User ID from unified Users table</param>
    /// <returns>Unified user profile</returns>
    Task<UnifiedUserDto?> GetUserProfileAsync(int userId);

    /// <summary>
    /// Updates user profile information
    /// Context-aware updates based on user type
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="updateRequest">Update request with changed fields</param>
    /// <returns>Updated user profile</returns>
    Task<UnifiedUserDto?> UpdateUserProfileAsync(int userId, UpdateProfileRequest updateRequest);

    /// <summary>
    /// Changes user password with security validation
    /// Works for all user types with appropriate security measures
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password for validation</param>
    /// <param name="newPassword">New password</param>
    /// <returns>Success status</returns>
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    // =====================================================
    // Registration & Password Management (Customer-focused)
    // =====================================================

    /// <summary>
    /// Registers new user account
    /// Context-aware registration supporting customer, admin, and other user types
    /// </summary>
    /// <param name="request">Registration request with user details</param>
    /// <returns>Authentication result with tokens</returns>
    Task<UnifiedAuthResult?> RegisterAsync(UnifiedRegisterRequest request);

    /// <summary>
    /// Initiates forgot password process
    /// Context-aware password reset supporting all user types
    /// </summary>
    /// <param name="request">Forgot password request</param>
    /// <returns>Success status and message</returns>
    Task<(bool Success, string Message)> ForgotPasswordAsync(UnifiedForgotPasswordRequest request);

    /// <summary>
    /// Resets password using reset token
    /// Context-aware password reset completion
    /// </summary>
    /// <param name="request">Reset password request with token</param>
    /// <returns>Success status and message</returns>
    Task<(bool Success, string Message)> ResetPasswordAsync(UnifiedResetPasswordRequest request);

    /// <summary>
    /// Changes password for authenticated user
    /// Context-aware password change with unified validation
    /// </summary>
    /// <param name="request">Change password request</param>
    /// <returns>Success status and message</returns>
    Task<(bool Success, string Message)> ChangePasswordAsync(UnifiedChangePasswordRequest request);

    /// <summary>
    /// Generate a secure email confirmation token for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="ipAddress">IP address for security logging</param>
    /// <param name="userAgent">User agent for security logging</param>
    /// <returns>Success status and generated token</returns>
    Task<(bool Success, string? Token)> GenerateEmailConfirmationTokenAsync(int userId, string email, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Validate and consume an email confirmation token
    /// </summary>
    /// <param name="token">Confirmation token</param>
    /// <param name="ipAddress">IP address for security logging</param>
    /// <param name="userAgent">User agent for security logging</param>
    /// <returns>Success status and message</returns>
    Task<(bool Success, string Message)> ConfirmEmailWithTokenAsync(string token, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Confirms user email address (Legacy - use ConfirmEmailWithTokenAsync for secure confirmation)
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>Success status</returns>
    [Obsolete("Use ConfirmEmailWithTokenAsync for secure token-based confirmation")]
    Task<bool> ConfirmEmailAsync(string email);

    /// <summary>
    /// Resend email confirmation with new secure token
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="ipAddress">IP address for security logging</param>
    /// <param name="userAgent">User agent for security logging</param>
    /// <returns>Success status and message</returns>
    Task<(bool Success, string Message)> ResendEmailConfirmationAsync(string email, string? ipAddress = null, string? userAgent = null);

    // =====================================================
    // Role Management (Preserves existing IUserService methods)
    // =====================================================

    /// <summary>
    /// Gets user roles
    /// Compatible with existing business logic that depends on role checking
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of role names</returns>
    Task<IEnumerable<string>> GetUserRolesAsync(int userId);

    /// <summary>
    /// Assigns role to user
    /// Maintains compatibility with existing role assignment logic
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name to assign</param>
    /// <returns>Success status</returns>
    Task<bool> AssignRoleAsync(int userId, string roleName);

    /// <summary>
    /// Removes role from user
    /// Maintains compatibility with existing role management
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name to remove</param>
    /// <returns>Success status</returns>
    Task<bool> RemoveRoleAsync(int userId, string roleName);

    // =====================================================
    // Audit & Security (Preserves IAdminAuthService audit methods)
    // =====================================================

    /// <summary>
    /// Records audit log for user actions
    /// Maintains compatibility with existing admin audit system
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="action">Action performed</param>
    /// <param name="entity">Entity affected</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="details">Additional details</param>
    /// <param name="oldValues">Old values (for updates)</param>
    /// <param name="newValues">New values</param>
    /// <param name="ipAddress">Client IP</param>
    /// <param name="userAgent">Client user agent</param>
    /// <returns>Task</returns>
    Task RecordAuditLogAsync(int userId, string action, string entity, string? entityId = null,
        string? details = null, string? oldValues = null, string? newValues = null,
        string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Synchronizes permissions between database and application
    /// Maintains compatibility with existing permission sync functionality
    /// </summary>
    /// <returns>Task</returns>
    Task SyncPermissionsAsync();

    // =====================================================
    // User Management (For admin contexts)
    // =====================================================

    /// <summary>
    /// Creates new user account
    /// Context-aware user creation (customer vs admin vs employee)
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <param name="firstName">First name</param>
    /// <param name="lastName">Last name</param>
    /// <param name="userType">Type of user to create</param>
    /// <param name="roles">Initial roles to assign</param>
    /// <param name="phoneNumber">Optional phone number</param>
    /// <returns>Created user profile</returns>
    Task<UnifiedUserDto?> CreateUserAsync(string email, string password, string firstName, string lastName,
        UserType userType, IEnumerable<string> roles, string? phoneNumber = null);

    /// <summary>
    /// Deactivates user account (soft delete)
    /// Works for all user types
    /// </summary>
    /// <param name="userId">User ID to deactivate</param>
    /// <returns>Success status</returns>
    Task<bool> DeactivateUserAsync(int userId);

    // =====================================================
    // Helper Methods for Backward Compatibility
    // =====================================================

    /// <summary>
    /// Gets current user from storage (for frontend contexts)
    /// Maintains compatibility with existing frontend auth context
    /// </summary>
    /// <returns>Current authenticated user</returns>
    UnifiedUserDto? GetCurrentUserFromStorage();

    /// <summary>
    /// Maps legacy AdminUser ID to unified User ID
    /// Helper method for transitioning existing business logic
    /// </summary>
    /// <param name="adminUserId">Legacy admin user ID</param>
    /// <returns>Unified user ID</returns>
    Task<int?> MapAdminUserIdToUnifiedUserIdAsync(int adminUserId);
}

/// <summary>
/// Update profile request DTO for unified user profile updates
/// </summary>
public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public string? Notes { get; set; }
}
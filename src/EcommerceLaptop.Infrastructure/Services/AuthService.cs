using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Facade for Authentication, Authorization, and Account Management services.
/// Delegates responsibilities to IdentityService, PermissionService, and AccountService.
/// Maintains backward compatibility for IAuthService consumers.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IIdentityService _identityService;
    private readonly IPermissionService _permissionService;
    private readonly IAccountService _accountService;

    public AuthService(
        IIdentityService identityService,
        IPermissionService permissionService,
        IAccountService accountService)
    {
        _identityService = identityService;
        _permissionService = permissionService;
        _accountService = accountService;
    }

    // =====================================================
    // Identity Operations (Delegated to IdentityService)
    // =====================================================

    public Task<UnifiedAuthResult?> AuthenticateAsync(string email, string password, AuthContext context, string? ipAddress = null, string? userAgent = null)
        => _identityService.AuthenticateAsync(email, password, context, ipAddress, userAgent);

    public Task<UnifiedRefreshResult?> RefreshTokenAsync(string refreshToken, AuthContext context, string? ipAddress = null, string? userAgent = null)
        => _identityService.RefreshTokenAsync(refreshToken, context, ipAddress, userAgent);

    public Task<bool> LogoutAsync(int userId, AuthContext context, string? refreshToken = null)
        => _identityService.LogoutAsync(userId, context, refreshToken);

    public Task<UnifiedAuthResult?> RegisterAsync(UnifiedRegisterRequest request)
        => _identityService.RegisterAsync(request);

    public Task<(bool Success, string Message)> ForgotPasswordAsync(UnifiedForgotPasswordRequest request)
        => _identityService.ForgotPasswordAsync(request);

    public Task<(bool Success, string Message)> ResetPasswordAsync(UnifiedResetPasswordRequest request)
        => _identityService.ResetPasswordAsync(request);

    public Task<(bool Success, string? Token)> GenerateEmailConfirmationTokenAsync(int userId, string email, string? ipAddress = null, string? userAgent = null)
        => _identityService.GenerateEmailConfirmationTokenAsync(userId, email, ipAddress, userAgent);

    public Task<(bool Success, string Message)> ConfirmEmailWithTokenAsync(string token, string? ipAddress = null, string? userAgent = null)
        => _identityService.ConfirmEmailWithTokenAsync(token, ipAddress, userAgent);

    public Task<(bool Success, string Message)> ResendEmailConfirmationAsync(string email, string? ipAddress = null, string? userAgent = null)
        => _identityService.ResendEmailConfirmationAsync(email, ipAddress, userAgent);

    [Obsolete("Use ConfirmEmailWithTokenAsync")]
    public Task<bool> ConfirmEmailAsync(string email)
        => _identityService.ConfirmEmailAsync(email);

    public UnifiedUserDto? GetCurrentUserFromStorage()
        => _identityService.GetCurrentUserFromStorage();

    // =====================================================
    // Permission Operations (Delegated to PermissionService)
    // =====================================================

    public Task<bool> HasPermissionAsync(int userId, string permission)
        => _permissionService.HasPermissionAsync(userId, permission);

    public Task<IEnumerable<AdminPermissionDto>> GetUserPermissionsAsync(int userId)
        => _permissionService.GetUserPermissionsAsync(userId);

    public Task<IEnumerable<AdminPermissionDto>> GetAllPermissionsAsync()
        => _permissionService.GetAllPermissionsAsync();

    public Task<Dictionary<string, IEnumerable<AdminPermissionDto>>> GetRolePermissionMappingsAsync()
        => _permissionService.GetRolePermissionMappingsAsync();

    // Stubbed in implementation but interface method exists
    public Task SyncPermissionsAsync()
        => _permissionService.SyncPermissionsAsync();


    // =====================================================
    // Account Operations (Delegated to AccountService)
    // =====================================================

    public Task<UnifiedUserDto?> GetUserProfileAsync(int userId)
        => _accountService.GetUserProfileAsync(userId);

    public Task<UnifiedUserDto?> UpdateUserProfileAsync(int userId, UpdateProfileRequest updateRequest)
        => _accountService.UpdateUserProfileAsync(userId, updateRequest);

    public Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        => _accountService.ChangePasswordAsync(userId, currentPassword, newPassword);

    public Task<(bool Success, string Message)> ChangePasswordAsync(UnifiedChangePasswordRequest request)
        => _accountService.ChangePasswordAsync(request);

    public Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        => _accountService.GetUserRolesAsync(userId);

    public Task<bool> AssignRoleAsync(int userId, string roleName)
        => _accountService.AssignRoleAsync(userId, roleName);

    public Task<bool> RemoveRoleAsync(int userId, string roleName)
        => _accountService.RemoveRoleAsync(userId, roleName);

    public Task<UnifiedUserDto?> CreateUserAsync(string email, string password, string firstName, string lastName, UserType userType, IEnumerable<string> roles, string? phoneNumber = null)
        => _accountService.CreateUserAsync(email, password, firstName, lastName, userType, roles, phoneNumber);

    public Task<bool> DeactivateUserAsync(int userId)
        => _accountService.DeactivateUserAsync(userId);

    public Task<int?> MapAdminUserIdToUnifiedUserIdAsync(int adminUserId)
        => _accountService.MapAdminUserIdToUnifiedUserIdAsync(adminUserId);

    // =====================================================
    // Deprecated Methods
    // =====================================================

    public Task RecordAuditLogAsync(int userId, string action, string entity, string? entityId = null,
        string? details = null, string? oldValues = null, string? newValues = null,
        string? ipAddress = null, string? userAgent = null)
    {
        return Task.CompletedTask; // No-op as per original
    }
}

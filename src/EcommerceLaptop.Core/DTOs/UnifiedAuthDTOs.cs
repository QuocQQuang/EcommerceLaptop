using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// User DTO for backward compatibility with legacy authentication system
/// </summary>
public record UserDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? ProfilePictureUrl,
    bool IsActive,
    DateTime CreatedAt,
    List<string> Roles)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Unified authentication context for permission system
/// Supports multiple user types while preserving existing business logic
/// </summary>
public enum AuthContext
{
    Customer = 1,
    Admin = 2,
    Employee = 3,
    Partner = 4,
    API = 5,
    Mobile = 6
}

/// <summary>
/// Enhanced user type enum for unified permission system
/// Replaces separated Users/AdminUsers model
/// </summary>
public enum UserType
{
    Customer = 1,
    Admin = 2,
    Employee = 3,
    Partner = 4
}

/// <summary>
/// Unified authentication result for all user contexts
/// Replaces separate AuthenticationResult and AdminLoginResponseDto
/// </summary>
public record UnifiedAuthResult(
    bool Success,
    string? ErrorMessage,
    string? AccessToken,
    string? RefreshToken,
    DateTime ExpiresAt,
    int ExpiresIn,
    UnifiedUserDto? User,
    AuthContext Context)
{
    /// <summary>
    /// Converts unified result to legacy AdminLoginResponseDto for backward compatibility
    /// </summary>
    public AdminLoginResponseDto? ToAdminLoginResponse()
    {
        if (!Success || User == null) return null;

        return new AdminLoginResponseDto
        {
            User = new AdminUserDto
            {
                Id = User.Id,
                Email = User.Email,
                FirstName = User.FirstName,
                LastName = User.LastName,
                Avatar = User.Avatar,
                IsActive = User.IsActive,
                IsEmailVerified = User.IsEmailVerified,
                CreatedAt = User.CreatedAt,
                LastLoginAt = User.LastLoginAt,
                Role = User.PrimaryRole != null ? new AdminRoleDto
                {
                    Id = User.PrimaryRole.Id,
                    Name = User.PrimaryRole.Name,
                    Description = User.PrimaryRole.Description,
                    Permissions = User.Permissions
                } : new AdminRoleDto(),
                Permissions = User.Permissions
            },
            AccessToken = AccessToken!,
            RefreshToken = RefreshToken!,
            ExpiresAt = ExpiresAt,
            ExpiresIn = ExpiresIn
        };
    }

    /// <summary>
    /// Converts unified result to legacy AuthenticationResult for backward compatibility
    /// </summary>
    public AuthenticationResult? ToAuthenticationResult()
    {
        if (!Success || User == null) return null;

        return new AuthenticationResult
        {
            Success = Success,
            ErrorMessage = ErrorMessage,
            AccessToken = AccessToken,
            RefreshToken = RefreshToken,
            AccessTokenExpiry = ExpiresAt,
            User = new Core.Entities.User
            {
                Id = User.Id,
                Email = User.Email,
                FirstName = User.FirstName,
                LastName = User.LastName,
                PhoneNumber = User.PhoneNumber ?? string.Empty,
                IsActive = User.IsActive,
                CreatedAt = User.CreatedAt
            },
            Roles = User.Roles.Select(r => r.Name).ToList()
        };
    }
}

/// <summary>
/// Unified user DTO that works for all user types
/// Consolidates AdminUserDto and regular UserDto
/// </summary>
public record UnifiedUserDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Avatar,
    bool IsActive,
    bool IsEmailVerified,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    string? LastLoginIP,
    UserType UserType,
    List<RoleDto> Roles,
    List<AdminPermissionDto> Permissions,
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    string? Notes)
{
    public RoleDto? PrimaryRole => Roles.FirstOrDefault();

    /// <summary>
    /// Checks if user has specific permission
    /// </summary>
    public bool HasPermission(string permission)
    {
        return Permissions.Any(p => p.Name == permission);
    }

    /// <summary>
    /// Checks if user has any of the specified roles
    /// </summary>
    public bool HasRole(params string[] roleNames)
    {
        return Roles.Any(r => roleNames.Contains(r.Name));
    }

    /// <summary>
    /// Gets user display name
    /// </summary>
    public string DisplayName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Unified refresh token result
/// </summary>
public record UnifiedRefreshResult(
    bool Success,
    string? ErrorMessage,
    string? AccessToken,
    string? RefreshToken,
    DateTime ExpiresAt,
    int ExpiresIn)
{
    /// <summary>
    /// Converts to legacy AdminRefreshTokenResponseDto
    /// </summary>
    public AdminRefreshTokenResponseDto? ToAdminRefreshResponse()
    {
        if (!Success) return null;

        return new AdminRefreshTokenResponseDto
        {
            AccessToken = AccessToken!,
            RefreshToken = RefreshToken!,
            ExpiresAt = ExpiresAt,
            ExpiresIn = ExpiresIn
        };
    }
}

/// <summary>
/// Role DTO that works for both admin and customer roles
/// </summary>
public record RoleDto(
    int Id,
    string Name,
    string Description,
    bool IsAdminRole,
    List<AdminPermissionDto> Permissions);

/// <summary>
/// Unified login request that supports context-aware authentication
/// </summary>
public record UnifiedLoginRequest(
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
    string Email,

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự và không quá 100 ký tự")]
    string Password,

    AuthContext? Context = null,
    string? IpAddress = null,
    string? UserAgent = null,
    bool RememberMe = false);

/// <summary>
/// Unified registration request that supports context-aware registration
/// </summary>
public record UnifiedRegisterRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
    public string Email { get; init; } = default!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự và không quá 100 ký tự")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
    public string Password { get; init; } = default!;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; init; } = default!;

    [Required(ErrorMessage = "Tên là bắt buộc")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên phải có ít nhất 3 ký tự và không quá 100 ký tự")]
    [RegularExpression(@"^[a-zA-Z-\s]+$", ErrorMessage = "Tên chỉ được chứa chữ cái và khoảng trắng")]
    public string FirstName { get; init; } = default!;

    [Required(ErrorMessage = "Họ là bắt buộc")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Họ phải có ít nhất 3 ký tự và không quá 100 ký tự")]
    [RegularExpression(@"^[a-zA-Z-\s]+$", ErrorMessage = "Họ chỉ được chứa chữ cái và khoảng trắng")]
    public string LastName { get; init; } = default!;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string? PhoneNumber { get; init; }

    [Required(ErrorMessage = "Bạn phải đồng ý với điều khoản")]
    public bool AcceptTerms { get; init; } = false;

    public AuthContext? Context { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

/// <summary>
/// Unified change password request
/// </summary>
public record UnifiedChangePasswordRequest
{
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu hiện tại phải có ít nhất 8 ký tự và không quá 100 ký tự")]
    public string CurrentPassword { get; init; } = default!;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự và không quá 100 ký tự")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Mật khẩu mới phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
    public string NewPassword { get; init; } = default!;

    [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; init; } = default!;

    public AuthContext? Context { get; init; }
}

/// <summary>
/// Unified forgot password request
/// </summary>
public record UnifiedForgotPasswordRequest(
    string Email,
    AuthContext? Context = null);

/// <summary>
/// Unified reset password request
/// </summary>
public record UnifiedResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword,
    string? IpAddress = null,
    AuthContext? Context = null);

/// <summary>
/// Resend email confirmation request
/// </summary>
public record ResendEmailConfirmationRequest(
    string Email);
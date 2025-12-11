using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Service suitable for Identity management (Login, Registration, Token management)
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Authenticates user with context-aware security
    /// </summary>
    Task<UnifiedAuthResult?> AuthenticateAsync(string email, string password, AuthContext context, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Refreshes authentication token for any user type
    /// </summary>
    Task<UnifiedRefreshResult?> RefreshTokenAsync(string refreshToken, AuthContext context, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Logs out user and revokes tokens
    /// </summary>
    Task<bool> LogoutAsync(int userId, AuthContext context, string? refreshToken = null);

    /// <summary>
    /// Registers new user account
    /// </summary>
    Task<UnifiedAuthResult?> RegisterAsync(UnifiedRegisterRequest request);

    /// <summary>
    /// Initiates forgot password process
    /// </summary>
    Task<(bool Success, string Message)> ForgotPasswordAsync(UnifiedForgotPasswordRequest request);

    /// <summary>
    /// Resets password using reset token
    /// </summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(UnifiedResetPasswordRequest request);

    /// <summary>
    /// Generate a secure email confirmation token for a user
    /// </summary>
    Task<(bool Success, string? Token)> GenerateEmailConfirmationTokenAsync(int userId, string email, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Validate and consume an email confirmation token
    /// </summary>
    Task<(bool Success, string Message)> ConfirmEmailWithTokenAsync(string token, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Resend email confirmation with new secure token
    /// </summary>
    Task<(bool Success, string Message)> ResendEmailConfirmationAsync(string email, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Confirms user email address (Legacy)
    /// </summary>
    [Obsolete("Use ConfirmEmailWithTokenAsync for secure token-based confirmation")]
    Task<bool> ConfirmEmailAsync(string email);

    /// <summary>
    /// Gets current user from storage (for frontend contexts)
    /// </summary>
    UnifiedUserDto? GetCurrentUserFromStorage();
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Infrastructure.Services.Security;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Authentication controller supporting all user types with context-aware authentication
/// Handles both customer and admin authentication through unified API
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController(
    IAuthService authService,
    IAuditLoggingService auditLoggingService,
    ILogger<AuthController> logger) : BaseApiController(logger)
{
    private readonly IAuthService _authService = authService;
    private readonly IAuditLoggingService _auditLoggingService = auditLoggingService;

    /// <summary>
    /// Universal login endpoint supporting all user types
    /// Replaces both /api/auth/login and /api/admin/auth/login
    /// </summary>
    /// <param name="request">Unified login request with context</param>
    /// <returns>Unified authentication result</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AdminAuthPolicy")]
    public async Task<IActionResult> Login([FromBody] UnifiedLoginRequest request)
    {
        // Default to Customer context if not specified
        var context = request.Context ?? AuthContext.Customer;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        _logger.LogInformation(" UNIFIED LOGIN ATTEMPT - Email: {Email} | Context: {Context} | IP: {IP}",
            request.Email, context, ipAddress);

        var result = await _authService.AuthenticateAsync(
            request.Email,
            request.Password,
            context,
            ipAddress,
            userAgent);

        if (result == null || !result.Success)
        {
            _logger.LogWarning(" UNIFIED LOGIN FAILED - Email: {Email} | Context: {Context}",
                request.Email, context);

            // Log failed login as security event
            await _auditLoggingService.LogSecurityEventAsync(
                "failed_login",
                $"Failed login attempt for email: {request.Email}",
                ipAddress ?? "Unknown",
                correlationId: Guid.NewGuid().ToString()
            );

            return Unauthorized(new
            {
                success = false,
                error = result?.ErrorMessage ?? "Authentication failed"
            });
        }

        _logger.LogInformation(" UNIFIED LOGIN SUCCESS - Email: {Email} | Context: {Context} | UserType: {UserType}",
            request.Email, context, result.User?.UserType);

        // Log successful login based on context
        if (context == AuthContext.Admin && result.User != null)
        {
            await _auditLoggingService.LogAdminActivityAsync(
                result.User.Id,
                "admin_login",
                $"Admin login successful for email: {request.Email}",
                ipAddress ?? "Unknown",
                userAgent ?? "Unknown"
            );
        }
        else if (result.User != null)
        {
            await _auditLoggingService.LogUserActivityAsync(
                result.User.Id,
                "login",
                $"User login successful for email: {request.Email}",
                ipAddress ?? "Unknown",
                userAgent ?? "Unknown"
            );
        }

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    /// <summary>
    /// Universal token refresh endpoint
    /// Replaces both customer and admin refresh endpoints
    /// </summary>
    /// <param name="request">Refresh token request with context</param>
    /// <returns>New token pair</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        _logger.LogInformation(" UNIFIED REFRESH ATTEMPT - Context: {Context}", request.Context);

        var result = await _authService.RefreshTokenAsync(
            request.RefreshToken,
            request.Context,
            ipAddress,
            userAgent);

        if (result == null || !result.Success)
        {
            _logger.LogWarning(" UNIFIED REFRESH FAILED - Context: {Context}", request.Context);
            return Unauthorized(new { error = result?.ErrorMessage ?? "Token refresh failed" });
        }

        _logger.LogInformation(" UNIFIED REFRESH SUCCESS - Context: {Context}", request.Context);

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    /// <summary>
    /// Universal logout endpoint
    /// Works for all user types
    /// </summary>
    /// <param name="request">Logout request</param>
    /// <returns>Logout result</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unable to identify user" });
        }

        var context = GetCurrentUserContext(); // Helper method to determine context from token

        _logger.LogInformation(" UNIFIED LOGOUT ATTEMPT - UserId: {UserId} | Context: {Context}",
            userId, context);

        var result = await _authService.LogoutAsync(userId.Value, context, request?.RefreshToken);

        if (result)
        {
            _logger.LogInformation(" UNIFIED LOGOUT SUCCESS - UserId: {UserId}", userId);

            // Log successful logout based on context
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            if (context == AuthContext.Admin)
            {
                await _auditLoggingService.LogAdminActivityAsync(
                    userId.Value,
                    "admin_logout",
                    $"Admin logout successful for userId: {userId}",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown"
                );
            }
            else
            {
                await _auditLoggingService.LogUserActivityAsync(
                    userId.Value,
                    "logout",
                    $"User logout successful for userId: {userId}",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown"
                );
            }

            return Ok(new { success = true, message = "Logged out successfully" });
        }
        else
        {
            _logger.LogWarning(" UNIFIED LOGOUT FAILED - UserId: {UserId}", userId);
            return BadRequest(new { error = "Logout failed" });
        }
    }

    /// <summary>
    /// Get current user profile
    /// Context-aware profile retrieval
    /// </summary>
    /// <returns>User profile</returns>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unable to identify user" });
        }

        _logger.LogInformation(" UNIFIED PROFILE REQUEST - UserId: {UserId}", userId);

        var profile = await _authService.GetUserProfileAsync(userId.Value);

        if (profile == null)
        {
            _logger.LogWarning(" UNIFIED PROFILE NOT FOUND - UserId: {UserId}", userId);
            return NotFound(new { error = "User profile not found" });
        }

        _logger.LogInformation(" UNIFIED PROFILE SUCCESS - UserId: {UserId} | UserType: {UserType}",
            userId, profile.UserType);

        return Ok(new
        {
            success = true,
            data = profile
        });
    }

    /// <summary>
    /// Check user permission
    /// Unified permission checking for all user types
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>Permission status</returns>
    [HttpGet("check-permission")]
    [Authorize]
    public async Task<IActionResult> CheckPermission([FromQuery, Required] string permission)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unable to identify user" });
        }

        var hasPermission = await _authService.HasPermissionAsync(userId.Value, permission);

        return Ok(new
        {
            success = true,
            data = new { hasPermission, permission, userId = userId.Value }
        });
    }

    /// <summary>
    /// Get user permissions
    /// Primarily for admin users, returns empty for customers
    /// </summary>
    /// <returns>User permissions</returns>
    [HttpGet("permissions")]
    [Authorize]
    public async Task<IActionResult> GetPermissions()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unable to identify user" });
        }

        var permissions = await _authService.GetUserPermissionsAsync(userId.Value);

        return Ok(new
        {
            success = true,
            data = permissions
        });
    }

    /// <summary>
    /// Universal registration endpoint
    /// Replaces /api/auth/register
    /// </summary>
    /// <param name="request">Unified registration request with context</param>
    /// <returns>Registration result with tokens</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UnifiedRegisterRequest request)
    {
        // Default to Customer context if not specified
        var context = request.Context ?? AuthContext.Customer;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        request.IpAddress = ipAddress;
        request.UserAgent = userAgent;

        _logger.LogInformation(" UNIFIED REGISTER ATTEMPT - Email: {Email} | Context: {Context} | IP: {IP}",
            request.Email, context, ipAddress);

        var result = await _authService.RegisterAsync(request);

        if (result == null || !result.Success)
        {
            _logger.LogWarning(" UNIFIED REGISTER FAILED - Email: {Email} | Context: {Context}",
                request.Email, context);

            return BadRequest(new
            {
                success = false,
                error = result?.ErrorMessage ?? "Registration failed"
            });
        }

        _logger.LogInformation(" UNIFIED REGISTER SUCCESS - Email: {Email} | Context: {Context} | UserType: {UserType}",
            request.Email, context, result.User?.UserType);

        // Log successful registration
        if (result.User != null)
        {
            if (context == AuthContext.Admin)
            {
                await _auditLoggingService.LogAdminActivityAsync(
                    result.User.Id,
                    "admin_register",
                    $"Admin registration successful for email: {request.Email}",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown"
                );
            }
            else
            {
                await _auditLoggingService.LogUserActivityAsync(
                    result.User.Id,
                    "register",
                    $"User registration successful for email: {request.Email}",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown"
                );
            }
        }

        return Ok(new
        {
            success = true,
            data = result
        });
    }

    /// <summary>
    /// Universal change password endpoint
    /// </summary>
    /// <param name="request">Change password request with context</param>
    /// <returns>Change password result</returns>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("PasswordChangePolicy")]
    public async Task<IActionResult> ChangePassword([FromBody] UnifiedChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unable to identify user" });
        }

        _logger.LogInformation(" UNIFIED CHANGE PASSWORD - UserId: {UserId}", userId);

        // Use the existing method that takes userId directly
        var success = await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);

        if (success)
        {
            _logger.LogInformation(" UNIFIED CHANGE PASSWORD SUCCESS - UserId: {UserId}", userId);

            // Log successful password change
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var context = GetCurrentUserContext();

            if (context == AuthContext.Admin)
            {
                await _auditLoggingService.LogAdminActivityAsync(
                    userId.Value,
                    "password_change",
                    $"Admin password change successful for userId: {userId}",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown"
                );
            }
            else
            {
                await _auditLoggingService.LogUserActivityAsync(
                    userId.Value,
                    "password_change",
                    $"User password change successful for userId: {userId}",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown"
                );
            }

            return Ok(new { success = true, message = "Password changed successfully" });
        }
        else
        {
            _logger.LogWarning(" UNIFIED CHANGE PASSWORD FAILED - UserId: {UserId}", userId);

            // Log failed password change as security event
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditLoggingService.LogSecurityEventAsync(
                "failed_password_change",
                $"Failed password change attempt for userId: {userId}",
                ipAddress ?? "Unknown",
                userId: userId.Value,
                correlationId: Guid.NewGuid().ToString()
            );

            return BadRequest(new { success = false, error = "Failed to change password" });
        }
    }

    /// <summary>
    /// Universal forgot password endpoint
    /// </summary>
    /// <param name="request">Forgot password request with context</param>
    /// <returns>Forgot password result</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] UnifiedForgotPasswordRequest request)
    {
        _logger.LogInformation(" UNIFIED FORGOT PASSWORD - Email: {Email}", request.Email);

        var (success, message) = await _authService.ForgotPasswordAsync(request);

        return Ok(new { success = true, message }); // Always return success for security
    }

    /// <summary>
    /// Universal reset password endpoint
    /// </summary>
    /// <param name="request">Reset password request with context</param>
    /// <returns>Reset password result</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] UnifiedResetPasswordRequest request)
    {
        // Extract IP address and user agent for security logging
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

        // Set IP address in request
        request.IpAddress = ipAddress;

        _logger.LogInformation(" UNIFIED RESET PASSWORD - Email: {Email}, Token: {Token}, IP: {IP}",
            request.Email, request.Token, ipAddress);

        var (success, message) = await _authService.ResetPasswordAsync(request);

        if (success)
        {
            _logger.LogInformation(" UNIFIED RESET PASSWORD SUCCESS - Email: {Email}, IP: {IP}",
                request.Email, ipAddress);
            return Ok(new { success = true, message });
        }
        else
        {
            _logger.LogWarning(" UNIFIED RESET PASSWORD FAILED - Email: {Email}, Message: {Message}, IP: {IP}",
                request.Email, message, ipAddress);
            return BadRequest(new { success = false, error = message });
        }
    }

    /// <summary>
    /// Confirm email address (no token yet; email link identifies by email only)
    /// </summary>
    /// <param name="email">Email to confirm</param>
    /// <returns>Confirmation result</returns>
    /// <summary>
    /// Confirm email address using secure token
    /// </summary>
    /// <param name="token">Email confirmation token</param>
    /// <returns>Confirmation result</returns>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery, Required] string token)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var (success, message) = await _authService.ConfirmEmailWithTokenAsync(token, ipAddress, userAgent);

        if (success)
        {
            _logger.LogInformation(" Email confirmed successfully from IP {IP}", ipAddress);
            return Ok(new { success = true, message });
        }
        else
        {
            _logger.LogWarning(" Email confirmation failed from IP {IP}: {Message}", ipAddress, message);
            return BadRequest(new { success = false, message });
        }
    }

    /// <summary>
    /// Legacy email confirmation endpoint (deprecated - use token-based confirmation)
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>Confirmation result</returns>
    [HttpGet("confirm-email-legacy")]
    [AllowAnonymous]
    [Obsolete("Use /confirm-email with token parameter for secure confirmation")]
    public async Task<IActionResult> ConfirmEmailLegacy([FromQuery, Required] string email)
    {
        var success = await _authService.ConfirmEmailAsync(email);
        return Ok(new { success });
    }

    /// <summary>
    /// Resend email confirmation with new secure token
    /// </summary>
    /// <param name="request">Email address to resend confirmation</param>
    /// <returns>Resend confirmation result</returns>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { success = false, message = "Email address is required" });
        }

        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers["User-Agent"].ToString();

        // Use AuthService to handle resend logic (it has access to all needed services)
        var result = await _authService.ResendEmailConfirmationAsync(request.Email, ipAddress, userAgent);

        if (result.Success)
        {
            _logger.LogInformation(" Email confirmation resent for email {Email} from IP {IP}", request.Email, ipAddress);
            return Ok(new { success = true, message = result.Message });
        }
        else
        {
            _logger.LogWarning(" Failed to resend confirmation for email {Email} from IP {IP}: {Message}",
                request.Email, ipAddress, result.Message);
            return StatusCode(500, new { success = false, message = result.Message });
        }
    }

    /// <summary>
    /// Get current user profile (alternative endpoint for /me)
    /// </summary>
    /// <returns>User profile</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "Unable to identify user" });
        }

        _logger.LogInformation(" ME REQUEST - UserId: {UserId}", userId);

        var profile = await _authService.GetUserProfileAsync(userId.Value);

        if (profile == null)
        {
            _logger.LogWarning("  ME NOT FOUND - UserId: {UserId}", userId);
            return NotFound(new { error = "User profile not found" });
        }

        _logger.LogInformation("  ME SUCCESS - UserId: {UserId} | UserType: {UserType}",
            userId, profile.UserType);

        return Ok(new
        {
            success = true,
            data = profile
        });
    }

    // =====================================================
    // Helper Methods
    // =====================================================

    /// <summary>
    /// Determines authentication context from current token
    /// </summary>
    private AuthContext GetCurrentUserContext()
    {
        var contextClaim = User.FindFirst("context")?.Value;
        return Enum.TryParse<AuthContext>(contextClaim, out var context) ? context : AuthContext.Customer;
    }
}

/// <summary>
/// Refresh token request DTO for unified auth
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public AuthContext Context { get; set; } = AuthContext.Customer;
}

/// <summary>
/// Logout request DTO
/// </summary>
public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

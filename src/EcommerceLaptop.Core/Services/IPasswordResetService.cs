using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Password reset service interface implementing OWASP security standards
/// Handles secure password reset token generation, validation, and management
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Initiates password reset process with rate limiting and security checks
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="ipAddress">Client IP address for security auditing</param>
    /// <param name="userAgent">Client user agent for security auditing</param>
    /// <returns>Password reset result</returns>
    Task<PasswordResetResult> InitiatePasswordResetAsync(string email, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Validates password reset token
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="token">Password reset token</param>
    /// <returns>Token validation result</returns>
    Task<TokenValidationResult> ValidateResetTokenAsync(string email, string token);

    /// <summary>
    /// Confirms password reset using valid token
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="token">Password reset token</param>
    /// <param name="newPassword">New password</param>
    /// <param name="ipAddress">Client IP address for security auditing</param>
    /// <returns>Password reset confirmation result</returns>
    Task<PasswordResetConfirmationResult> ConfirmPasswordResetAsync(string email, string token, string newPassword, string? ipAddress = null);

    /// <summary>
    /// Cleans up expired password reset tokens
    /// Should be called by a background service
    /// </summary>
    /// <returns>Number of expired tokens cleaned up</returns>
    Task<int> CleanupExpiredTokensAsync();

    /// <summary>
    /// Checks if user has exceeded password reset request limit
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <returns>Rate limit check result</returns>
    Task<RateLimitResult> CheckRateLimitAsync(string email, string? ipAddress = null);

    /// <summary>
    /// Invalidates all existing password reset tokens for a user
    /// Used when password is changed or for security reasons
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Number of tokens invalidated</returns>
    Task<int> InvalidateUserTokensAsync(int userId);

    /// <summary>
    /// Gets password reset statistics for monitoring
    /// </summary>
    /// <param name="fromDate">Start date for statistics</param>
    /// <param name="toDate">End date for statistics</param>
    /// <returns>Password reset statistics</returns>
    Task<PasswordResetStatistics> GetStatisticsAsync(DateTime fromDate, DateTime toDate);
}

/// <summary>
/// Result of password reset initiation
/// </summary>
public class PasswordResetResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public bool EmailSent { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? RemainingAttempts { get; set; }
    public TimeSpan? CooldownPeriod { get; set; }
}

/// <summary>
/// Result of token validation
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MinutesRemaining { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// Result of password reset confirmation
/// </summary>
public class PasswordResetConfirmationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public DateTime ResetAt { get; set; }
    public bool NotificationSent { get; set; }
}

/// <summary>
/// Rate limiting check result
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RemainingAttempts { get; set; }
    public TimeSpan? CooldownPeriod { get; set; }
    public DateTime? NextAllowedAt { get; set; }
}

/// <summary>
/// Password reset statistics
/// </summary>
public class PasswordResetStatistics
{
    public int TotalRequests { get; set; }
    public int SuccessfulResets { get; set; }
    public int ExpiredTokens { get; set; }
    public int InvalidAttempts { get; set; }
    public int RateLimitedRequests { get; set; }
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulResets / TotalRequests * 100 : 0;
    public Dictionary<string, int> RequestsByDay { get; set; } = new();
    public Dictionary<string, int> TopRequestIPs { get; set; } = new();
}
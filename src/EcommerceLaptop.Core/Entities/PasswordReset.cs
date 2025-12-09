namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Password reset entity for secure token management
/// Implements OWASP security standards for password reset functionality
/// </summary>
public class PasswordReset
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to User table
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Secure random token for password reset validation
    /// This is stored as plain text but should be cryptographically secure
    /// </summary>
    public string ResetToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration timestamp (UTC)
    /// Recommended: 15-30 minutes for security
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been used
    /// Prevents token reuse attacks
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// When the password reset request was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// IP address of the password reset request
    /// For security auditing and suspicious activity detection
    /// </summary>
    public string? RequestIpAddress { get; set; }

    /// <summary>
    /// User agent string from the password reset request
    /// For security auditing purposes
    /// </summary>
    public string? RequestUserAgent { get; set; }

    /// <summary>
    /// When the token was actually used for password reset (UTC)
    /// Null if not yet used
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// IP address when the token was used for password reset
    /// For security auditing
    /// </summary>
    public string? UsedIpAddress { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
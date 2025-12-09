using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Secure email confirmation token entity with built-in security measures
/// </summary>
public class EmailConfirmationToken
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// User ID this token belongs to
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Cryptographically secure random token (256-bit)
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Email address this token is for (for additional validation)
    /// </summary>
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time (default: 24 hours)
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been used (one-time use only)
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// IP address from which the token was generated (for security logging)
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? GeneratedFromIp { get; set; }

    /// <summary>
    /// User agent from which the token was generated
    /// </summary>
    [MaxLength(500)]
    public string? GeneratedFromUserAgent { get; set; }

    /// <summary>
    /// When the token was used (null if not used)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// IP address from which the token was used
    /// </summary>
    [MaxLength(45)]
    public string? UsedFromIp { get; set; }

    /// <summary>
    /// User agent from which the token was used
    /// </summary>
    [MaxLength(500)]
    public string? UsedFromUserAgent { get; set; }

    /// <summary>
    /// When this token was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this token was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to User
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Check if token is valid (not expired, not used)
    /// </summary>
    public bool IsValid => !IsUsed && ExpiresAt > DateTime.UtcNow;

    /// <summary>
    /// Check if token is expired
    /// </summary>
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
}
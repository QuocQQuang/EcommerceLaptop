using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Refresh token entity for secure token rotation
/// Implements OAuth 2.0 refresh token best practices
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    [StringLength(500)]
    public string? ReplacedByToken { get; set; }

    [StringLength(1000)]
    public string? RevokedReason { get; set; }

    [StringLength(45)]
    public string? CreatedByIp { get; set; }

    [StringLength(45)]
    public string? RevokedByIp { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}

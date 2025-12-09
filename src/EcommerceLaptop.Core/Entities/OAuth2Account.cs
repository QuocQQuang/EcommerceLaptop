using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// OAuth2 account linking entity for social authentication
/// Links local user accounts with external OAuth2 providers
/// </summary>
public class OAuth2Account
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Provider { get; set; } = string.Empty; // Google, Facebook, etc.

    [Required]
    [StringLength(255)]
    public string ProviderUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Represents a shopping cart session for guest users (non-authenticated)
/// </summary>
public class CartSession
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique session identifier for guest users
    /// </summary>
    [Required]
    [StringLength(128)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Browser fingerprint for additional session validation
    /// </summary>
    [StringLength(64)]
    public string? BrowserFingerprint { get; set; }

    /// <summary>
    /// IP address of the guest user
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Collection of items in the session cart
    /// </summary>
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    /// <summary>
    /// When the session was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the session was last accessed
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the session expires
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

    /// <summary>
    /// Whether the session is still active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the session has been migrated to a user account
    /// </summary>
    public bool IsMigrated { get; set; } = false;

    /// <summary>
    /// User ID if session was migrated to an account
    /// </summary>
    public string? MigratedToUserId { get; set; }

    /// <summary>
    /// When the session was migrated
    /// </summary>
    public DateTime? MigratedAt { get; set; }

    /// <summary>
    /// Optional discount code applied to the session cart
    /// </summary>
    [StringLength(50)]
    public string? DiscountCode { get; set; }

    /// <summary>
    /// Discount amount applied (if any)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    /// <summary>
    /// Tax amount calculated for the session cart
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; } = 0;

    /// <summary>
    /// Shipping cost for the session cart
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal ShippingCost { get; set; } = 0;

    /// <summary>
    /// Total amount including tax and shipping
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; } = 0;

    /// <summary>
    /// Subtotal before tax, shipping, and discounts
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal SubTotal { get; set; } = 0;

    /// <summary>
    /// Check if session is expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Calculated properties for convenient access
    /// </summary>
    [NotMapped]
    public int ItemCount => CartItems.Sum(item => item.Quantity);

    [NotMapped]
    public decimal ItemsTotal => CartItems.Sum(item => item.TotalPrice);

    /// <summary>
    /// Update last accessed time and extend expiration
    /// </summary>
    public void UpdateLastAccessed()
    {
        LastAccessedAt = DateTime.UtcNow;
        if (!IsMigrated && IsActive)
        {
            ExpiresAt = DateTime.UtcNow.AddDays(30); // Extend session
        }
    }

    /// <summary>
    /// Mark session as migrated to user account
    /// </summary>
    public void MarkAsMigrated(string userId)
    {
        IsMigrated = true;
        MigratedToUserId = userId;
        MigratedAt = DateTime.UtcNow;
        IsActive = false;
    }
}

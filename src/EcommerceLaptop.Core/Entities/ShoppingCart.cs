using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Represents a shopping cart for authenticated users
/// </summary>
public class ShoppingCart
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User who owns this cart
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Collection of items in the cart
    /// </summary>
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    /// <summary>
    /// When the cart was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the cart was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the cart is active or has been processed
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional discount code applied to the cart
    /// </summary>
    public string? DiscountCode { get; set; }

    /// <summary>
    /// Discount amount applied (if any)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    /// <summary>
    /// Tax amount calculated for the cart
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; } = 0;

    /// <summary>
    /// Shipping cost for the cart
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
    /// Calculated properties for convenient access
    /// </summary>
    [NotMapped]
    public int ItemCount => CartItems.Sum(item => item.Quantity);

    [NotMapped]
    public decimal ItemsTotal => CartItems.Sum(item => item.TotalPrice);
}

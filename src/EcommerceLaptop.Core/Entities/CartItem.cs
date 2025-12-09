using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Represents an individual item in a shopping cart
/// </summary>
public class CartItem
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Shopping cart this item belongs to (for authenticated users)
    /// </summary>
    public int? ShoppingCartId { get; set; }

    /// <summary>
    /// Navigation property to the shopping cart
    /// </summary>
    public virtual ShoppingCart? ShoppingCart { get; set; }

    /// <summary>
    /// Cart session this item belongs to (for guest users)
    /// </summary>
    public int? CartSessionId { get; set; }

    /// <summary>
    /// Navigation property to the cart session
    /// </summary>
    public virtual CartSession? CartSession { get; set; }

    /// <summary>
    /// Product being added to cart
    /// </summary>
    [Required]
    public int ProductId { get; set; }

    /// <summary>
    /// Navigation property to the product
    /// </summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>
    /// Quantity of the product
    /// </summary>
    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Unit price at the time of adding to cart (for price history)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total price for this line item (UnitPrice * Quantity)
    /// </summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Discount applied to this specific item
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal ItemDiscount { get; set; } = 0;

    /// <summary>
    /// When the item was added to cart
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the item was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional configuration options (JSON stored as string)
    /// </summary>
    public string? ConfigurationOptions { get; set; }

    /// <summary>
    /// Whether this item is part of a bundle
    /// </summary>
    public bool IsBundle { get; set; } = false;

    /// <summary>
    /// Parent bundle item ID if this is part of a bundle
    /// </summary>
    public int? ParentBundleItemId { get; set; }

    /// <summary>
    /// Navigation property to parent bundle item
    /// </summary>
    public virtual CartItem? ParentBundleItem { get; set; }

    /// <summary>
    /// Child items if this is a bundle
    /// </summary>
    public virtual ICollection<CartItem> BundleItems { get; set; } = new List<CartItem>();

    /// <summary>
    /// Calculate total with any item-specific discounts
    /// </summary>
    [NotMapped]
    public decimal FinalPrice => TotalPrice - ItemDiscount;
}

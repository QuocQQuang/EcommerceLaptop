using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.DTOs.Cart;

/// <summary>
/// DTO for adding items to cart
/// </summary>
public class AddToCartDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Product ID must be a positive number")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Optional configuration options for the product (e.g., color, storage)
    /// </summary>
    public Dictionary<string, string>? ConfigurationOptions { get; set; }

    /// <summary>
    /// Session ID for guest users
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Whether to replace existing item or add to quantity
    /// </summary>
    public bool ReplaceIfExists { get; set; } = false;

    /// <summary>
    /// Bundle items if adding a bundle product
    /// </summary>
    public List<BundleItemDto>? BundleItems { get; set; }
}

/// <summary>
/// DTO for bundle items within a cart item
/// </summary>
public class BundleItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    public Dictionary<string, string>? ConfigurationOptions { get; set; }
}

/// <summary>
/// DTO for updating cart item quantity
/// </summary>
public class UpdateCartItemDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Cart Item ID must be a positive number")]
    public int CartItemId { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Quantity must be between 0 and 100")]
    public int Quantity { get; set; }

    /// <summary>
    /// Updated configuration options
    /// </summary>
    public Dictionary<string, string>? ConfigurationOptions { get; set; }

    /// <summary>
    /// Session ID for guest users
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// DTO for removing items from cart
/// </summary>
public class RemoveFromCartDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Cart Item ID must be a positive number")]
    public int CartItemId { get; set; }

    /// <summary>
    /// Session ID for guest users
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// DTO for applying discount codes
/// </summary>
public class ApplyDiscountDto
{
    [Required]
    [StringLength(50, ErrorMessage = "Discount code cannot exceed 50 characters")]
    public string DiscountCode { get; set; } = string.Empty;

    /// <summary>
    /// Session ID for guest users
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// DTO for cart item in responses
/// </summary>
public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string Brand { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal ItemDiscount { get; set; }
    public decimal FinalPrice { get; set; }
    public Dictionary<string, string>? ConfigurationOptions { get; set; }
    public bool IsBundle { get; set; }
    public List<CartItemDto>? BundleItems { get; set; }
    public DateTime AddedAt { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int StockQuantity { get; set; }
    public string? UnavailabilityReason { get; set; }
}

/// <summary>
/// DTO for cart summary information
/// </summary>
public class CartSummaryDto
{
    public int ItemCount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalAmount { get; set; }
    public string? DiscountCode { get; set; }
    public bool HasUnavailableItems { get; set; }
    public List<string> Warnings { get; set; } = new List<string>();
}

/// <summary>
/// Complete cart response DTO
/// </summary>
public class CartResponseDto
{
    public int? CartId { get; set; }
    public string? SessionId { get; set; }
    public string? UserId { get; set; }
    public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    public CartSummaryDto Summary { get; set; } = new CartSummaryDto();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsGuest { get; set; }
}

/// <summary>
/// DTO for cart migration from session to user
/// </summary>
public class MigrateCartDto
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether to merge with existing user cart or replace
    /// </summary>
    public bool MergeWithExisting { get; set; } = true;
}

/// <summary>
/// DTO for bulk cart operations
/// </summary>
public class BulkCartOperationDto
{
    public List<AddToCartDto>? ItemsToAdd { get; set; }
    public List<UpdateCartItemDto>? ItemsToUpdate { get; set; }
    public List<int>? ItemIdsToRemove { get; set; }
    public string? SessionId { get; set; }
}

/// <summary>
/// DTO for cart validation results
/// </summary>
public class CartValidationDto
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public List<CartItemValidationDto> ItemValidations { get; set; } = new List<CartItemValidationDto>();
}

/// <summary>
/// DTO for individual cart item validation
/// </summary>
public class CartItemValidationDto
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public bool IsValid { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public bool HasPriceChanged { get; set; } = false;
    public decimal CurrentPrice { get; set; }
    public decimal CartPrice { get; set; }
    public int AvailableQuantity { get; set; }
    public int RequestedQuantity { get; set; }
    public List<string> Issues { get; set; } = new List<string>();
}

/// <summary>
/// DTO for shipping options calculation request
/// </summary>
public class ShippingCalculationRequest
{
    [Required]
    public string? UserId { get; set; }
    
    public string? SessionId { get; set; }
    
    [Required]
    public ShippingAddressDto ShippingAddress { get; set; } = null!;
    
    public bool IncludeCodOptions { get; set; } = true;
    
    public string? PreferredProvider { get; set; }
}

/// <summary>
/// DTO for shipping address
/// </summary>
public class ShippingAddressDto
{
    [Required]
    public string Street { get; set; } = string.Empty;
    
    [Required]
    public string Ward { get; set; } = string.Empty;
    
    [Required] 
    public string District { get; set; } = string.Empty;
    
    [Required]
    public string Province { get; set; } = string.Empty;
    
    public string Country { get; set; } = "Vietnam";
    
    public string? ReceiverName { get; set; }
    
    public string? ReceiverPhone { get; set; }
}

/// <summary>
/// DTO for shipping options response
/// </summary>
public class ShippingOptionsResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ShippingOptionDto> Options { get; set; } = new List<ShippingOptionDto>();
    public ShippingAddressDto? ValidatedAddress { get; set; }
    public List<string> AddressValidationWarnings { get; set; } = new List<string>();
}

/// <summary>
/// DTO for individual shipping option
/// </summary>
public class ShippingOptionDto
{
    public string Provider { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public decimal CodFee { get; set; }
    public decimal TotalFee { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
    public string DeliveryTimeFrame { get; set; } = string.Empty;
    public bool SupportsCod { get; set; }
    public decimal MaxCodAmount { get; set; }
    public bool IsRecommended { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> ProviderData { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// DTO for basic shipping cost calculation (legacy support)
/// </summary>
public class CalculateShippingRequest
{
    public string? SessionId { get; set; }
    
    [Required]
    public string ShippingAddress { get; set; } = string.Empty;
}

using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

public interface ICartValidationService
{
    /// <summary>
    /// Validates an entire cart including all items and business rules
    /// </summary>
    Task<CartValidationDto> ValidateCartAsync(List<CartItem> cartItems, string? userId = null);

    /// <summary>
    /// Validates a single cart item
    /// </summary>
    Task<CartItemValidationDto> ValidateCartItemAsync(CartItem cartItem);

    /// <summary>
    /// Validates inventory availability for a product
    /// </summary>
    Task<bool> ValidateInventoryAsync(int productId, int requestedQuantity);

    /// <summary>
    /// Validates if a product is available for purchase
    /// </summary>
    Task<bool> ValidateProductAvailabilityAsync(int productId);

    /// <summary>
    /// Validates if the cart price matches the current product price
    /// </summary>
    Task<bool> ValidatePricingAsync(int productId, decimal cartPrice);

    /// <summary>
    /// Validates quantity limits for a product
    /// </summary>
    Task<bool> ValidateQuantityLimitsAsync(int productId, int quantity, string? userId = null);

    /// <summary>
    /// Validates a discount code
    /// </summary>
    Task<bool> ValidateDiscountCodeAsync(string discountCode, decimal subtotal, string? userId = null);

    /// <summary>
    /// Validates business rules for cart items
    /// </summary>
    Task<List<string>> ValidateBusinessRulesAsync(List<CartItem> cartItems, string? userId = null);

    /// <summary>
    /// Validates bundle compatibility
    /// </summary>
    Task<bool> ValidateBundleCompatibilityAsync(List<BundleItemDto> bundleItems);

    /// <summary>
    /// Validates user cart limits
    /// </summary>
    Task<bool> ValidateUserCartLimitsAsync(string userId, int additionalItems = 0);

    /// <summary>
    /// Validates session cart limits
    /// </summary>
    Task<bool> ValidateSessionCartLimitsAsync(string sessionId, int additionalItems = 0);
}

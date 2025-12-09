using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

public interface IPricingService
{
    /// <summary>
    /// Calculates the price for a specific item with quantity and configuration options
    /// </summary>
    Task<decimal> CalculateItemPriceAsync(int productId, int quantity, Dictionary<string, string>? configuration = null);

    /// <summary>
    /// Calculates the total price for a bundle of items
    /// </summary>
    Task<decimal> CalculateBundlePriceAsync(List<BundleItemDto> bundleItems);

    /// <summary>
    /// Calculates the discount amount for a given discount code
    /// </summary>
    Task<decimal> CalculateDiscountAsync(string discountCode, decimal subtotal, string? userId = null);

    /// <summary>
    /// Calculates the tax amount based on subtotal and shipping address
    /// </summary>
    Task<decimal> CalculateTaxAsync(decimal subtotal, string? shippingAddress = null);

    /// <summary>
    /// Calculates shipping cost based on subtotal, item count, and shipping address
    /// </summary>
    Task<decimal> CalculateShippingAsync(decimal subtotal, int itemCount, string? shippingAddress = null);

    /// <summary>
    /// Calculates a complete cart summary including all pricing components
    /// </summary>
    Task<CartSummaryDto> CalculateCartSummaryAsync(List<CartItem> cartItems, string? discountCode = null, string? shippingAddress = null);

    /// <summary>
    /// Validates if a discount code is valid for a specific user
    /// </summary>
    Task<bool> ValidateDiscountCodeAsync(string discountCode, string? userId = null);

    /// <summary>
    /// Gets the bundle discount percentage for a bundle product
    /// </summary>
    Task<decimal> GetBundleDiscountPercentageAsync(int bundleProductId);

    /// <summary>
    /// Calculates volume discount based on product and quantity
    /// </summary>
    Task<decimal> CalculateVolumeDiscountAsync(int productId, int quantity);

    /// <summary>
    /// Calculates seasonal discount for a product on a specific date
    /// </summary>
    Task<decimal> CalculateSeasonalDiscountAsync(int productId, DateTime purchaseDate);
}

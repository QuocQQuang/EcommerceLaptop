using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Service for advanced pricing calculations including discounts, taxes, bundles, and shipping
/// </summary>
public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PricingService> _logger;

    // Tax rates by region (in real implementation, this would come from configuration or database)
    private readonly Dictionary<string, decimal> _taxRates = new()
    {
        { "CA", 0.0875m },  // California
        { "NY", 0.08m },    // New York
        { "TX", 0.0625m },  // Texas
        { "FL", 0.06m },    // Florida
        { "DEFAULT", 0.085m } // Default rate
    };

    // Volume discount tiers
    private readonly Dictionary<int, decimal> _volumeDiscounts = new()
    {
        { 5, 0.02m },   // 2% discount for 5+ items
        { 10, 0.05m },  // 5% discount for 10+ items
        { 20, 0.08m },  // 8% discount for 20+ items
        { 50, 0.12m }   // 12% discount for 50+ items
    };

    public PricingService(ApplicationDbContext context, ILogger<PricingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculate item price including any configuration-based pricing
    /// </summary>
    public async Task<decimal> CalculateItemPriceAsync(int productId, int quantity, Dictionary<string, string>? configuration = null)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {productId} not found");
            }

            var basePrice = product.Price;

            // Apply configuration-based price adjustments
            if (configuration != null)
            {
                foreach (var config in configuration)
                {
                    basePrice += GetConfigurationPriceAdjustment(config.Key, config.Value);
                }
            }

            // Apply volume discount
            var volumeDiscount = await CalculateVolumeDiscountAsync(productId, quantity);
            var discountedPrice = basePrice * (1 - volumeDiscount);

            // Apply seasonal discount
            var seasonalDiscount = await CalculateSeasonalDiscountAsync(productId, DateTime.UtcNow);
            discountedPrice *= (1 - seasonalDiscount);

            return discountedPrice * quantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating item price for product {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Calculate bundle price with bundle-specific discounts
    /// </summary>
    public async Task<decimal> CalculateBundlePriceAsync(List<BundleItemDto> bundleItems)
    {
        try
        {
            decimal totalPrice = 0;

            foreach (var item in bundleItems)
            {
                var itemPrice = await CalculateItemPriceAsync(item.ProductId, item.Quantity, item.ConfigurationOptions);
                totalPrice += itemPrice;
            }

            // Apply bundle discount (typically 10-15% off individual prices)
            var bundleDiscountPercentage = 0.12m; // 12% bundle discount
            var bundleDiscount = totalPrice * bundleDiscountPercentage;

            return totalPrice - bundleDiscount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bundle price for {ItemCount} items", bundleItems.Count);
            throw;
        }
    }

    /// <summary>
    /// Calculate discount amount based on discount code
    /// </summary>
    public async Task<decimal> CalculateDiscountAsync(string discountCode, decimal subtotal, string? userId = null)
    {
        try
        {
            // Check if discount code exists in database
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == discountCode && 
                                         c.IsActive && 
                                         c.ValidFrom <= DateTime.UtcNow && 
                                         c.ValidTo >= DateTime.UtcNow);

            if (coupon != null)
            {
                // Check usage limits
                if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
                {
                    return 0;
                }

                // Check minimum order amount
                if (subtotal < coupon.MinimumOrderAmount)
                {
                    return 0;
                }

                decimal discount = 0;

                if (coupon.Type == CouponType.Percentage)
                {
                    discount = subtotal * (coupon.Value / 100);
                }
                else if (coupon.Type == CouponType.FixedAmount)
                {
                    discount = coupon.Value;
                }

                // Apply maximum discount amount limit
                if (coupon.MaximumDiscountAmount > 0 && discount > coupon.MaximumDiscountAmount)
                {
                    discount = coupon.MaximumDiscountAmount;
                }

                return discount;
            }

            // Handle hardcoded discount codes for demonstration
            var discountPercentage = discountCode.ToUpper() switch
            {
                "SAVE10" => 0.10m,
                "SAVE20" => 0.20m,
                "WELCOME15" => 0.15m,
                "STUDENT" => 0.25m,
                "NEWCUSTOMER" => 0.30m,
                "BULK25" when subtotal >= 1000 => 0.25m,
                "VIP40" when await IsVipCustomerAsync(userId) => 0.40m,
                _ => 0m
            };

            return subtotal * discountPercentage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating discount for code {DiscountCode}", discountCode);
            return 0;
        }
    }

    /// <summary>
    /// Calculate tax amount based on subtotal and shipping address
    /// </summary>
    public async Task<decimal> CalculateTaxAsync(decimal subtotal, string? shippingAddress = null)
    {
        try
        {
            var taxRate = GetTaxRateForAddress(shippingAddress);
            return subtotal * taxRate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating tax for address {ShippingAddress}", shippingAddress);
            return subtotal * _taxRates["DEFAULT"];
        }
    }

    /// <summary>
    /// Calculate shipping cost based on cart details
    /// </summary>
    public async Task<decimal> CalculateShippingAsync(decimal subtotal, int itemCount, string? shippingAddress = null)
    {
        try
        {
            // Free shipping thresholds
            if (subtotal >= 100)
            {
                return 0; // Free shipping over $100
            }

            // Base shipping rates
            decimal baseShipping = 9.99m;

            // Additional cost for multiple items
            if (itemCount > 3)
            {
                baseShipping += (itemCount - 3) * 2.50m;
            }

            // Express shipping zones (higher cost for certain areas)
            if (IsExpressShippingZone(shippingAddress))
            {
                baseShipping += 5.00m;
            }

            // Reduced shipping for medium orders
            if (subtotal >= 50)
            {
                baseShipping = Math.Max(baseShipping * 0.6m, 5.99m);
            }

            return baseShipping;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating shipping for subtotal {Subtotal} and {ItemCount} items", subtotal, itemCount);
            return 9.99m; // Default shipping cost
        }
    }

    /// <summary>
    /// Calculate complete cart summary with all pricing components
    /// </summary>
    public async Task<CartSummaryDto> CalculateCartSummaryAsync(List<CartItem> cartItems, string? discountCode = null, string? shippingAddress = null)
    {
        try
        {
            var summary = new CartSummaryDto
            {
                ItemCount = cartItems.Sum(ci => ci.Quantity)
            };

            // Calculate subtotal
            summary.SubTotal = cartItems.Sum(ci => ci.TotalPrice);

            // Apply discount
            if (!string.IsNullOrEmpty(discountCode))
            {
                summary.DiscountAmount = await CalculateDiscountAsync(discountCode, summary.SubTotal);
            }

            // Calculate tax on discounted amount
            var taxableAmount = summary.SubTotal - summary.DiscountAmount;
            summary.TaxAmount = await CalculateTaxAsync(taxableAmount, shippingAddress);

            // Calculate shipping
            summary.ShippingCost = await CalculateShippingAsync(summary.SubTotal, summary.ItemCount, shippingAddress);

            // Calculate total
            summary.TotalAmount = summary.SubTotal - summary.DiscountAmount + summary.TaxAmount + summary.ShippingCost;

            // Add warnings for pricing considerations
            summary.Warnings = GeneratePricingWarnings(summary, cartItems);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cart summary for {ItemCount} items", cartItems.Count);
            throw;
        }
    }

    /// <summary>
    /// Validate if discount code is valid and applicable
    /// </summary>
    public async Task<bool> ValidateDiscountCodeAsync(string discountCode, string? userId = null)
    {
        try
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == discountCode && 
                                         c.IsActive && 
                                         c.ValidFrom <= DateTime.UtcNow && 
                                         c.ValidTo >= DateTime.UtcNow);

            if (coupon != null)
            {
                return coupon.UsageLimit == 0 || coupon.UsedCount < coupon.UsageLimit;
            }

            // Check hardcoded discount codes
            var validCodes = new[] { "SAVE10", "SAVE20", "WELCOME15", "STUDENT", "NEWCUSTOMER", "BULK25", "VIP40" };
            return validCodes.Contains(discountCode.ToUpper());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating discount code {DiscountCode}", discountCode);
            return false;
        }
    }

    /// <summary>
    /// Get bundle discount percentage for a specific bundle product
    /// </summary>
    public async Task<decimal> GetBundleDiscountPercentageAsync(int bundleProductId)
    {
        try
        {
            var bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.Id == bundleProductId);

            if (bundle != null)
            {
                return bundle.DiscountPercentage / 100m;
            }

            return 0.12m; // Default 12% bundle discount
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bundle discount for product {ProductId}", bundleProductId);
            return 0.12m;
        }
    }

    /// <summary>
    /// Calculate volume discount based on quantity
    /// </summary>
    public async Task<decimal> CalculateVolumeDiscountAsync(int productId, int quantity)
    {
        try
        {
            foreach (var tier in _volumeDiscounts.OrderByDescending(x => x.Key))
            {
                if (quantity >= tier.Key)
                {
                    return tier.Value;
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating volume discount for product {ProductId} with quantity {Quantity}", productId, quantity);
            return 0;
        }
    }

    /// <summary>
    /// Calculate seasonal discount (e.g., holiday sales, back-to-school)
    /// </summary>
    public async Task<decimal> CalculateSeasonalDiscountAsync(int productId, DateTime purchaseDate)
    {
        try
        {
            var month = purchaseDate.Month;

            // Back to school season (August - September)
            if (month >= 8 && month <= 9)
            {
                return 0.15m; // 15% discount
            }

            // Holiday season (November - December)
            if (month >= 11 && month <= 12)
            {
                return 0.20m; // 20% discount
            }

            // New Year clearance (January)
            if (month == 1)
            {
                return 0.25m; // 25% discount
            }

            // Summer sale (June - July)
            if (month >= 6 && month <= 7)
            {
                return 0.10m; // 10% discount
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating seasonal discount for product {ProductId}", productId);
            return 0;
        }
    }

    #region Private Helper Methods

    private decimal GetConfigurationPriceAdjustment(string configKey, string configValue)
    {
        return configKey.ToLower() switch
        {
            "storage" => configValue switch
            {
                "512GB" => 100m,
                "1TB" => 200m,
                "2TB" => 400m,
                _ => 0m
            },
            "ram" => configValue switch
            {
                "16GB" => 150m,
                "32GB" => 300m,
                "64GB" => 600m,
                _ => 0m
            },
            "warranty" => configValue switch
            {
                "2Year" => 99m,
                "3Year" => 199m,
                "4Year" => 299m,
                _ => 0m
            },
            "color" => configValue switch
            {
                "Custom" => 50m,
                "Premium" => 100m,
                _ => 0m
            },
            _ => 0m
        };
    }

    private decimal GetTaxRateForAddress(string? shippingAddress)
    {
        if (string.IsNullOrEmpty(shippingAddress))
        {
            return _taxRates["DEFAULT"];
        }

        // Simple state extraction (in real implementation, use proper address parsing)
        var upperAddress = shippingAddress.ToUpper();
        foreach (var state in _taxRates.Keys.Where(k => k != "DEFAULT"))
        {
            if (upperAddress.Contains(state))
            {
                return _taxRates[state];
            }
        }

        return _taxRates["DEFAULT"];
    }

    private bool IsExpressShippingZone(string? shippingAddress)
    {
        if (string.IsNullOrEmpty(shippingAddress))
        {
            return false;
        }

        var expressZones = new[] { "AK", "HI", "PR", "GU", "VI" }; // Alaska, Hawaii, territories
        var upperAddress = shippingAddress.ToUpper();
        return expressZones.Any(zone => upperAddress.Contains(zone));
    }

    private async Task<bool> IsVipCustomerAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
        {
            return false;
        }

        try
        {
            // Check if user has VIP status (simplified check)
            var orderCount = await _context.Orders
                .CountAsync(o => o.UserId == userIdInt && o.Status == OrderStatus.Delivered);

            var totalSpent = await _context.Orders
                .Where(o => o.UserId == userIdInt && o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalAmount);

            return orderCount >= 10 || totalSpent >= 5000; // VIP if 10+ orders or $5000+ spent
        }
        catch
        {
            return false;
        }
    }

    private List<string> GeneratePricingWarnings(CartSummaryDto summary, List<CartItem> cartItems)
    {
        var warnings = new List<string>();

        // Check for price changes
        foreach (var item in cartItems)
        {
            if (item.Product.Price != item.UnitPrice)
            {
                warnings.Add($"Price has changed for {item.Product.Name}");
            }
        }

        // Check for stock issues
        foreach (var item in cartItems)
        {
            if (item.Product.Inventory.AvailableQuantity < item.Quantity)
            {
                warnings.Add($"Limited stock available for {item.Product.Name}");
            }
        }

        // Suggest free shipping threshold
        if (summary.ShippingCost > 0 && summary.SubTotal >= 75)
        {
            var needed = 100 - summary.SubTotal;
            warnings.Add($"Add ${needed:F2} more for free shipping!");
        }

        // Suggest volume discounts
        var totalItems = cartItems.Sum(ci => ci.Quantity);
        if (totalItems >= 3 && totalItems < 5)
        {
            warnings.Add("Add 2 more items to qualify for volume discount!");
        }

        return warnings;
    }

    #endregion
}

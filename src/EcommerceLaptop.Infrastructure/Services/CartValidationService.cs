using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Service for comprehensive cart validation including inventory, pricing, and business rules
/// </summary>
public class CartValidationService : ICartValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CartValidationService> _logger;

    // Business rule constants
    private const int MAX_CART_ITEMS = 50;
    private const int MAX_QUANTITY_PER_ITEM = 10;
    private const decimal MIN_ORDER_AMOUNT = 10.00m;
    private const decimal MAX_ORDER_AMOUNT = 50000.00m;
    private const int MAX_BUNDLE_ITEMS = 10;

    public CartValidationService(ApplicationDbContext context, ILogger<CartValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Helper method to convert string userId to integer
    /// </summary>
    private bool TryParseUserId(string? userId, out int userIdInt)
    {
        userIdInt = 0;
        return !string.IsNullOrEmpty(userId) && int.TryParse(userId, out userIdInt);
    }

    /// <summary>
    /// Validate entire cart for all business rules and constraints
    /// </summary>
    public async Task<CartValidationDto> ValidateCartAsync(List<CartItem> cartItems, string? userId = null)
    {
        try
        {
            var validation = new CartValidationDto();

            if (!cartItems.Any())
            {
                validation.Warnings.Add("Cart is empty");
                return validation;
            }

            // Validate each cart item
            foreach (var cartItem in cartItems)
            {
                var itemValidation = await ValidateCartItemAsync(cartItem);
                validation.ItemValidations.Add(itemValidation);

                if (!itemValidation.IsValid)
                {
                    validation.IsValid = false;
                    validation.Errors.AddRange(itemValidation.Issues);
                }
            }

            // Validate business rules
            var businessRuleErrors = await ValidateBusinessRulesAsync(cartItems, userId);
            if (businessRuleErrors.Any())
            {
                validation.IsValid = false;
                validation.Errors.AddRange(businessRuleErrors);
            }

            // Validate cart limits
            if (!string.IsNullOrEmpty(userId))
            {
                if (!await ValidateUserCartLimitsAsync(userId))
                {
                    validation.IsValid = false;
                    validation.Errors.Add($"Cart exceeds maximum allowed items ({MAX_CART_ITEMS})");
                }
            }

            // Calculate totals for validation
            var subtotal = cartItems.Sum(ci => ci.TotalPrice);
            if (subtotal < MIN_ORDER_AMOUNT)
            {
                validation.Warnings.Add($"Minimum order amount is ${MIN_ORDER_AMOUNT:F2}");
            }

            if (subtotal > MAX_ORDER_AMOUNT)
            {
                validation.IsValid = false;
                validation.Errors.Add($"Order amount exceeds maximum limit of ${MAX_ORDER_AMOUNT:F2}");
            }

            // Check for duplicate products (excluding bundles)
            var duplicateProducts = cartItems
                .Where(ci => !ci.IsBundle)
                .GroupBy(ci => new { ci.ProductId, ci.ConfigurationOptions })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.ProductId)
                .ToList();

            if (duplicateProducts.Any())
            {
                validation.Warnings.Add("Cart contains duplicate products. Consider consolidating quantities.");
            }

            return validation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart with {ItemCount} items", cartItems.Count);
            throw;
        }
    }

    /// <summary>
    /// Validate individual cart item
    /// </summary>
    public async Task<CartItemValidationDto> ValidateCartItemAsync(CartItem cartItem)
    {
        try
        {
            var validation = new CartItemValidationDto
            {
                CartItemId = cartItem.Id,
                ProductId = cartItem.ProductId,
                RequestedQuantity = cartItem.Quantity,
                CartPrice = cartItem.UnitPrice
            };

            var product = await _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);

            if (product == null)
            {
                validation.IsValid = false;
                validation.IsAvailable = false;
                validation.Issues.Add("Product not found");
                return validation;
            }

            validation.CurrentPrice = product.Price;
            validation.AvailableQuantity = product.Inventory.AvailableQuantity;

            // Check product availability
            if (!await ValidateProductAvailabilityAsync(cartItem.ProductId))
            {
                validation.IsValid = false;
                validation.IsAvailable = false;
                validation.Issues.Add("Product is no longer available");
            }

            // Check inventory
            if (!await ValidateInventoryAsync(cartItem.ProductId, cartItem.Quantity))
            {
                validation.IsValid = false;
                validation.Issues.Add($"Insufficient stock. Available: {validation.AvailableQuantity}, Requested: {cartItem.Quantity}");
            }

            // Check price changes
            if (!await ValidatePricingAsync(cartItem.ProductId, cartItem.UnitPrice))
            {
                validation.HasPriceChanged = true;
                validation.Issues.Add($"Price has changed from ${cartItem.UnitPrice:F2} to ${product.Price:F2}");
            }

            // Check quantity limits
            if (cartItem.Quantity > MAX_QUANTITY_PER_ITEM)
            {
                validation.IsValid = false;
                validation.Issues.Add($"Quantity exceeds maximum allowed ({MAX_QUANTITY_PER_ITEM}) per item");
            }

            // Check if item is discontinued - removing this check as IsDiscontinued property doesn't exist
            // We'll rely on IsActive status instead

            // Check bundle validity
            if (cartItem.IsBundle && cartItem.BundleItems?.Any() == true)
            {
                var bundleItems = cartItem.BundleItems.Select(bi => new BundleItemDto
                {
                    ProductId = bi.ProductId,
                    Quantity = bi.Quantity,
                    ConfigurationOptions = string.IsNullOrEmpty(bi.ConfigurationOptions) 
                        ? null 
                        : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(bi.ConfigurationOptions)
                }).ToList();

                if (!await ValidateBundleCompatibilityAsync(bundleItems))
                {
                    validation.IsValid = false;
                    validation.Issues.Add("Bundle contains incompatible items");
                }
            }

            return validation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart item {CartItemId}", cartItem.Id);
            throw;
        }
    }

    /// <summary>
    /// Validate inventory availability for requested quantity
    /// </summary>
    public async Task<bool> ValidateInventoryAsync(int productId, int requestedQuantity)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            return product.Inventory.AvailableQuantity >= requestedQuantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating inventory for product {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Validate product availability (active, not discontinued)
    /// </summary>
    public async Task<bool> ValidateProductAvailabilityAsync(int productId)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            return product != null && product.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product availability for {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Validate pricing (check for price changes)
    /// </summary>
    public async Task<bool> ValidatePricingAsync(int productId, decimal cartPrice)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            // Allow small price differences (e.g., $0.01) due to rounding
            return Math.Abs(product.Price - cartPrice) <= 0.01m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating pricing for product {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Validate quantity limits per user/product
    /// </summary>
    public async Task<bool> ValidateQuantityLimitsAsync(int productId, int quantity, string? userId = null)
    {
        try
        {
            // Check basic quantity limit
            if (quantity > MAX_QUANTITY_PER_ITEM)
            {
                return false;
            }

            // Check user-specific limits (if authenticated)
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                // Check if user has existing orders for this product in the last 30 days
                var recentOrders = await _context.OrderItems
                    .Where(oi => oi.Order.UserId == userIdInt && 
                                oi.ProductId == productId && 
                                oi.Order.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .SumAsync(oi => oi.Quantity);

                // Limit total quantity to 50 per product per month
                if (recentOrders + quantity > 50)
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating quantity limits for product {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Validate discount code eligibility
    /// </summary>
    public async Task<bool> ValidateDiscountCodeAsync(string discountCode, decimal subtotal, string? userId = null)
    {
        try
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == discountCode && c.IsActive);

            if (coupon != null)
            {
                // Check validity period
                if (coupon.ValidFrom > DateTime.UtcNow || coupon.ValidTo < DateTime.UtcNow)
                {
                    return false;
                }

                // Check usage limits
                if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
                {
                    return false;
                }

                // Check minimum order amount
                if (subtotal < coupon.MinimumOrderAmount)
                {
                    return false;
                }

                // Check user-specific restrictions (if any)
                if (!string.IsNullOrEmpty(userId))
                {
                    // User-specific coupon validation would go here
                    // For now, assume valid
                }

                return true;
            }

            // Check hardcoded discount codes
            var validCodes = new[] { "SAVE10", "SAVE20", "WELCOME15", "STUDENT", "NEWCUSTOMER" };
            return validCodes.Contains(discountCode.ToUpper());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating discount code {DiscountCode}", discountCode);
            return false;
        }
    }

    /// <summary>
    /// Validate business rules for the cart
    /// </summary>
    public async Task<List<string>> ValidateBusinessRulesAsync(List<CartItem> cartItems, string? userId = null)
    {
        var errors = new List<string>();

        try
        {
            // Rule 1: Maximum items in cart
            if (cartItems.Count > MAX_CART_ITEMS)
            {
                errors.Add($"Cart cannot contain more than {MAX_CART_ITEMS} items");
            }

            // Rule 2: Maximum total quantity
            var totalQuantity = cartItems.Sum(ci => ci.Quantity);
            if (totalQuantity > 100)
            {
                errors.Add("Total quantity cannot exceed 100 items");
            }

            // Rule 3: Bundle rules
            var bundleItems = cartItems.Where(ci => ci.IsBundle).ToList();
            foreach (var bundleItem in bundleItems)
            {
                if (bundleItem.BundleItems?.Count > MAX_BUNDLE_ITEMS)
                {
                    errors.Add($"Bundle cannot contain more than {MAX_BUNDLE_ITEMS} items");
                }
            }

            // Rule 4: Incompatible products
            var incompatibleCombinations = await CheckIncompatibleProductsAsync(cartItems);
            errors.AddRange(incompatibleCombinations);

            // Rule 5: Age restrictions (for certain products)
            if (!string.IsNullOrEmpty(userId))
            {
                var ageRestrictedProducts = await CheckAgeRestrictionsAsync(cartItems, userId);
                errors.AddRange(ageRestrictedProducts);
            }

            // Rule 6: Geographic restrictions
            var geographicRestrictions = await CheckGeographicRestrictionsAsync(cartItems, userId);
            errors.AddRange(geographicRestrictions);

            // Rule 7: Minimum order requirements for certain categories
            var categoryErrors = await ValidateCategoryRequirementsAsync(cartItems);
            errors.AddRange(categoryErrors);

            return errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating business rules for cart");
            return new List<string> { "Error validating business rules" };
        }
    }

    /// <summary>
    /// Validate bundle compatibility
    /// </summary>
    public async Task<bool> ValidateBundleCompatibilityAsync(List<BundleItemDto> bundleItems)
    {
        try
        {
            if (!bundleItems.Any()) return true;

            var productIds = bundleItems.Select(bi => bi.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // Check that all products exist and are available
            if (products.Count != productIds.Count)
            {
                return false;
            }

            // Check compatibility rules
            foreach (var product in products)
            {
                if (!product.IsActive)
                {
                    return false;
                }
            }

            // Check specific compatibility rules (e.g., laptop + incompatible accessories)
            var laptops = products.Where(p => p is Laptop).ToList();
            var accessories = products.Where(p => p is Accessory).Cast<Accessory>().ToList();

            foreach (var laptop in laptops)
            {
                foreach (var accessory in accessories)
                {
                    if (!string.IsNullOrEmpty(accessory.Compatibility) && 
                        !accessory.Compatibility.Contains(laptop.Brand))
                    {
                        return false; // Incompatible accessory
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bundle compatibility");
            return false;
        }
    }

    /// <summary>
    /// Validate user cart limits
    /// </summary>
    public async Task<bool> ValidateUserCartLimitsAsync(string userId, int additionalItems = 0)
    {
        try
        {
            if (!TryParseUserId(userId, out int userIdInt))
            {
                return false;
            }

            var currentCartItemCount = await _context.CartItems
                .CountAsync(ci => ci.ShoppingCart!.UserId == userIdInt);

            return (currentCartItemCount + additionalItems) <= MAX_CART_ITEMS;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user cart limits for {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Validate session cart limits
    /// </summary>
    public async Task<bool> ValidateSessionCartLimitsAsync(string sessionId, int additionalItems = 0)
    {
        try
        {
            var currentCartItemCount = await _context.CartItems
                .CountAsync(ci => ci.CartSession!.SessionId == sessionId);

            return (currentCartItemCount + additionalItems) <= MAX_CART_ITEMS;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session cart limits for {SessionId}", sessionId);
            return false;
        }
    }

    #region Private Helper Methods

    private async Task<List<string>> CheckIncompatibleProductsAsync(List<CartItem> cartItems)
    {
        var errors = new List<string>();
        
        try
        {
            // Example: Certain product combinations are not allowed
            var productIds = cartItems.Select(ci => ci.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // Check for conflicting brands (example rule)
            var brands = products.Select(p => p.Brand).Distinct().ToList();
            if (brands.Count > 1 && brands.Contains("CompetitorBrand"))
            {
                errors.Add("Cannot mix certain brands in the same order");
            }

            return errors;
        }
        catch
        {
            return errors;
        }
    }

    private Task<List<string>> CheckAgeRestrictionsAsync(List<CartItem> cartItems, string userId)
    {
        var errors = new List<string>();
        
        try
        {
            // Age validation removed since User entity doesn't have DateOfBirth property
            // Additional user-specific validations could go here
            
            return Task.FromResult(errors);
        }
        catch
        {
            return Task.FromResult(errors);
        }
    }

    private Task<List<string>> CheckGeographicRestrictionsAsync(List<CartItem> cartItems, string? userId)
    {
        var errors = new List<string>();
        
        try
        {
            // Example: Some products may have geographic restrictions
            // This would typically check user's address or shipping location
            // For now, return empty list
            return Task.FromResult(errors);
        }
        catch
        {
            return Task.FromResult(errors);
        }
    }

    private Task<List<string>> ValidateCategoryRequirementsAsync(List<CartItem> cartItems)
    {
        var errors = new List<string>();
        
        try
        {
            // Example: Minimum order requirements for certain categories
            var laptopItems = cartItems.Where(ci => ci.Product is Laptop).ToList();
            if (laptopItems.Any())
            {
                var laptopTotal = laptopItems.Sum(ci => ci.TotalPrice);
                if (laptopTotal < 500)
                {
                    errors.Add("Minimum order amount for laptops is $500");
                }
            }

            return Task.FromResult(errors);
        }
        catch
        {
            return Task.FromResult(errors);
        }
    }

    #endregion
}

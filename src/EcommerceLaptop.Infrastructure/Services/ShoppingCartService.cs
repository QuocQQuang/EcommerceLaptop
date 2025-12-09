using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Service for managing shopping cart operations for both authenticated and guest users
/// </summary>
public class ShoppingCartService : IShoppingCartService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShoppingCartService> _logger;

    public ShoppingCartService(
        ApplicationDbContext context, 
        ILogger<ShoppingCartService> logger)
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
    /// Get cart for authenticated user or guest session
    /// </summary>
    public async Task<CartResponseDto> GetCartAsync(string? userId, string? sessionId)
    {
        try
        {
            if (!string.IsNullOrEmpty(userId))
            {
                return await GetUserCartAsync(userId);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                return await GetSessionCartAsync(sessionId);
            }

            return new CartResponseDto { IsGuest = string.IsNullOrEmpty(userId) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for user {UserId} or session {SessionId}", userId, sessionId);
            throw;
        }
    }

    /// <summary>
    /// Add item to cart (user or session)
    /// </summary>
    public async Task<CartResponseDto> AddToCartAsync(AddToCartDto addToCartDto, string? userId)
    {
        try
        {
            // Validate product exists and is available
            var product = await _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == addToCartDto.ProductId);

            if (product == null)
            {
                throw new ArgumentException($"Product with ID {addToCartDto.ProductId} not found");
            }

            if (!product.IsActive)
            {
                throw new InvalidOperationException($"Product {product.Name} is not available");
            }

            if (product.Inventory.AvailableQuantity < addToCartDto.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock. Available: {product.Inventory.AvailableQuantity}, Requested: {addToCartDto.Quantity}");
            }

            if (!string.IsNullOrEmpty(userId))
            {
                return await AddToUserCartAsync(addToCartDto, userId, product);
            }
            else if (!string.IsNullOrEmpty(addToCartDto.SessionId))
            {
                return await AddToSessionCartAsync(addToCartDto, product);
            }

            throw new ArgumentException("Either userId or sessionId must be provided");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart: {@AddToCartDto}", addToCartDto);
            throw;
        }
    }

    /// <summary>
    /// Update cart item quantity or configuration
    /// </summary>
    public async Task<CartResponseDto> UpdateCartItemAsync(UpdateCartItemDto updateCartDto, string? userId)
    {
        try
        {
            CartItem? cartItem = null;

            if (!string.IsNullOrEmpty(userId) && TryParseUserId(userId, out int userIdInt))
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.ShoppingCart)
                    .FirstOrDefaultAsync(ci => ci.Id == updateCartDto.CartItemId && 
                                             ci.ShoppingCart!.UserId == userIdInt);
            }
            else if (!string.IsNullOrEmpty(updateCartDto.SessionId))
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.CartSession)
                    .FirstOrDefaultAsync(ci => ci.Id == updateCartDto.CartItemId && 
                                             ci.CartSession!.SessionId == updateCartDto.SessionId);
            }

            if (cartItem == null)
            {
                throw new ArgumentException("Cart item not found");
            }

            if (updateCartDto.Quantity == 0)
            {
                // Remove item if quantity is 0
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                // Validate stock
                if (cartItem.Product.Inventory.AvailableQuantity < updateCartDto.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock. Available: {cartItem.Product.Inventory.AvailableQuantity}, Requested: {updateCartDto.Quantity}");
                }

                // Update item
                cartItem.Quantity = updateCartDto.Quantity;
                cartItem.TotalPrice = cartItem.UnitPrice * updateCartDto.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;

                if (updateCartDto.ConfigurationOptions != null)
                {
                    cartItem.ConfigurationOptions = JsonSerializer.Serialize(updateCartDto.ConfigurationOptions);
                }

                _context.CartItems.Update(cartItem);
            }

            await _context.SaveChangesAsync();

            // Recalculate cart totals
            await RecalculateCartTotalsAsync(userId, updateCartDto.SessionId);

            return await GetCartAsync(userId, updateCartDto.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item: {@UpdateCartDto}", updateCartDto);
            throw;
        }
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    public async Task<CartResponseDto> RemoveFromCartAsync(RemoveFromCartDto removeFromCartDto, string? userId)
    {
        try
        {
            CartItem? cartItem = null;

            if (!string.IsNullOrEmpty(userId) && TryParseUserId(userId, out int userIdInt))
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.ShoppingCart)
                    .FirstOrDefaultAsync(ci => ci.Id == removeFromCartDto.CartItemId && 
                                             ci.ShoppingCart!.UserId == userIdInt);
            }
            else if (!string.IsNullOrEmpty(removeFromCartDto.SessionId))
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.CartSession)
                    .FirstOrDefaultAsync(ci => ci.Id == removeFromCartDto.CartItemId && 
                                             ci.CartSession!.SessionId == removeFromCartDto.SessionId);
            }

            if (cartItem == null)
            {
                throw new ArgumentException("Cart item not found");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            // Recalculate cart totals
            await RecalculateCartTotalsAsync(userId, removeFromCartDto.SessionId);

            return await GetCartAsync(userId, removeFromCartDto.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item: {@RemoveFromCartDto}", removeFromCartDto);
            throw;
        }
    }

    /// <summary>
    /// Clear entire cart
    /// </summary>
    public async Task<CartResponseDto> ClearCartAsync(string? userId, string? sessionId)
    {
        try
        {
            if (!string.IsNullOrEmpty(userId) && TryParseUserId(userId, out int userIdInt))
            {
                var cart = await _context.ShoppingCarts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.IsActive);

                if (cart != null)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    cart.SubTotal = 0;
                    cart.TotalAmount = 0;
                    cart.DiscountAmount = 0;
                    cart.TaxAmount = 0;
                    cart.ShippingCost = 0;
                    cart.DiscountCode = null;
                    cart.UpdatedAt = DateTime.UtcNow;
                    _context.ShoppingCarts.Update(cart);
                }
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                var cartSession = await _context.CartSessions
                    .Include(cs => cs.CartItems)
                    .FirstOrDefaultAsync(cs => cs.SessionId == sessionId && cs.IsActive);

                if (cartSession != null)
                {
                    _context.CartItems.RemoveRange(cartSession.CartItems);
                    cartSession.SubTotal = 0;
                    cartSession.TotalAmount = 0;
                    cartSession.DiscountAmount = 0;
                    cartSession.TaxAmount = 0;
                    cartSession.ShippingCost = 0;
                    cartSession.DiscountCode = null;
                    cartSession.UpdateLastAccessed();
                    _context.CartSessions.Update(cartSession);
                }
            }

            await _context.SaveChangesAsync();
            return await GetCartAsync(userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user {UserId} or session {SessionId}", userId, sessionId);
            throw;
        }
    }

    /// <summary>
    /// Apply discount code to cart
    /// </summary>
    public async Task<CartResponseDto> ApplyDiscountAsync(ApplyDiscountDto discountDto, string? userId)
    {
        try
        {
            // Here you would typically validate the discount code against a discounts table
            // For now, we'll simulate some basic discount codes
            decimal discountPercentage = discountDto.DiscountCode.ToUpper() switch
            {
                "SAVE10" => 0.10m,
                "SAVE20" => 0.20m,
                "WELCOME15" => 0.15m,
                "STUDENT" => 0.25m,
                _ => 0m
            };

            if (discountPercentage == 0)
            {
                throw new ArgumentException("Invalid discount code");
            }

            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await GetOrCreateUserCartAsync(userId);
                cart.DiscountCode = discountDto.DiscountCode;
                _context.ShoppingCarts.Update(cart);
            }
            else if (!string.IsNullOrEmpty(discountDto.SessionId))
            {
                var cartSession = await GetOrCreateSessionCartAsync(discountDto.SessionId);
                cartSession.DiscountCode = discountDto.DiscountCode;
                _context.CartSessions.Update(cartSession);
            }

            await _context.SaveChangesAsync();
            await RecalculateCartTotalsAsync(userId, discountDto.SessionId);

            return await GetCartAsync(userId, discountDto.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount: {@DiscountDto}", discountDto);
            throw;
        }
    }

    /// <summary>
    /// Remove discount code from cart
    /// </summary>
    public async Task<CartResponseDto> RemoveDiscountAsync(string? userId, string? sessionId)
    {
        try
        {
            if (!string.IsNullOrEmpty(userId) && TryParseUserId(userId, out int userIdInt))
            {
                var cart = await _context.ShoppingCarts
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.IsActive);

                if (cart != null)
                {
                    cart.DiscountCode = null;
                    cart.DiscountAmount = 0;
                    _context.ShoppingCarts.Update(cart);
                }
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                var cartSession = await _context.CartSessions
                    .FirstOrDefaultAsync(cs => cs.SessionId == sessionId && cs.IsActive);

                if (cartSession != null)
                {
                    cartSession.DiscountCode = null;
                    cartSession.DiscountAmount = 0;
                    _context.CartSessions.Update(cartSession);
                }
            }

            await _context.SaveChangesAsync();
            await RecalculateCartTotalsAsync(userId, sessionId);

            return await GetCartAsync(userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing discount for user {UserId} or session {SessionId}", userId, sessionId);
            throw;
        }
    }

    /// <summary>
    /// Validate cart items for availability, pricing, and stock
    /// </summary>
    public async Task<CartValidationDto> ValidateCartAsync(string? userId, string? sessionId)
    {
        try
        {
            var validation = new CartValidationDto();
            var cartItems = new List<CartItem>();

            if (!string.IsNullOrEmpty(userId) && TryParseUserId(userId, out int userIdInt))
            {
                cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.ShoppingCart)
                    .Where(ci => ci.ShoppingCart!.UserId == userIdInt)
                    .ToListAsync();
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Include(ci => ci.CartSession)
                    .Where(ci => ci.CartSession!.SessionId == sessionId)
                    .ToListAsync();
            }

            foreach (var item in cartItems)
            {
                var itemValidation = new CartItemValidationDto
                {
                    CartItemId = item.Id,
                    ProductId = item.ProductId,
                    CurrentPrice = item.Product.Price,
                    CartPrice = item.UnitPrice,
                    AvailableQuantity = item.Product.Inventory.AvailableQuantity,
                    RequestedQuantity = item.Quantity
                };

                // Check availability
                if (!item.Product.IsActive)
                {
                    itemValidation.IsAvailable = false;
                    itemValidation.IsValid = false;
                    itemValidation.Issues.Add("Product is no longer available");
                }

                // Check stock
                if (item.Product.Inventory.AvailableQuantity < item.Quantity)
                {
                    itemValidation.IsValid = false;
                    itemValidation.Issues.Add($"Insufficient stock. Available: {item.Product.Inventory.AvailableQuantity}, Requested: {item.Quantity}");
                }

                // Check price changes
                if (item.Product.Price != item.UnitPrice)
                {
                    itemValidation.HasPriceChanged = true;
                    itemValidation.Issues.Add($"Price has changed from {item.UnitPrice:C} to {item.Product.Price:C}");
                }

                validation.ItemValidations.Add(itemValidation);

                if (!itemValidation.IsValid)
                {
                    validation.IsValid = false;
                    validation.Errors.AddRange(itemValidation.Issues);
                }
            }

            return validation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart for user {UserId} or session {SessionId}", userId, sessionId);
            throw;
        }
    }

    /// <summary>
    /// Migrate guest cart session to authenticated user
    /// </summary>
    public async Task<CartResponseDto> MigrateSessionCartToUserAsync(MigrateCartDto migrateDto)
    {
        try
        {
            var cartSession = await _context.CartSessions
                .Include(cs => cs.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(cs => cs.SessionId == migrateDto.SessionId && cs.IsActive);

            if (cartSession == null || !cartSession.CartItems.Any())
            {
                return await GetCartAsync(migrateDto.UserId, null);
            }

            var userCart = await GetOrCreateUserCartAsync(migrateDto.UserId);

            if (migrateDto.MergeWithExisting)
            {
                // Merge session cart items with existing user cart
                foreach (var sessionItem in cartSession.CartItems)
                {
                    var existingItem = await _context.CartItems
                        .FirstOrDefaultAsync(ci => ci.ShoppingCartId == userCart.Id && 
                                                   ci.ProductId == sessionItem.ProductId &&
                                                   ci.ConfigurationOptions == sessionItem.ConfigurationOptions);

                    if (existingItem != null)
                    {
                        // Update quantity
                        existingItem.Quantity += sessionItem.Quantity;
                        existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
                        existingItem.UpdatedAt = DateTime.UtcNow;
                        _context.CartItems.Update(existingItem);
                    }
                    else
                    {
                        // Add new item to user cart
                        var newCartItem = new CartItem
                        {
                            ShoppingCartId = userCart.Id,
                            ProductId = sessionItem.ProductId,
                            Quantity = sessionItem.Quantity,
                            UnitPrice = sessionItem.UnitPrice,
                            TotalPrice = sessionItem.TotalPrice,
                            ConfigurationOptions = sessionItem.ConfigurationOptions,
                            IsBundle = sessionItem.IsBundle,
                            AddedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.CartItems.Add(newCartItem);
                    }
                }
            }
            else
            {
                // Replace user cart with session cart
                var existingUserCartItems = await _context.CartItems
                    .Where(ci => ci.ShoppingCartId == userCart.Id)
                    .ToListAsync();

                _context.CartItems.RemoveRange(existingUserCartItems);

                foreach (var sessionItem in cartSession.CartItems)
                {
                    var newCartItem = new CartItem
                    {
                        ShoppingCartId = userCart.Id,
                        ProductId = sessionItem.ProductId,
                        Quantity = sessionItem.Quantity,
                        UnitPrice = sessionItem.UnitPrice,
                        TotalPrice = sessionItem.TotalPrice,
                        ConfigurationOptions = sessionItem.ConfigurationOptions,
                        IsBundle = sessionItem.IsBundle,
                        AddedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.CartItems.Add(newCartItem);
                }
            }

            // Mark session as migrated
            cartSession.MarkAsMigrated(migrateDto.UserId);
            _context.CartSessions.Update(cartSession);

            await _context.SaveChangesAsync();
            await RecalculateCartTotalsAsync(migrateDto.UserId, null);

            return await GetCartAsync(migrateDto.UserId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating cart: {@MigrateDto}", migrateDto);
            throw;
        }
    }

    /// <summary>
    /// Perform bulk cart operations
    /// </summary>
    public async Task<CartResponseDto> BulkCartOperationAsync(BulkCartOperationDto bulkOperation, string? userId)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Add items
                if (bulkOperation.ItemsToAdd?.Any() == true)
                {
                    foreach (var addItem in bulkOperation.ItemsToAdd)
                    {
                        await AddToCartAsync(addItem, userId);
                    }
                }

                // Update items
                if (bulkOperation.ItemsToUpdate?.Any() == true)
                {
                    foreach (var updateItem in bulkOperation.ItemsToUpdate)
                    {
                        await UpdateCartItemAsync(updateItem, userId);
                    }
                }

                // Remove items
                if (bulkOperation.ItemIdsToRemove?.Any() == true)
                {
                    foreach (var itemId in bulkOperation.ItemIdsToRemove)
                    {
                        await RemoveFromCartAsync(new RemoveFromCartDto 
                        { 
                            CartItemId = itemId, 
                            SessionId = bulkOperation.SessionId 
                        }, userId);
                    }
                }

                await transaction.CommitAsync();
                return await GetCartAsync(userId, bulkOperation.SessionId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk cart operation: {@BulkOperation}", bulkOperation);
            throw;
        }
    }

    /// <summary>
    /// Calculate shipping cost based on cart contents and address
    /// </summary>
    public async Task<decimal> CalculateShippingAsync(string? userId, string? sessionId, string shippingAddress)
    {
        try
        {
            // Validate inputs - return 0 for invalid inputs
            if ((string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId)) || 
                string.IsNullOrEmpty(shippingAddress))
            {
                return 0;
            }

            var cart = await GetCartAsync(userId, sessionId);
            
            // Return 0 if cart is empty
            if (!cart.Items.Any())
            {
                return 0;
            }
            
            // Simple shipping calculation - kept for backward compatibility
            if (cart.Summary.SubTotal >= 100)
            {
                return 0; // Free shipping over $100
            }
            else if (cart.Summary.SubTotal >= 50)
            {
                return 5.99m; // Reduced shipping
            }
            else
            {
                return 9.99m; // Standard shipping
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating shipping for user {UserId} or session {SessionId}", userId, sessionId);
            throw;
        }
    }

    /// <summary>
    /// Clean up expired guest cart sessions
    /// </summary>
    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _context.CartSessions
                .Include(cs => cs.CartItems)
                .Where(cs => cs.ExpiresAt < DateTime.UtcNow || !cs.IsActive)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                _context.CartItems.RemoveRange(session.CartItems);
                _context.CartSessions.Remove(session);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired cart sessions", expiredSessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
            throw;
        }
    }

    #region Private Helper Methods

    private async Task<CartResponseDto> GetUserCartAsync(string userId)
    {
        if (!TryParseUserId(userId, out int userIdInt))
        {
            return new CartResponseDto { UserId = userId, IsGuest = false };
        }

        var cart = await _context.ShoppingCarts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.BundleItems)
                    .ThenInclude(bi => bi.Product)
            .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.IsActive);

        if (cart == null)
        {
            return new CartResponseDto { UserId = userId, IsGuest = false };
        }

        return MapToCartResponseDto(cart, null, userId, false);
    }

    private async Task<CartResponseDto> GetSessionCartAsync(string sessionId)
    {
        var cartSession = await _context.CartSessions
            .Include(cs => cs.CartItems)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Images)
            .Include(cs => cs.CartItems)
                .ThenInclude(ci => ci.BundleItems)
                    .ThenInclude(bi => bi.Product)
            .FirstOrDefaultAsync(cs => cs.SessionId == sessionId && cs.IsActive);

        if (cartSession == null)
        {
            return new CartResponseDto { SessionId = sessionId, IsGuest = true };
        }

        cartSession.UpdateLastAccessed();
        _context.CartSessions.Update(cartSession);
        await _context.SaveChangesAsync();

        return MapToCartResponseDto(null, cartSession, null, true);
    }

    private async Task<CartResponseDto> AddToUserCartAsync(AddToCartDto addToCartDto, string userId, Product product)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var cart = await GetOrCreateUserCartAsync(userId);

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.ShoppingCartId == cart.Id && 
                                           ci.ProductId == addToCartDto.ProductId);

            var configJson = addToCartDto.ConfigurationOptions != null ? 
                JsonSerializer.Serialize(addToCartDto.ConfigurationOptions) : null;

            if (existingItem != null && existingItem.ConfigurationOptions == configJson)
            {
                if (addToCartDto.ReplaceIfExists)
                {
                    existingItem.Quantity = addToCartDto.Quantity;
                }
                else
                {
                    existingItem.Quantity += addToCartDto.Quantity;
                }
                
                existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                _context.CartItems.Update(existingItem);

                // If bundle, update child items quantities proportionally
                if (existingItem.IsBundle && addToCartDto.BundleItems?.Any() == true)
                {
                    await UpdateBundleChildItems(existingItem, addToCartDto.BundleItems, addToCartDto.Quantity);
                }
            }
            else
            {
                var cartItem = new CartItem
                {
                    ShoppingCartId = cart.Id,
                    ProductId = addToCartDto.ProductId,
                    Quantity = addToCartDto.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * addToCartDto.Quantity,
                    ConfigurationOptions = addToCartDto.ConfigurationOptions != null ? 
                        JsonSerializer.Serialize(addToCartDto.ConfigurationOptions) : null,
                    IsBundle = addToCartDto.BundleItems?.Any() == true,
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync(); // Save to get the ID

                // If this is a bundle, create child items for each bundle component
                if (addToCartDto.BundleItems?.Any() == true)
                {
                    await CreateBundleChildItems(cartItem.Id, addToCartDto.BundleItems, addToCartDto.Quantity, cart.Id, null);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await RecalculateCartTotalsAsync(userId, null);

            return await GetCartAsync(userId, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<CartResponseDto> AddToSessionCartAsync(AddToCartDto addToCartDto, Product product)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var cartSession = await GetOrCreateSessionCartAsync(addToCartDto.SessionId!);

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartSessionId == cartSession.Id && 
                                           ci.ProductId == addToCartDto.ProductId);

            var configJson = addToCartDto.ConfigurationOptions != null ? 
                JsonSerializer.Serialize(addToCartDto.ConfigurationOptions) : null;

            if (existingItem != null && existingItem.ConfigurationOptions == configJson)
            {
                if (addToCartDto.ReplaceIfExists)
                {
                    existingItem.Quantity = addToCartDto.Quantity;
                }
                else
                {
                    existingItem.Quantity += addToCartDto.Quantity;
                }
                
                existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                _context.CartItems.Update(existingItem);

                // If bundle, update child items quantities proportionally
                if (existingItem.IsBundle && addToCartDto.BundleItems?.Any() == true)
                {
                    await UpdateBundleChildItems(existingItem, addToCartDto.BundleItems, addToCartDto.Quantity);
                }
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartSessionId = cartSession.Id,
                    ProductId = addToCartDto.ProductId,
                    Quantity = addToCartDto.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * addToCartDto.Quantity,
                    ConfigurationOptions = addToCartDto.ConfigurationOptions != null ? 
                        JsonSerializer.Serialize(addToCartDto.ConfigurationOptions) : null,
                    IsBundle = addToCartDto.BundleItems?.Any() == true,
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync(); // Save to get the ID

                // If this is a bundle, create child items for each bundle component
                if (addToCartDto.BundleItems?.Any() == true)
                {
                    await CreateBundleChildItems(cartItem.Id, addToCartDto.BundleItems, addToCartDto.Quantity, null, cartSession.Id);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await RecalculateCartTotalsAsync(null, addToCartDto.SessionId);

            return await GetCartAsync(null, addToCartDto.SessionId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<ShoppingCart> GetOrCreateUserCartAsync(string userId)
    {
        if (!TryParseUserId(userId, out int userIdInt))
        {
            throw new ArgumentException("Invalid user ID format", nameof(userId));
        }

        var cart = await _context.ShoppingCarts
            .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.IsActive);

        if (cart == null)
        {
            cart = new ShoppingCart
            {
                UserId = userIdInt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

    private async Task<CartSession> GetOrCreateSessionCartAsync(string sessionId)
    {
        var cartSession = await _context.CartSessions
            .FirstOrDefaultAsync(cs => cs.SessionId == sessionId && cs.IsActive);

        if (cartSession == null)
        {
            cartSession = new CartSession
            {
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _context.CartSessions.Add(cartSession);
            await _context.SaveChangesAsync();
        }
        else
        {
            cartSession.UpdateLastAccessed();
            _context.CartSessions.Update(cartSession);
            await _context.SaveChangesAsync();
        }

        return cartSession;
    }

    private async Task RecalculateCartTotalsAsync(string? userId, string? sessionId)
    {
        if (!string.IsNullOrEmpty(userId) && TryParseUserId(userId, out int userIdInt))
        {
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.IsActive);

            if (cart != null)
            {
                cart.SubTotal = cart.CartItems.Sum(ci => ci.TotalPrice);
                
                // Apply discount
                if (!string.IsNullOrEmpty(cart.DiscountCode))
                {
                    cart.DiscountAmount = CalculateDiscount(cart.DiscountCode, cart.SubTotal);
                }

                // Calculate tax (8.5% example rate)
                cart.TaxAmount = (cart.SubTotal - cart.DiscountAmount) * 0.085m;

                // Calculate total
                cart.TotalAmount = cart.SubTotal - cart.DiscountAmount + cart.TaxAmount + cart.ShippingCost;
                cart.UpdatedAt = DateTime.UtcNow;

                _context.ShoppingCarts.Update(cart);
                await _context.SaveChangesAsync();
            }
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            var cartSession = await _context.CartSessions
                .Include(cs => cs.CartItems)
                .FirstOrDefaultAsync(cs => cs.SessionId == sessionId && cs.IsActive);

            if (cartSession != null)
            {
                cartSession.SubTotal = cartSession.CartItems.Sum(ci => ci.TotalPrice);
                
                // Apply discount
                if (!string.IsNullOrEmpty(cartSession.DiscountCode))
                {
                    cartSession.DiscountAmount = CalculateDiscount(cartSession.DiscountCode, cartSession.SubTotal);
                }

                // Calculate tax (8.5% example rate)
                cartSession.TaxAmount = (cartSession.SubTotal - cartSession.DiscountAmount) * 0.085m;

                // Calculate total
                cartSession.TotalAmount = cartSession.SubTotal - cartSession.DiscountAmount + cartSession.TaxAmount + cartSession.ShippingCost;
                cartSession.UpdateLastAccessed();

                _context.CartSessions.Update(cartSession);
                await _context.SaveChangesAsync();
            }
        }
    }

    private decimal CalculateDiscount(string discountCode, decimal subtotal)
    {
        var discountPercentage = discountCode.ToUpper() switch
        {
            "SAVE10" => 0.10m,
            "SAVE20" => 0.20m,
            "WELCOME15" => 0.15m,
            "STUDENT" => 0.25m,
            _ => 0m
        };

        return subtotal * discountPercentage;
    }

    private CartResponseDto MapToCartResponseDto(ShoppingCart? cart, CartSession? cartSession, string? userId, bool isGuest)
    {
        var response = new CartResponseDto
        {
            CartId = cart?.Id,
            SessionId = cartSession?.SessionId,
            UserId = userId,
            IsGuest = isGuest,
            CreatedAt = cart?.CreatedAt ?? cartSession?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = cart?.UpdatedAt ?? cartSession?.LastAccessedAt ?? DateTime.UtcNow,
            IsActive = cart?.IsActive ?? cartSession?.IsActive ?? true
        };

        var items = cart?.CartItems ?? cartSession?.CartItems ?? new List<CartItem>();

        // Filter to only show parent items (not child bundle items)
        var parentItems = items.Where(ci => ci.ParentBundleItemId == null).ToList();

        response.Items = parentItems.Select(ci => new CartItemDto
        {
            Id = ci.Id,
            ProductId = ci.ProductId,
            ProductName = ci.Product?.Name ?? "Unknown Product",
            ProductSku = ci.Product?.SKU ?? "UNKNOWN",
            ProductImageUrl = ci.Product?.Images?.FirstOrDefault()?.ImageUrl,
            Brand = ci.Product?.Brand ?? "Unknown Brand",
            Quantity = ci.Quantity,
            UnitPrice = ci.UnitPrice,
            TotalPrice = ci.TotalPrice,
            ItemDiscount = ci.ItemDiscount,
            FinalPrice = ci.FinalPrice,
            ConfigurationOptions = string.IsNullOrEmpty(ci.ConfigurationOptions) ? 
                null : JsonSerializer.Deserialize<Dictionary<string, string>>(ci.ConfigurationOptions),
            IsBundle = ci.IsBundle,
            BundleItems = ci.IsBundle ? items
                .Where(childItem => childItem.ParentBundleItemId == ci.Id)
                .Select(childItem => new CartItemDto
                {
                    Id = childItem.Id,
                    ProductId = childItem.ProductId,
                    ProductName = childItem.Product?.Name ?? "Unknown Product",
                    ProductSku = childItem.Product?.SKU ?? "UNKNOWN",
                    ProductImageUrl = childItem.Product?.Images?.FirstOrDefault()?.ImageUrl,
                    Brand = childItem.Product?.Brand ?? "Unknown Brand",
                    Quantity = childItem.Quantity,
                    UnitPrice = childItem.UnitPrice,
                    TotalPrice = childItem.TotalPrice,
                    ItemDiscount = childItem.ItemDiscount,
                    FinalPrice = childItem.FinalPrice,
                    IsBundle = false,
                    AddedAt = childItem.AddedAt,
                    IsAvailable = childItem.Product?.IsActive ?? false,
                    StockQuantity = childItem.Product?.Inventory?.AvailableQuantity ?? 0,
                    UnavailabilityReason = childItem.Product?.IsActive != true ? "Product is no longer available" : null
                }).ToList() : null,
            AddedAt = ci.AddedAt,
            IsAvailable = ci.Product?.IsActive ?? false,
            StockQuantity = ci.Product?.Inventory?.AvailableQuantity ?? 0,
            UnavailabilityReason = ci.Product?.IsActive != true ? "Product is no longer available" : null
        }).ToList();

        response.Summary = new CartSummaryDto
        {
            ItemCount = cart?.ItemCount ?? cartSession?.ItemCount ?? 0,
            SubTotal = cart?.SubTotal ?? cartSession?.SubTotal ?? 0,
            DiscountAmount = cart?.DiscountAmount ?? cartSession?.DiscountAmount ?? 0,
            TaxAmount = cart?.TaxAmount ?? cartSession?.TaxAmount ?? 0,
            ShippingCost = cart?.ShippingCost ?? cartSession?.ShippingCost ?? 0,
            TotalAmount = cart?.TotalAmount ?? cartSession?.TotalAmount ?? 0,
            DiscountCode = cart?.DiscountCode ?? cartSession?.DiscountCode,
            HasUnavailableItems = response.Items.Any(i => !i.IsAvailable)
        };

        return response;
    }

    /// <summary>
    /// Creates child CartItem records for each bundle component
    /// </summary>
    private async Task CreateBundleChildItems(int parentCartItemId, List<BundleItemDto> bundleItems, int parentQuantity, int? cartId, int? cartSessionId)
    {
        foreach (var bundleItem in bundleItems)
        {
            // Validate the bundle item product exists and is available
            var bundleProduct = await _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == bundleItem.ProductId);

            if (bundleProduct == null || !bundleProduct.IsActive)
            {
                _logger.LogWarning("Bundle item product {ProductId} not found or inactive", bundleItem.ProductId);
                continue;
            }

            var totalQuantityNeeded = bundleItem.Quantity * parentQuantity;
            if (bundleProduct.Inventory.AvailableQuantity < totalQuantityNeeded)
            {
                throw new InvalidOperationException($"Insufficient stock for bundle item {bundleProduct.Name}. Available: {bundleProduct.Inventory.AvailableQuantity}, Required: {totalQuantityNeeded}");
            }

            var childCartItem = new CartItem
            {
                ShoppingCartId = cartId,
                CartSessionId = cartSessionId,
                ProductId = bundleItem.ProductId,
                Quantity = totalQuantityNeeded,
                UnitPrice = bundleProduct.Price,
                TotalPrice = bundleProduct.Price * totalQuantityNeeded,
                ParentBundleItemId = parentCartItemId,
                IsBundle = false,
                AddedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CartItems.Add(childCartItem);
        }
    }

    /// <summary>
    /// Updates quantities of existing bundle child items
    /// </summary>
    private async Task UpdateBundleChildItems(CartItem parentItem, List<BundleItemDto> bundleItems, int newParentQuantity)
    {
        var existingChildItems = await _context.CartItems
            .Where(ci => ci.ParentBundleItemId == parentItem.Id)
            .ToListAsync();

        foreach (var bundleItem in bundleItems)
        {
            var existingChild = existingChildItems.FirstOrDefault(ci => ci.ProductId == bundleItem.ProductId);
            if (existingChild != null)
            {
                var newQuantity = bundleItem.Quantity * newParentQuantity;
                
                // Validate stock
                var product = await _context.Products
                    .Include(p => p.Inventory)
                    .FirstOrDefaultAsync(p => p.Id == bundleItem.ProductId);

                if (product != null && product.Inventory.AvailableQuantity < newQuantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for bundle item {product.Name}. Available: {product.Inventory.AvailableQuantity}, Required: {newQuantity}");
                }

                existingChild.Quantity = newQuantity;
                existingChild.TotalPrice = existingChild.UnitPrice * newQuantity;
                existingChild.UpdatedAt = DateTime.UtcNow;
                _context.CartItems.Update(existingChild);
            }
        }
    }

    #endregion
}

using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EcommerceLaptop.API.Features.Cart;

/// <summary>
/// Internal helper methods for Cart feature logic
/// </summary>
internal static class CartHelpers
{
    public static bool TryParseUserId(string? userId, out int userIdInt)
    {
        userIdInt = 0;
        return !string.IsNullOrEmpty(userId) && int.TryParse(userId, out userIdInt);
    }

    public static async Task<ShoppingCart> GetOrCreateUserCartAsync(ApplicationDbContext context, string userId)
    {
        if (!TryParseUserId(userId, out int userIdInt))
        {
            throw new ArgumentException("Invalid user ID format", nameof(userId));
        }

        var cart = await context.ShoppingCarts
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

            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();
        }

        return cart;
    }

    public static async Task<CartSession> GetOrCreateSessionCartAsync(ApplicationDbContext context, string sessionId)
    {
        var cartSession = await context.CartSessions
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

            context.CartSessions.Add(cartSession);
            await context.SaveChangesAsync();
        }
        else
        {
            cartSession.UpdateLastAccessed();
            context.CartSessions.Update(cartSession);
            await context.SaveChangesAsync();
        }

        return cartSession;
    }

    public static async Task RecalculateCartTotalsAsync(ApplicationDbContext context, string? userId, string? sessionId)
    {
        if (!string.IsNullOrEmpty(userId) && TryParseUserId(userId, out int userIdInt))
        {
            var cart = await context.ShoppingCarts
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

                context.ShoppingCarts.Update(cart);
                await context.SaveChangesAsync();
            }
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            var cartSession = await context.CartSessions
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

                context.CartSessions.Update(cartSession);
                await context.SaveChangesAsync();
            }
        }
    }

    public static decimal CalculateDiscount(string discountCode, decimal subtotal)
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

    public static CartResponseDto MapToCartResponseDto(ShoppingCart? cart, CartSession? cartSession, string? userId, bool isGuest)
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

    public static async Task CreateBundleChildItems(ApplicationDbContext context, ILogger logger, int parentCartItemId, List<BundleItemDto> bundleItems, int parentQuantity, int? cartId, int? cartSessionId)
    {
        foreach (var bundleItem in bundleItems)
        {
            // Validate the bundle item product exists and is available
            var bundleProduct = await context.Products
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == bundleItem.ProductId);

            if (bundleProduct == null || !bundleProduct.IsActive)
            {
                logger.LogWarning("Bundle item product {ProductId} not found or inactive", bundleItem.ProductId);
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

            context.CartItems.Add(childCartItem);
        }
    }

    public static async Task UpdateBundleChildItems(ApplicationDbContext context, CartItem parentItem, List<BundleItemDto> bundleItems, int newParentQuantity)
    {
        var existingChildItems = await context.CartItems
            .Where(ci => ci.ParentBundleItemId == parentItem.Id)
            .ToListAsync();

        foreach (var bundleItem in bundleItems)
        {
            var existingChild = existingChildItems.FirstOrDefault(ci => ci.ProductId == bundleItem.ProductId);
            if (existingChild != null)
            {
                var newQuantity = bundleItem.Quantity * newParentQuantity;
                
                // Validate stock
                var product = await context.Products
                    .Include(p => p.Inventory)
                    .FirstOrDefaultAsync(p => p.Id == bundleItem.ProductId);

                if (product != null && product.Inventory.AvailableQuantity < newQuantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for bundle item {product.Name}. Available: {product.Inventory.AvailableQuantity}, Required: {newQuantity}");
                }

                existingChild.Quantity = newQuantity;
                existingChild.TotalPrice = existingChild.UnitPrice * newQuantity;
                existingChild.UpdatedAt = DateTime.UtcNow;
                context.CartItems.Update(existingChild);
            }
        }
    }

    public static async Task<CartResponseDto> GetCartAsync(ApplicationDbContext context, ILogger logger, string? userId, string? sessionId)
    {
       try
        {
            if (!string.IsNullOrEmpty(userId))
            {
                if (!TryParseUserId(userId, out int userIdInt))
                {
                    return new CartResponseDto { UserId = userId, IsGuest = false };
                }

                var cart = await context.ShoppingCarts
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
            else if (!string.IsNullOrEmpty(sessionId))
            {
                var cartSession = await context.CartSessions
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
                context.CartSessions.Update(cartSession);
                await context.SaveChangesAsync();

                return MapToCartResponseDto(null, cartSession, null, true);
            }

            return new CartResponseDto { IsGuest = string.IsNullOrEmpty(userId) };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting cart for user {UserId} or session {SessionId}", userId, sessionId);
            throw;
        }
    }
}

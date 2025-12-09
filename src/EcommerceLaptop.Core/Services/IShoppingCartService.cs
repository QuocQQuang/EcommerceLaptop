using EcommerceLaptop.Core.DTOs.Cart;

namespace EcommerceLaptop.Core.Services;

public interface IShoppingCartService
{
    /// <summary>
    /// Gets the cart for a user or session
    /// </summary>
    Task<CartResponseDto> GetCartAsync(string? userId, string? sessionId);

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    Task<CartResponseDto> AddToCartAsync(AddToCartDto addToCartDto, string? userId);

    /// <summary>
    /// Updates the quantity of an item in the cart
    /// </summary>
    Task<CartResponseDto> UpdateCartItemAsync(UpdateCartItemDto updateCartDto, string? userId);

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    Task<CartResponseDto> RemoveFromCartAsync(RemoveFromCartDto removeFromCartDto, string? userId);

    /// <summary>
    /// Clears all items from the cart
    /// </summary>
    Task<CartResponseDto> ClearCartAsync(string? userId, string? sessionId);

    /// <summary>
    /// Applies a discount to the cart
    /// </summary>
    Task<CartResponseDto> ApplyDiscountAsync(ApplyDiscountDto discountDto, string? userId);

    /// <summary>
    /// Removes any applied discount from the cart
    /// </summary>
    Task<CartResponseDto> RemoveDiscountAsync(string? userId, string? sessionId);

    /// <summary>
    /// Validates the cart and returns any issues
    /// </summary>
    Task<CartValidationDto> ValidateCartAsync(string? userId, string? sessionId);

    /// <summary>
    /// Migrates a session cart to a user cart when they log in
    /// </summary>
    Task<CartResponseDto> MigrateSessionCartToUserAsync(MigrateCartDto migrateDto);

    /// <summary>
    /// Performs bulk operations on cart items
    /// </summary>
    Task<CartResponseDto> BulkCartOperationAsync(BulkCartOperationDto bulkOperation, string? userId);

    /// <summary>
    /// Calculates shipping cost for the cart
    /// </summary>
    Task<decimal> CalculateShippingAsync(string? userId, string? sessionId, string shippingAddress);

    /// <summary>
    /// Cleans up expired session carts
    /// </summary>
    Task CleanupExpiredSessionsAsync();
}

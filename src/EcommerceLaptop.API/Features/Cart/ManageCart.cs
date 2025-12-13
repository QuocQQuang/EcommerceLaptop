using MediatR;
using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EcommerceLaptop.API.Features.Cart;

// Add to Cart
public record AddToCartCommand(AddToCartDto Request, string? UserId) : IRequest<CartResponseDto>;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AddToCartHandler> _logger;

    public AddToCartHandler(ApplicationDbContext context, ILogger<AddToCartHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartResponseDto> Handle(AddToCartCommand command, CancellationToken cancellationToken)
    {
        var addToCartDto = command.Request;
        var userId = command.UserId;

        try
        {
            // Validate product exists and is available
            var product = await _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Id == addToCartDto.ProductId, cancellationToken);

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

    private async Task<CartResponseDto> AddToUserCartAsync(AddToCartDto addToCartDto, string userId, Product product)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var cart = await CartHelpers.GetOrCreateUserCartAsync(_context, userId);

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
                    await CartHelpers.UpdateBundleChildItems(_context, existingItem, addToCartDto.BundleItems, addToCartDto.Quantity);
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
                    ConfigurationOptions = configJson,
                    IsBundle = addToCartDto.BundleItems?.Any() == true,
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync(); // Save to get the ID

                // If this is a bundle, create child items for each bundle component
                if (addToCartDto.BundleItems?.Any() == true)
                {
                    await CartHelpers.CreateBundleChildItems(_context, _logger, cartItem.Id, addToCartDto.BundleItems, addToCartDto.Quantity, cart.Id, null);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await CartHelpers.RecalculateCartTotalsAsync(_context, userId, null);

            return await CartHelpers.GetCartAsync(_context, _logger, userId, null);
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
            var cartSession = await CartHelpers.GetOrCreateSessionCartAsync(_context, addToCartDto.SessionId!);

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
                    await CartHelpers.UpdateBundleChildItems(_context, existingItem, addToCartDto.BundleItems, addToCartDto.Quantity);
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
                    ConfigurationOptions = configJson,
                    IsBundle = addToCartDto.BundleItems?.Any() == true,
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync(); // Save to get the ID

                // If this is a bundle, create child items for each bundle component
                if (addToCartDto.BundleItems?.Any() == true)
                {
                    await CartHelpers.CreateBundleChildItems(_context, _logger, cartItem.Id, addToCartDto.BundleItems, addToCartDto.Quantity, null, cartSession.Id);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await CartHelpers.RecalculateCartTotalsAsync(_context, null, addToCartDto.SessionId);

            return await CartHelpers.GetCartAsync(_context, _logger, null, addToCartDto.SessionId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

// Update Cart Item
public record UpdateCartItemCommand(UpdateCartItemDto Request, string? UserId) : IRequest<CartResponseDto>;

public class UpdateCartItemHandler : IRequestHandler<UpdateCartItemCommand, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UpdateCartItemHandler> _logger;

    public UpdateCartItemHandler(ApplicationDbContext context, ILogger<UpdateCartItemHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartResponseDto> Handle(UpdateCartItemCommand command, CancellationToken cancellationToken)
    {
        var updateCartDto = command.Request;
        var userId = command.UserId;

        try
        {
            CartItem? cartItem = null;

            if (!string.IsNullOrEmpty(userId) && CartHelpers.TryParseUserId(userId, out int userIdInt))
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .ThenInclude(p => p.Inventory) // Need inventory
                    .Include(ci => ci.ShoppingCart)
                    .FirstOrDefaultAsync(ci => ci.Id == updateCartDto.CartItemId && 
                                             ci.ShoppingCart!.UserId == userIdInt, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(updateCartDto.SessionId))
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .ThenInclude(p => p.Inventory)// Need inventory
                    .Include(ci => ci.CartSession)
                    .FirstOrDefaultAsync(ci => ci.Id == updateCartDto.CartItemId && 
                                             ci.CartSession!.SessionId == updateCartDto.SessionId, cancellationToken);
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

            await _context.SaveChangesAsync(cancellationToken);

            // Recalculate cart totals
            await CartHelpers.RecalculateCartTotalsAsync(_context, userId, updateCartDto.SessionId);

            return await CartHelpers.GetCartAsync(_context, _logger, userId, updateCartDto.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item: {@UpdateCartDto}", updateCartDto);
            throw;
        }
    }
}

// Remove From Cart
public record RemoveFromCartCommand(RemoveFromCartDto Request, string? UserId) : IRequest<CartResponseDto>;

public class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RemoveFromCartHandler> _logger;

    public RemoveFromCartHandler(ApplicationDbContext context, ILogger<RemoveFromCartHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartResponseDto> Handle(RemoveFromCartCommand command, CancellationToken cancellationToken)
    {
        var removeFromCartDto = command.Request;
        var userId = command.UserId;

        try
        {
            CartItem? cartItem = null;

            if (!string.IsNullOrEmpty(userId) && CartHelpers.TryParseUserId(userId, out int userIdInt))
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.ShoppingCart)
                    .FirstOrDefaultAsync(ci => ci.Id == removeFromCartDto.CartItemId && 
                                             ci.ShoppingCart!.UserId == userIdInt, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(removeFromCartDto.SessionId))
            {
                cartItem = await _context.CartItems
                    .Include(ci => ci.CartSession)
                    .FirstOrDefaultAsync(ci => ci.Id == removeFromCartDto.CartItemId && 
                                             ci.CartSession!.SessionId == removeFromCartDto.SessionId, cancellationToken);
            }

            if (cartItem == null)
            {
                throw new ArgumentException("Cart item not found");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync(cancellationToken);

            // Recalculate cart totals
            await CartHelpers.RecalculateCartTotalsAsync(_context, userId, removeFromCartDto.SessionId);

            return await CartHelpers.GetCartAsync(_context, _logger, userId, removeFromCartDto.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item: {@RemoveFromCartDto}", removeFromCartDto);
            throw;
        }
    }
}

// Clear Cart
public record ClearCartCommand(string? UserId, string? SessionId) : IRequest<CartResponseDto>;

public class ClearCartHandler : IRequestHandler<ClearCartCommand, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClearCartHandler> _logger;

    public ClearCartHandler(ApplicationDbContext context, ILogger<ClearCartHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartResponseDto> Handle(ClearCartCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;
        var sessionId = command.SessionId;

        try
        {
            if (!string.IsNullOrEmpty(userId) && CartHelpers.TryParseUserId(userId, out int userIdInt))
            {
                var cart = await _context.ShoppingCarts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.IsActive, cancellationToken);

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
                    .FirstOrDefaultAsync(cs => cs.SessionId == sessionId && cs.IsActive, cancellationToken);

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

            await _context.SaveChangesAsync(cancellationToken);
            return await CartHelpers.GetCartAsync(_context, _logger, userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user {UserId} or session {SessionId}", userId, sessionId);
            throw;
        }
    }
}

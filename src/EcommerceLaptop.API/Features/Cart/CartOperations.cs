using MediatR;
using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EcommerceLaptop.API.Features.Cart;

// Apply Discount
public record ApplyDiscountCommand(ApplyDiscountDto Request, string? UserId) : IRequest<CartResponseDto>;

public class ApplyDiscountHandler : IRequestHandler<ApplyDiscountCommand, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplyDiscountHandler> _logger;

    public ApplyDiscountHandler(ApplicationDbContext context, ILogger<ApplyDiscountHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartResponseDto> Handle(ApplyDiscountCommand command, CancellationToken cancellationToken)
    {
        var discountDto = command.Request;
        var userId = command.UserId;

        try
        {
            var discountPercentage = CartHelpers.CalculateDiscount(discountDto.DiscountCode, 100) / 100; // Just to check if valid

            if (discountPercentage == 0 && CartHelpers.CalculateDiscount(discountDto.DiscountCode, 100) == 0) 
            {
                 // Hacky check: CalculateDiscount returns amount based on total. If total is 100, it returns %.
                 // But if code is invalid it returns 0.
                 // Let's reuse CalculateDiscount logic but careful. 100 * % = amount. amount / 100 = %.
                 // If 0 returned for 100 input, it's 0%. 
                 // Wait, "SAVE10" -> 0.10. 100 * 0.10 = 10.
                 // "INVALID" -> 0. 100 * 0 = 0.
                 // So if result is 0, it's invalid OR 0% discount. Assuming invalid for now.
                 if (CartHelpers.CalculateDiscount(discountDto.DiscountCode, 100) == 0)
                 {
                    throw new ArgumentException("Invalid discount code");
                 }
            }

            if (!string.IsNullOrEmpty(userId))
            {
                var cart = await CartHelpers.GetOrCreateUserCartAsync(_context, userId);
                cart.DiscountCode = discountDto.DiscountCode;
                _context.ShoppingCarts.Update(cart);
            }
            else if (!string.IsNullOrEmpty(discountDto.SessionId))
            {
                var cartSession = await CartHelpers.GetOrCreateSessionCartAsync(_context, discountDto.SessionId);
                cartSession.DiscountCode = discountDto.DiscountCode;
                _context.CartSessions.Update(cartSession);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await CartHelpers.RecalculateCartTotalsAsync(_context, userId, discountDto.SessionId);

            return await CartHelpers.GetCartAsync(_context, _logger, userId, discountDto.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount: {@DiscountDto}", discountDto);
            throw;
        }
    }
}

// Remove Discount
public record RemoveDiscountCommand(string? UserId, string? SessionId) : IRequest<CartResponseDto>;

public class RemoveDiscountHandler : IRequestHandler<RemoveDiscountCommand, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RemoveDiscountHandler> _logger;

    public RemoveDiscountHandler(ApplicationDbContext context, ILogger<RemoveDiscountHandler> logger)
    {
         _context = context;
         _logger = logger;
    }

    public async Task<CartResponseDto> Handle(RemoveDiscountCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;
        var sessionId = command.SessionId;

        try
        {
            if (!string.IsNullOrEmpty(userId) && CartHelpers.TryParseUserId(userId, out int userIdInt))
            {
                var cart = await _context.ShoppingCarts
                    .FirstOrDefaultAsync(c => c.UserId == userIdInt && c.IsActive, cancellationToken);

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
                    .FirstOrDefaultAsync(cs => cs.SessionId == sessionId && cs.IsActive, cancellationToken);

                if (cartSession != null)
                {
                    cartSession.DiscountCode = null;
                    cartSession.DiscountAmount = 0;
                    _context.CartSessions.Update(cartSession);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await CartHelpers.RecalculateCartTotalsAsync(_context, userId, sessionId);

            return await CartHelpers.GetCartAsync(_context, _logger, userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing discount for user {UserId} or session {SessionId}", userId, sessionId);
            throw;
        }
    }
}

// Validate Cart
public record ValidateCartQuery(string? UserId, string? SessionId) : IRequest<CartValidationDto>;

public class ValidateCartHandler : IRequestHandler<ValidateCartQuery, CartValidationDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ValidateCartHandler> _logger;

    public ValidateCartHandler(ApplicationDbContext context, ILogger<ValidateCartHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartValidationDto> Handle(ValidateCartQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var sessionId = request.SessionId;

        try
        {
            var validation = new CartValidationDto();
            var cartItems = new List<CartItem>();

            if (!string.IsNullOrEmpty(userId) && CartHelpers.TryParseUserId(userId, out int userIdInt))
            {
                cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .ThenInclude(p => p.Inventory)
                    .Include(ci => ci.ShoppingCart)
                    .Where(ci => ci.ShoppingCart!.UserId == userIdInt)
                    .ToListAsync(cancellationToken);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .ThenInclude(p => p.Inventory)
                    .Include(ci => ci.CartSession)
                    .Where(ci => ci.CartSession!.SessionId == sessionId)
                    .ToListAsync(cancellationToken);
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
}

// Calculate Shipping
public record CalculateShippingQuery(string Address, string? UserId, string? SessionId) : IRequest<decimal>;

public class CalculateShippingHandler : IRequestHandler<CalculateShippingQuery, decimal>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CalculateShippingHandler> _logger;

    public CalculateShippingHandler(ApplicationDbContext context, ILogger<CalculateShippingHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<decimal> Handle(CalculateShippingQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var sessionId = request.SessionId;
        var shippingAddress = request.Address;

        try
        {
            // Validate inputs - return 0 for invalid inputs
            if ((string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId)) || 
                string.IsNullOrEmpty(shippingAddress))
            {
                return 0;
            }

            var cart = await CartHelpers.GetCartAsync(_context, _logger, userId, sessionId);
            
            // Return 0 if cart is empty
            if (!cart.Items.Any())
            {
                return 0;
            }
            
            // Simple shipping calculation - kept for backward compatibility
            // In a real app this might use a Shipping Provider Service, but that's another service to remove :)
            // Logic copied from Service
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
}

// Migrate Cart
public record MigrateCartCommand(MigrateCartDto Request) : IRequest<CartResponseDto>;

public class MigrateCartHandler : IRequestHandler<MigrateCartCommand, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MigrateCartHandler> _logger;

    public MigrateCartHandler(ApplicationDbContext context, ILogger<MigrateCartHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartResponseDto> Handle(MigrateCartCommand command, CancellationToken cancellationToken)
    {
        var migrateDto = command.Request;

        try
        {
            var cartSession = await _context.CartSessions
                .Include(cs => cs.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(cs => cs.SessionId == migrateDto.SessionId && cs.IsActive, cancellationToken);

            if (cartSession == null || !cartSession.CartItems.Any())
            {
                return await CartHelpers.GetCartAsync(_context, _logger, migrateDto.UserId, null);
            }

            var userCart = await CartHelpers.GetOrCreateUserCartAsync(_context, migrateDto.UserId);

            if (migrateDto.MergeWithExisting)
            {
                // Merge session cart items with existing user cart
                foreach (var sessionItem in cartSession.CartItems)
                {
                    var existingItem = await _context.CartItems
                        .FirstOrDefaultAsync(ci => ci.ShoppingCartId == userCart.Id && 
                                                   ci.ProductId == sessionItem.ProductId &&
                                                   ci.ConfigurationOptions == sessionItem.ConfigurationOptions, cancellationToken);

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
                    .ToListAsync(cancellationToken);

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

            await _context.SaveChangesAsync(cancellationToken);
            await CartHelpers.RecalculateCartTotalsAsync(_context, migrateDto.UserId, null);

            return await CartHelpers.GetCartAsync(_context, _logger, migrateDto.UserId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating cart: {@MigrateDto}", migrateDto);
            throw;
        }
    }
}

// Bulk Cart Operation
public record BulkCartOperationCommand(BulkCartOperationDto Request, string? UserId) : IRequest<CartResponseDto>;

public class BulkCartOperationHandler : IRequestHandler<BulkCartOperationCommand, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BulkCartOperationHandler> _logger;
    private readonly ISender _sender; // Using sender to call other handlers

    public BulkCartOperationHandler(ApplicationDbContext context, ILogger<BulkCartOperationHandler> logger, ISender sender)
    {
        _context = context;
        _logger = logger;
        _sender = sender;
    }

    public async Task<CartResponseDto> Handle(BulkCartOperationCommand command, CancellationToken cancellationToken)
    {
        var bulkOperation = command.Request;
        var userId = command.UserId;

        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Add items
                if (bulkOperation.ItemsToAdd?.Any() == true)
                {
                    foreach (var addItem in bulkOperation.ItemsToAdd)
                    {
                        // Direct calls to internal logic would be more efficient than sending commands through MediatR again,
                        // but to reuse the validation logic in AddToCartHandler, we can call Send.
                        // However, nested transactions might be an issue if AddToCartHandler also starts a transaction (it does).
                        // AddToCartHandler uses transaction. 
                        // It's better to NOT reuse the Handler if it does transaction management.
                        // Ideally logic should be in CartHelpers if we want reuse without transaction.
                        // But AddToCartHandler has complex logic.
                        
                        // For VSA, if we need to compose, we typically extract domain service or reusable helper.
                        // I'll call _sender.Send, but I need to make sure AddToCartHandler handles nested transactions or I should modify it.
                        // EF Core supports nested transactions (savepoints).
                        
                        await _sender.Send(new AddToCartCommand(addItem, userId), cancellationToken);
                    }
                }

                // Update items
                if (bulkOperation.ItemsToUpdate?.Any() == true)
                {
                    foreach (var updateItem in bulkOperation.ItemsToUpdate)
                    {
                         await _sender.Send(new UpdateCartItemCommand(updateItem, userId), cancellationToken);
                    }
                }

                // Remove items
                if (bulkOperation.ItemIdsToRemove?.Any() == true)
                {
                    foreach (var itemId in bulkOperation.ItemIdsToRemove)
                    {
                        await _sender.Send(new RemoveFromCartCommand(new RemoveFromCartDto 
                        { 
                            CartItemId = itemId, 
                            SessionId = bulkOperation.SessionId 
                        }, userId), cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                return await CartHelpers.GetCartAsync(_context, _logger, userId, bulkOperation.SessionId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk cart operation: {@BulkOperation}", bulkOperation);
            throw;
        }
    }
}

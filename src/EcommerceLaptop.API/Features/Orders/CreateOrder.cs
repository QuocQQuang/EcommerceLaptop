using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;

namespace EcommerceLaptop.API.Features.Orders;

// Command for atomic checkout (direct creation)
public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<AtomicCheckoutResult>
{
    public int? AuthenticatedUserId { get; init; }
}

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, AtomicCheckoutResult>
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryReservationService _inventoryService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public CreateOrderHandler(
        ApplicationDbContext context,
        IInventoryReservationService inventoryService,
        IMapper mapper,
        ILogger<CreateOrderHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _inventoryService = inventoryService;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<AtomicCheckoutResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // 1. Validate User Email (existing logic)
        await ValidateEmailConfirmedAsync(command.AuthenticatedUserId);

        var request = command.Request;
        if (command.AuthenticatedUserId.HasValue)
        {
             request.CustomerId = command.AuthenticatedUserId.Value;
        }

        // 2. Logic moved from OrderService.CreateOrderAndInitializePaymentAsync
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting order creation process for customer {CustomerId}, cart {CartId}, payment method {PaymentMethod}",
            request.CustomerId, request.CartId, request.PaymentMethod);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Step 1: Get cart and validate
            _logger.LogDebug("Fetching cart {CartId} for customer {CustomerId}", request.CartId, request.CustomerId);

            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == request.CartId && c.UserId == request.CustomerId, cancellationToken);

            if (cart == null || !cart.IsActive)
            {
                _logger.LogWarning("Cart validation failed - Cart {CartId} not found or inactive for customer {CustomerId}",
                    request.CartId, request.CustomerId);

                return new AtomicCheckoutResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid or inactive cart"
                };
            }

            _logger.LogInformation("Cart {CartId} validated successfully - {ItemCount} items, total ${Total:F2}",
                cart.Id, cart.CartItems.Count, cart.TotalAmount);

            // Step 2: Validate cart items availability
            _logger.LogDebug("Validating inventory availability for {ItemCount} cart items", cart.CartItems.Count);

            var validation = await _inventoryService.ValidateCartItemsAvailabilityAsync(cart.CartItems);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Inventory validation failed for cart {CartId}: {ValidationErrors}",
                    cart.Id, string.Join(", ", validation.Errors));

                return new AtomicCheckoutResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Insufficient inventory for some items: {string.Join(", ", validation.Errors)}"
                };
            }

            _logger.LogInformation("Inventory validation passed for all cart items");

            // Step 3: Create order
            _logger.LogDebug("Creating order entity from cart {CartId}", cart.Id);

            var orderNumber = GenerateOrderNumber();
            
            var shippingAddress = new EcommerceLaptop.Core.ValueObjects.Address(
                request.ShippingAddress,
                request.ShippingCity,
                request.ShippingProvince ?? "",
                request.ShippingPostalCode ?? "",
                request.ShippingCountry ?? "Vietnam"
            );

            var order = Order.Create(
                request.CustomerId,
                orderNumber,
                shippingAddress
            );
            
            order.SetFinancialDetails(cart.TaxAmount, cart.ShippingCost, cart.DiscountAmount);
            // Assuming Payment Method is stored somewhere or handled separately by Payment Service later?
            // The original code didn't set payment method on Order entity directly in Create function, 
            // but CreateOrderRequest has it. It might be used for Payment Initialization later.

            _context.Orders.Add(order);
            
            // Step 4: Create order items
            _logger.LogDebug("Creating {ItemCount} order items for order", cart.CartItems.Count);

            foreach (var cartItem in cart.CartItems)
            {
                order.AddItem(
                    cartItem.ProductId,
                    cartItem.Quantity,
                    cartItem.UnitPrice,
                    cartItem.ItemDiscount
                );

                _logger.LogDebug("Added order item: Product {ProductId}, Quantity {Quantity}, Unit Price ${UnitPrice:F2}",
                    cartItem.ProductId, cartItem.Quantity, cartItem.UnitPrice);
            }
            
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully created order {OrderId} and items", order.Id);

            // Step 5: Reserve inventory
            _logger.LogDebug("Reserving inventory for order {OrderId}", order.Id);

            var reservationResult = await _inventoryService.ReserveInventoryInternalAsync(order.Id);
            if (!reservationResult)
            {
                _logger.LogError("Inventory reservation failed for order {OrderId}", order.Id);

                return new AtomicCheckoutResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to reserve inventory for the order"
                };
            }

            _logger.LogInformation("Inventory successfully reserved for order {OrderId}", order.Id);

            // Step 6: Mark order as inventory reserved and clear cart
            _logger.LogDebug("Marking order {OrderId} as inventory reserved and clearing cart {CartId}", order.Id, cart.Id);

            order.MarkAsInventoryReserved();
            _context.Update(order);

            // Clear the cart
            cart.IsActive = false;
            _context.Update(cart);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Order creation completed successfully in {Duration:F2}ms. Order {OrderNumber} (ID: {OrderId}) created for customer {CustomerId}",
                duration.TotalMilliseconds, order.OrderNumber, order.Id, request.CustomerId);

            return new AtomicCheckoutResult
            {
                IsSuccess = true,
                Order = _mapper.Map<OrderDto>(order),
                Payment = new PaymentInitializationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment initialization will be handled separately",
                    ErrorCode = "PAYMENT_PENDING"
                }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Order creation failed. Error: {ErrorMessage}", ex.Message);
            return new AtomicCheckoutResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task ValidateEmailConfirmedAsync(int? authenticatedUserId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var isEmailConfirmed = context.User.Claims.FirstOrDefault(c => c.Type == "email_confirmed")?.Value;
        bool verified = false;

        if (!string.IsNullOrEmpty(isEmailConfirmed) && bool.TryParse(isEmailConfirmed, out var confirmed))
        {
            verified = confirmed;
        }
        else
        {
            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            if (authenticatedUserId.HasValue)
            {
                var profile = await authService.GetUserProfileAsync(authenticatedUserId.Value);
                if (profile != null && profile.IsEmailVerified == true)
                {
                    verified = true;
                }
            }
        }

        if (!verified)
        {
            // Previously returned generic error, here throwing exception to be caught or handled?
            // The original handler returned AtomicCheckoutResult with error.
            // But we can throw an exception that acts as a domain validation error.
            // However, method returns void. So we should probably return bool or throw.
            throw new InvalidOperationException("Ti khon ca bn cha xc thc email. Vui lng xc thc email trc khi mua hng.");
        }
    }

    private string GenerateOrderNumber()
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var uniqueSuffix = GenerateHexSuffix(12);
        return $"ORD-{datePrefix}-{uniqueSuffix}";
    }

    private static string GenerateHexSuffix(int length)
    {
        var byteCount = (int)Math.Ceiling(length / 2.0);
        Span<byte> bytes = stackalloc byte[byteCount];
        RandomNumberGenerator.Fill(bytes);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return hex.Length > length ? hex.Substring(0, length) : hex;
    }
}

// Command for creating order from cart
public record CreateOrderFromCartCommand(int CartId, CreateOrderRequest Request) : IRequest<OrderDto>
{
    public int? AuthenticatedUserId { get; init; }
}

public class CreateOrderFromCartHandler : IRequestHandler<CreateOrderFromCartCommand, OrderDto>
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryReservationService _inventoryService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public CreateOrderFromCartHandler(
        ApplicationDbContext context,
        IInventoryReservationService inventoryService,
        IEmailService emailService,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _inventoryService = inventoryService;
        _emailService = emailService;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<OrderDto> Handle(CreateOrderFromCartCommand command, CancellationToken cancellationToken)
    {
        if (!command.AuthenticatedUserId.HasValue)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        // Validate Email
        await ValidateEmailConfirmedAsync(command.AuthenticatedUserId.Value);

        var cartId = command.CartId;
        var customerId = command.AuthenticatedUserId.Value;
        
        // Reconstruct logic from CreateOrderFromCartAsync
        var addressParts = command.Request.ShippingAddress.ToString().Split(','); // Simplified reconstruction
        var requestAddr = command.Request.ShippingAddress;
        
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Get cart and validate
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == cartId && c.UserId == customerId, cancellationToken);

            if (cart == null || !cart.IsActive)
            {
                throw new ArgumentException("Invalid or inactive cart");
            }

            // Validate cart items availability
            var validation = await _inventoryService.ValidateCartItemsAvailabilityAsync(cart.CartItems);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException("Insufficient inventory for some items");
            }

            // Create Order
            var street = requestAddr; // Using the string directly as street if simple
            // In original code: var addressParts = shippingAddress.Split(','); ...
            // command.Request.ShippingAddress is a string? Or Address object?
            // In CreateOrderRequest, ShippingAddress is string.
            // Let's stick to the logic used in CreateOrderFromCartAsync
            
            var rawAddress = command.Request.ShippingAddress;
             var addressPartsStr = rawAddress.Split(',');
            var streetStr = addressPartsStr.Length > 0 ? addressPartsStr[0].Trim() : rawAddress;
            var cityStr = addressPartsStr.Length > 1 ? addressPartsStr[1].Trim() : "Unknown City";
            
            var address = new EcommerceLaptop.Core.ValueObjects.Address(streetStr, cityStr, "", "", "");

            var orderNumber = GenerateOrderNumber();

            var order = Order.Create(
                customerId,
                orderNumber,
                address
            );

            order.SetFinancialDetails(cart.TaxAmount, cart.ShippingCost, cart.DiscountAmount);

            _context.Orders.Add(order);

            foreach (var cartItem in cart.CartItems)
            {
                order.AddItem(
                    cartItem.ProductId,
                    cartItem.Quantity,
                    cartItem.UnitPrice,
                    cartItem.ItemDiscount
                );
            }
            
            await _context.SaveChangesAsync(cancellationToken);

            var reservationResult = await _inventoryService.ReserveInventoryInternalAsync(order.Id);
            if (!reservationResult)
            {
                throw new InvalidOperationException("Failed to reserve inventory for the order");
            }

            // Send confirmation email
            var user = await _context.Users.FindAsync(new object[] { customerId }, cancellationToken);
            if (user != null)
            {
                await _emailService.SendOrderConfirmationEmailAsync(user, order.OrderNumber);
            }

            await transaction.CommitAsync(cancellationToken);

            // Clear cart
            cart.IsActive = false;
            _context.Update(cart);
            await _context.SaveChangesAsync(cancellationToken);

            return _mapper.Map<OrderDto>(order);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ValidateEmailConfirmedAsync(int authenticatedUserId)
    {
         var context = _httpContextAccessor.HttpContext;
         if (context == null) return;
         
         // Reuse same logic
         var isEmailConfirmed = context.User.Claims.FirstOrDefault(c => c.Type == "email_confirmed")?.Value;
         bool verified = false;

         if (!string.IsNullOrEmpty(isEmailConfirmed) && bool.TryParse(isEmailConfirmed, out var confirmed))
         {
             verified = confirmed;
         }
         else
         {
             using var scope = _serviceProvider.CreateScope();
             var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
             var profile = await authService.GetUserProfileAsync(authenticatedUserId);
             if (profile != null && profile.IsEmailVerified == true)
             {
                 verified = true;
             }
         }

         if (!verified)
         {
             throw new InvalidOperationException("Ti khon ca bn cha xc thc email. Vui lng xc thc email trc khi mua hng.");
         }
    }

    private string GenerateOrderNumber()
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var uniqueSuffix = GenerateHexSuffix(12);
        return $"ORD-{datePrefix}-{uniqueSuffix}";
    }

    private static string GenerateHexSuffix(int length)
    {
        var byteCount = (int)Math.Ceiling(length / 2.0);
        Span<byte> bytes = stackalloc byte[byteCount];
        RandomNumberGenerator.Fill(bytes);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return hex.Length > length ? hex.Substring(0, length) : hex;
    }
}

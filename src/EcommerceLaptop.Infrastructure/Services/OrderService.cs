using AutoMapper;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceLaptop.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryReservationService _inventoryService;
    private readonly IEmailService _emailService;
    private readonly IOrderWorkflowService _workflowService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ApplicationDbContext context,
        IInventoryReservationService inventoryService,
        IEmailService emailService,
        IOrderWorkflowService workflowService,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _emailService = emailService;
        _workflowService = workflowService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(int cartId, int customerId, string shippingAddress)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Get cart and validate
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == cartId && c.UserId == customerId);

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

            // Create order
            var order = new Order
            {
                UserId = customerId,
                OrderNumber = GenerateOrderNumber(),
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SubTotal = cart.SubTotal,
                TaxAmount = cart.TaxAmount,
                ShippingAmount = cart.ShippingCost,
                DiscountAmount = cart.DiscountAmount,
                TotalAmount = cart.TotalAmount,
                InventoryReserved = false,
                ShippingStreet = shippingAddress
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create order items
            foreach (var cartItem in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    DiscountAmount = cartItem.ItemDiscount,
                    TotalPrice = cartItem.TotalPrice
                };
                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();

            // Reserve inventory (using internal method to avoid nested transaction)
            var reservationResult = await _inventoryService.ReserveInventoryInternalAsync(order.Id);
            if (!reservationResult)
            {
                throw new InvalidOperationException("Failed to reserve inventory for the order");
            }

            // Order remains Pending until payment webhook confirms success
            // Inventory is reserved to hold stock temporarily

            // Send confirmation email
            var user = await _context.Users.FindAsync(customerId);
            if (user != null)
            {
                await _emailService.SendOrderConfirmationEmailAsync(user, order.OrderNumber);
            }

            await transaction.CommitAsync();

            // Clear cart
            cart.IsActive = false;
            _context.Update(cart);
            await _context.SaveChangesAsync();

            return _mapper.Map<OrderDto>(order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AtomicCheckoutResult> CreateOrderAndInitializePaymentAsync(CreateOrderRequest request)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting order creation process for customer {CustomerId}, cart {CartId}, payment method {PaymentMethod}",
            request.CustomerId, request.CartId, request.PaymentMethod);

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Get cart and validate
            _logger.LogDebug("Fetching cart {CartId} for customer {CustomerId}", request.CartId, request.CustomerId);

            var cart = await _context.ShoppingCarts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == request.CartId && c.UserId == request.CustomerId);

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
            var order = new Order
            {
                UserId = request.CustomerId,
                OrderNumber = orderNumber,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SubTotal = cart.SubTotal,
                TaxAmount = cart.TaxAmount,
                ShippingAmount = cart.ShippingCost,
                DiscountAmount = cart.DiscountAmount,
                TotalAmount = cart.TotalAmount,
                InventoryReserved = false,
                ShippingStreet = request.ShippingAddress,
                ShippingCity = request.ShippingCity,
                ShippingProvince = request.ShippingProvince,
                ShippingPostalCode = request.ShippingPostalCode,
                ShippingCountry = request.ShippingCountry
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderNumber} (ID: {OrderId}) created successfully for customer {CustomerId}",
                orderNumber, order.Id, request.CustomerId);

            // Step 4: Create order items
            _logger.LogDebug("Creating {ItemCount} order items for order {OrderId}", cart.CartItems.Count, order.Id);

            foreach (var cartItem in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    DiscountAmount = cartItem.ItemDiscount,
                    TotalPrice = cartItem.TotalPrice
                };
                _context.OrderItems.Add(orderItem);

                _logger.LogDebug("Added order item: Product {ProductId}, Quantity {Quantity}, Unit Price ${UnitPrice:F2}",
                    cartItem.ProductId, cartItem.Quantity, cartItem.UnitPrice);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully created {ItemCount} order items for order {OrderId}", cart.CartItems.Count, order.Id);

            // Step 5: Reserve inventory (using internal method to avoid nested transaction)
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

            order.InventoryReserved = true;
            _context.Update(order);

            // Clear the cart
            cart.IsActive = false;
            _context.Update(cart);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

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
            await transaction.RollbackAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Order creation failed after {Duration:F2}ms for customer {CustomerId}, cart {CartId}. Error: {ErrorMessage}",
                duration.TotalMilliseconds, request.CustomerId, request.CartId, ex.Message);

            return new AtomicCheckoutResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OrderDetailsDto> GetOrderDetailsAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Audits)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new ArgumentException("Order not found");
        }

        return _mapper.Map<OrderDetailsDto>(order);
    }

    public async Task<EcommerceLaptop.Core.DTOs.PagedResult<OrderDto>> GetCustomerOrdersAsync(int customerId, int page = 1, int pageSize = 10)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Payments) // Include payments for payment status
            .Where(o => o.UserId == customerId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var orderDtos = _mapper.Map<List<OrderDto>>(orders);

        return new EcommerceLaptop.Core.DTOs.PagedResult<OrderDto>
        {
            Items = orderDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, UpdateStatusRequest request)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return false;
        }

        // Use workflow service for validation and audit
        return await _workflowService.UpdateOrderStatusAsync(orderId, request.NewStatus, request.Reason, null);
    }

    public async Task<bool> CancelOrderAsync(int orderId, CancelOrderRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Status == OrderStatus.Cancelled)
        {
            return false;
        }

        // Check if order has been paid - only allow cancellation for unpaid orders
        var hasCompletedPayment = order.Payments.Any(p => p.Status == PaymentStatus.Completed);
        if (hasCompletedPayment)
        {
            _logger.LogWarning("Cannot cancel order {OrderId} - order has been paid", orderId);
            return false;
        }

        // Use workflow service for cancellation logic
        return await _workflowService.CancelOrderAsync(orderId, request.Reason);
    }

    private string GenerateOrderNumber()
    {
        // Collision-resistant order number: ORD-YYYYMMDD-<12 hex chars>
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var uniqueSuffix = GenerateHexSuffix(12); // 48 bits of randomness
        return $"ORD-{datePrefix}-{uniqueSuffix}";
    }

    private static string GenerateHexSuffix(int length)
    {
        // Generate cryptographically strong random bytes and return hex string of desired length
        var byteCount = (int)Math.Ceiling(length / 2.0);
        Span<byte> bytes = stackalloc byte[byteCount];
        RandomNumberGenerator.Fill(bytes);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return hex.Length > length ? hex.Substring(0, length) : hex;
    }



    public async Task<EcommerceLaptop.Core.DTOs.PagedResult<OrderDto>> GetAdminOrdersAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null, int? customerId = null)
    {
        try
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search) ||
                                        o.User.Email.Contains(search) ||
                                        o.User.FirstName.Contains(search) ||
                                        o.User.LastName.Contains(search));
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            {
                query = query.Where(o => o.Status == orderStatus);
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.UserId == customerId.Value);
            }

            // Order by most recent first
            query = query.OrderByDescending(o => o.CreatedAt);

            var totalCount = await query.CountAsync();

            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var orderDtos = _mapper.Map<List<OrderDto>>(orders);

            return new EcommerceLaptop.Core.DTOs.PagedResult<OrderDto>
            {
                Items = orderDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin orders with filters: search={Search}, status={Status}, customerId={CustomerId}", search, status, customerId);
            throw;
        }
    }

    public async Task<EcommerceLaptop.Core.DTOs.PagedResult<AdminOrderDto>> GetEnhancedAdminOrdersAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null, int? customerId = null)
    {
        try
        {
            _logger.LogInformation("Getting enhanced admin orders with filters: page={Page}, pageSize={PageSize}, search={Search}, status={Status}, customerId={CustomerId}",
                page, pageSize, search, status, customerId);

            var query = _context.Orders
                .Include(o => o.User) // Include user for customer information
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments) // Include payments for payment info
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search) ||
                                        o.User.Email.Contains(search) ||
                                        o.User.FirstName.Contains(search) ||
                                        o.User.LastName.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    query = query.Where(o => o.Status == orderStatus);
                }
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.UserId == customerId.Value);
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var adminOrderDtos = _mapper.Map<List<AdminOrderDto>>(orders);

            return new EcommerceLaptop.Core.DTOs.PagedResult<AdminOrderDto>
            {
                Items = adminOrderDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enhanced admin orders with filters: search={Search}, status={Status}, customerId={CustomerId}", search, status, customerId);
            throw;
        }
    }



    public async Task<EcommerceLaptop.Core.Entities.Payment?> GetPaymentByTransactionIdAsync(string transactionId)
    {
        try
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            _logger.LogInformation("Database lookup for transaction {TransactionId}: {Found}",
                transactionId, payment != null ? "Found" : "Not Found");

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up payment by transaction ID {TransactionId}", transactionId);
            return null;
        }
    }
}

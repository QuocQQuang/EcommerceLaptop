using AutoMapper;
using AddressVO = EcommerceLaptop.Core.ValueObjects.Address;
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
        Order order;

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

            // Create Address Value Object
            var addressParts = shippingAddress.Split(',');
            var street = addressParts.Length > 0 ? addressParts[0].Trim() : shippingAddress;
            var city = addressParts.Length > 1 ? addressParts[1].Trim() : "Unknown City";
            var address = new AddressVO(street, city, "", "", "");

            // Create order
            order = Order.Create(
                customerId,
                GenerateOrderNumber(),
                address
            );

            // Calculate financials from cart
            order.SetFinancialDetails(cart.TaxAmount, cart.ShippingCost, cart.DiscountAmount);

            _context.Orders.Add(order);

            // Add order items
            foreach (var cartItem in cart.CartItems)
            {
                order.AddItem(
                    cartItem.ProductId,
                    cartItem.Quantity,
                    cartItem.UnitPrice,
                    cartItem.ItemDiscount
                );
            }

            // Reserve inventory inline  RowVersion on Inventory prevents overselling
            var productIds = cart.CartItems.Select(ci => ci.ProductId).ToList();
            var inventories = await _context.Inventories
                .Where(i => productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId);

            foreach (var cartItem in cart.CartItems)
            {
                if (!inventories.TryGetValue(cartItem.ProductId, out var inventory)
                    || inventory.AvailableQuantity < cartItem.Quantity)
                {
                    throw new InvalidOperationException("Failed to reserve inventory for the order");
                }

                inventory.ReserveStock(
                    cartItem.Quantity,
                    order.OrderNumber,
                    "Reserved for order",
                    customerId
                );
            }

            order.MarkAsInventoryReserved();

            // Deactivate cart INSIDE transaction to prevent double orders on crash
            cart.IsActive = false;

            // Single atomic SaveChangesAsync: Order + Items + Inventory + Cart
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion conflict  another transaction reserved the same inventory
            await transaction.RollbackAsync();
            throw new InvalidOperationException(
                "Another order was placed for the same product at the same time. Please try again.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        // Fire-and-forget email  don't block HTTP response for SMTP timeout (~2 min)
        _ = Task.Run(async () =>
        {
            try
            {
                var user = await _context.Users.FindAsync(customerId);
                if (user != null)
                {
                    await _emailService.SendOrderConfirmationEmailAsync(user, order.OrderNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send order confirmation email for order {OrderNumber}, but order was created successfully", order.OrderNumber);
            }
        });

        return _mapper.Map<OrderDto>(order);
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

            // Step 2: Create order
            _logger.LogDebug("Creating order entity from cart {CartId}", cart.Id);

            var orderNumber = GenerateOrderNumber();

            var shippingAddress = new AddressVO(
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

            _context.Orders.Add(order);

            // Step 3: Create order items
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

            // Step 4: Reserve inventory inline  RowVersion on Inventory prevents overselling
            _logger.LogDebug("Reserving inventory for order");

            var productIds = cart.CartItems.Select(ci => ci.ProductId).ToList();
            var inventories = await _context.Inventories
                .Where(i => productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId);

            foreach (var cartItem in cart.CartItems)
            {
                if (!inventories.TryGetValue(cartItem.ProductId, out var inv)
                    || inv.AvailableQuantity < cartItem.Quantity)
                {
                    _logger.LogError("Inventory reservation failed for product {ProductId}", cartItem.ProductId);

                    return new AtomicCheckoutResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to reserve inventory for the order"
                    };
                }

                inv.ReserveStock(
                    cartItem.Quantity,
                    order.OrderNumber,
                    "Reserved for order",
                    request.CustomerId
                );
            }

            _logger.LogInformation("Inventory successfully reserved for order");

            // Step 5: Mark order as inventory reserved and deactivate cart
            order.MarkAsInventoryReserved();
            cart.IsActive = false;

            // Single atomic SaveChangesAsync: Order + Items + Inventory + Cart
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully created order {OrderId} with inventory reserved", order.Id);

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
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogWarning("Concurrency conflict after {Duration:F2}ms for customer {CustomerId}, cart {CartId}  another order reserved the same inventory",
                duration.TotalMilliseconds, request.CustomerId, request.CartId);

            return new AtomicCheckoutResult
            {
                IsSuccess = false,
                ErrorMessage = "Another order was placed for the same product at the same time. Please try again."
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

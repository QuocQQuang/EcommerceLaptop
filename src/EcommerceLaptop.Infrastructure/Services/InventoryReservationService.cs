using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceLaptop.Infrastructure.Services;

public class InventoryReservationService : IInventoryReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InventoryReservationService> _logger;

    public InventoryReservationService(ApplicationDbContext context, ILogger<InventoryReservationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Internal method to reserve inventory without managing transactions
    /// Used when transaction is already managed by caller (e.g., OrderService)
    /// </summary>
    /// <summary>
    /// Internal method to reserve inventory without managing transactions
    /// Used when transaction is already managed by caller (e.g., OrderService)
    /// </summary>
    public async Task<bool> ReserveInventoryInternalAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .ThenInclude(p => p.Inventory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return false;
        }

        foreach (var orderItem in order.OrderItems)
        {
            var inventory = orderItem.Product.Inventory;
            if (inventory == null || inventory.AvailableQuantity < orderItem.Quantity)
            {
                return false; // Don't rollback here - let caller handle it
            }

            inventory.ReserveStock(
                orderItem.Quantity, 
                order.OrderNumber, 
                "Reserved for order", 
                order.UserId
            );
        }

        order.MarkAsInventoryReserved();
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReserveInventoryForOrderAsync(int orderId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var result = await ReserveInventoryInternalAsync(orderId);
            if (result)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ReleaseInventoryForOrderAsync(int orderId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Inventory)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return false;
            }

            foreach (var orderItem in order.OrderItems)
            {
                var inventory = orderItem.Product.Inventory;
                if (inventory != null)
                {
                    // Use CancelReservation
                    inventory.CancelReservation(
                        orderItem.Quantity,
                        order.OrderNumber,
                        "Released from cancelled order",
                        order.UserId
                    );
                }
            }

            order.ReleaseInventoryReservation();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<InventoryValidationResult> ValidateCartItemsAvailabilityAsync(IEnumerable<CartItem> cartItems)
    {
        var result = new InventoryValidationResult { IsValid = true };
        var inventories = await _context.Inventories
            .Where(i => cartItems.Select(ci => ci.ProductId).Contains(i.ProductId))
            .ToDictionaryAsync(i => i.ProductId);

        foreach (var cartItem in cartItems)
        {
            if (inventories.TryGetValue(cartItem.ProductId, out var inventory))
            {
                if (inventory.AvailableQuantity < cartItem.Quantity)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Insufficient stock for {cartItem.Product?.Name}. Available: {inventory.AvailableQuantity}, Requested: {cartItem.Quantity}");
                }
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add($"No inventory record for product {cartItem.ProductId}");
            }
        }

        return result;
    }

    public async Task<bool> UpdateReservedQuantityAsync(int productId, int quantityChange)
    {
        var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (inventory == null)
        {
            return false;
        }

        // Logic here is ambiguous: is it a reservation or release?
        // Assuming quantityChange > 0 is Reserve, < 0 is Release
        if (quantityChange > 0)
        {
            inventory.ReserveStock(quantityChange, "MANUAL_UPDATE", "Manual reservation update", 1);
        }
        else if (quantityChange < 0)
        {
            inventory.CancelReservation(Math.Abs(quantityChange), "MANUAL_UPDATE", "Manual reservation update", 1);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Confirms inventory reservations by permanently deducting from stock and clearing reservation
    /// Used when payment is successful
    /// </summary>
    public async Task<bool> ConfirmInventoryReservationAsync(int orderId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || !order.InventoryReserved)
            {
                _logger.LogWarning("Order {OrderId} not found or inventory not reserved", orderId);
                return false;
            }

            foreach (var item in order.OrderItems)
            {
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
                if (inventory != null)
                {
                    if (inventory.ReservedQuantity >= item.Quantity)
                    {
                        inventory.ConfirmReservation(
                            item.Quantity,
                            $"ORDER_{orderId}",
                            "Order confirmed - Inventory deducted",
                            0 // System
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Inventory mismatch for product {ProductId} in order {OrderId}. Reserved: {Reserved}, Required: {Required}", 
                            item.ProductId, orderId, inventory.ReservedQuantity, item.Quantity);
                        
                        // Force fix: if we don't have enough reserved, we just deduct what we can from reserved and the rest?
                        // Or we just deduct from stock anyway?
                        // ConfirmReservation throws if Reserved < Q.
                        // We must handle this mismatch manually or assume Reserved is reliable.
                        // If Reserved < Q, we should probably just RemoveStock (adjusting quantity) and set Reserved to MAX(0, Reserved - Q) manually?
                        // But I can't set Reserved manually.
                        // I will use RemoveStock directly and CancelReservation for whatever IS reserved?
                        
                        var reservedToRelease = Math.Min(inventory.ReservedQuantity, item.Quantity);
                        if (reservedToRelease > 0)
                        {
                            // This adds a Release transaction which we might NOT want.
                            // But since it's a "mismatch fix", logging it is fine?
                            // No, ConfirmReservation reduces reserved silently.
                            // I can't call ConfirmReservation if Q > Reserved.
                            
                            // Domain method limitation: cannot "Force" fix mismatch if private setters are used.
                            // I might need a "ForceCorrection" method on Inventory or just rely on ConfirmReservation logic being correct if data is correct.
                            // Given this is an edge case block ("mismatch"), let's try to proceed as best as possible.
                            
                            // Scenario: Reserved = 5, Item Q = 10.
                            // We want: Reserved -> 0, Stock -> Stock - 10.
                            // ConfirmReservation(5) -> Reserved=0, Stock=Stock-5.
                            // Then RemoveStock(5) -> Stock=Stock-10.
                            // This works!
                            
                            if (inventory.ReservedQuantity > 0)
                            {
                                inventory.ConfirmReservation(inventory.ReservedQuantity, $"ORDER_{orderId}", "Partial reservation confirmation", 0);
                            }
                            
                            var remaining = item.Quantity - inventory.ReservedQuantity; // Wait, inventory.ReservedQuantity is 0 now.
                            // So: var remaining = item.Quantity - initialReserved;
                            
                            // Recalculating:
                            var initialReserved = inventory.ReservedQuantity;
                            inventory.ConfirmReservation(initialReserved, $"ORDER_{orderId}", "Partial reservation confirmation", 0);
                            
                            var leftover = item.Quantity - initialReserved;
                            inventory.RemoveStock(leftover, $"ORDER_{orderId}", "Force deduction for unreserved portion", 0);
                        }
                        else
                        {
                            inventory.RemoveStock(item.Quantity, $"ORDER_{orderId}", "Force deduction", 0);
                        }
                    }
                    
                    _context.Inventories.Update(inventory);
                }
            }

            order.ReleaseInventoryReservation();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Confirmed inventory for order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error confirming inventory for order {OrderId}", orderId);
            return false;
        }
    }

    /// <summary>
    /// Restocks inventory for an order (e.g. refund/return)
    /// </summary>
    public async Task<bool> RestockInventoryForOrderAsync(int orderId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for restocking", orderId);
                return false;
            }

            foreach (var item in order.OrderItems)
            {
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
                if (inventory != null)
                {
                    // Use AddStock domain method
                    inventory.AddStock(
                        item.Quantity,
                        $"REFUND_{orderId}",
                        "Order refunded - Inventory restocked",
                        0 // System
                    );
                    
                    _context.Inventories.Update(inventory);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Restocked inventory for order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error restocking inventory for order {OrderId}", orderId);
            return false;
        }
    }
}

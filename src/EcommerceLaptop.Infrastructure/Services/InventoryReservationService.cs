using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceLaptop.Infrastructure.Services;

public class InventoryReservationService : IInventoryReservationService
{
    private readonly ApplicationDbContext _context;

    public InventoryReservationService(ApplicationDbContext context)
    {
        _context = context;
    }

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
            if (inventory == null || inventory.QuantityInStock < orderItem.Quantity)
            {
                return false; // Don't rollback here - let caller handle it
            }

            inventory.ReservedQuantity += orderItem.Quantity;
            inventory.LastStockUpdate = DateTime.UtcNow;

            // Create transaction record
            var transactionRecord = new InventoryTransaction
            {
                InventoryId = inventory.Id,
                Type = InventoryTransactionType.Reservation,
                Quantity = orderItem.Quantity,
                Reference = order.OrderNumber,
                Notes = "Reserved for order",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = order.UserId
            };
            _context.InventoryTransactions.Add(transactionRecord);
        }

        order.InventoryReserved = true;
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
                    inventory.ReservedQuantity -= orderItem.Quantity;
                    inventory.LastStockUpdate = DateTime.UtcNow;

                    var transactionRecord = new InventoryTransaction
                    {
                        InventoryId = inventory.Id,
                        Type = InventoryTransactionType.Release,
                        Quantity = -orderItem.Quantity,
                        Reference = order.OrderNumber,
                        Notes = "Released from cancelled order",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = order.UserId
                    };
                    _context.InventoryTransactions.Add(transactionRecord);
                }
            }

            order.InventoryReserved = false;
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

        inventory.ReservedQuantity += quantityChange;
        inventory.LastStockUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}

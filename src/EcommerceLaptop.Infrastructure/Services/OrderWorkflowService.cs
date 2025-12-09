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

public class OrderWorkflowService : IOrderWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryReservationService _inventoryService;
    private readonly ILogger<OrderWorkflowService> _logger;

    public OrderWorkflowService(ApplicationDbContext context, IInventoryReservationService inventoryService, ILogger<OrderWorkflowService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string reason, int? changedByUserId = null)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning(" ORDER NOT FOUND - OrderId: {OrderId}", orderId);
            return false;
        }

        _logger.LogInformation(" ORDER STATUS TRANSITION - OrderId: {OrderId}, CurrentStatus: {CurrentStatus}, NewStatus: {NewStatus}", 
            orderId, order.Status, newStatus);

        if (!CanTransitionTo(order.Status, newStatus))
        {
            var validTransitions = GetValidTransitions(order.Status);
            _logger.LogWarning(" INVALID TRANSITION - OrderId: {OrderId}, CurrentStatus: {CurrentStatus}, AttemptedStatus: {NewStatus}, ValidTransitions: [{ValidTransitions}]", 
                orderId, order.Status, newStatus, string.Join(", ", validTransitions));
            return false;
        }

        // Create audit record
        var audit = new OrderAudit
        {
            OrderId = orderId,
            OldStatus = order.Status,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            Reason = reason,
            ChangedAt = DateTime.UtcNow
        };
        _context.OrderAudits.Add(audit);

        // Update order status
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        // Business logic based on new status
        switch (newStatus)
        {
            case OrderStatus.Confirmed:
                // Reserve inventory if not already reserved
                if (!order.InventoryReserved)
                {
                    var reserved = await _inventoryService.ReserveInventoryForOrderAsync(orderId);
                    if (!reserved)
                    {
                        // Rollback if reservation fails
                        return false;
                    }
                }
                break;
            case OrderStatus.Processing:
                // Prepare for shipping
                break;
            case OrderStatus.Shipped:
                // Order shipped - no tracking needed for simplified system
                break;
            case OrderStatus.Delivered:
                // Order delivered successfully
                break;
            case OrderStatus.Cancelled:
                // Release inventory
                if (order.InventoryReserved)
                {
                    await _inventoryService.ReleaseInventoryForOrderAsync(orderId);
                }
                break;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelOrderAsync(int orderId, string reason)
    {
        return await UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled, reason);
    }

    public bool CanTransitionTo(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return currentStatus switch
        {
            OrderStatus.Pending => newStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => newStatus is OrderStatus.Processing or OrderStatus.Cancelled,
            OrderStatus.Processing => newStatus is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => newStatus is OrderStatus.Delivered or OrderStatus.Cancelled,
            OrderStatus.Delivered => newStatus is OrderStatus.Returned or OrderStatus.Refunded,
            _ => false
        };
    }

    private IEnumerable<OrderStatus> GetValidTransitions(OrderStatus currentStatus)
    {
        return currentStatus switch
        {
            OrderStatus.Pending => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => new[] { OrderStatus.Processing, OrderStatus.Cancelled },
            OrderStatus.Processing => new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            OrderStatus.Shipped => new[] { OrderStatus.Delivered, OrderStatus.Cancelled },
            OrderStatus.Delivered => new[] { OrderStatus.Returned, OrderStatus.Refunded },
            _ => Array.Empty<OrderStatus>()
        };
    }

    private string GenerateTrackingNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(10000, 99999);
        return $"TRK-{timestamp}-{random}";
    }
}

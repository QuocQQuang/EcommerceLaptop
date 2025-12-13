using MediatR;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Orders;

// Update Order Status
public record UpdateOrderStatusCommand(int OrderId, UpdateStatusRequest Request) : IRequest<bool>;

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderWorkflowService _workflowService;
    private readonly ILogger<UpdateOrderStatusHandler> _logger;

    public UpdateOrderStatusHandler(
        ApplicationDbContext context, 
        IOrderWorkflowService workflowService,
        ILogger<UpdateOrderStatusHandler> logger)
    {
        _context = context;
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.OrderId }, cancellationToken);
        if (order == null) return false;

        _logger.LogInformation("Updating order {OrderId} status to {NewStatus}", request.OrderId, request.Request.NewStatus);

        return await _workflowService.UpdateOrderStatusAsync(order.Id, request.Request.NewStatus, request.Request.Reason ?? "Admin Update");
    }
}

// Cancel Order
public record CancelOrderCommand(int OrderId, CancelOrderRequest Request) : IRequest<bool>
{
    public int? AuthenticatedUserId { get; init; }
}

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderWorkflowService _workflowService;
    private readonly ILogger<CancelOrderHandler> _logger;

    public CancelOrderHandler(
        ApplicationDbContext context, 
        IOrderWorkflowService workflowService,
        ILogger<CancelOrderHandler> logger)
    {
        _context = context;
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to cancel order {OrderId}", command.OrderId);

        var order = await _context.Orders
            .Include(o => o.Payments) // Include payments to check if paid
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order == null) return false;

        // Validation: Cannot cancel if already Paid (Business Rule from OrderService)
        // OrderService logic: 
        // var order = await _context.Orders.Include(o => o.Payments).FirstOrDefaultAsync...
        // if (order.Status == OrderStatus.Completed || order.IsPaid) ... return false;
        
        // Actually OrderService logic was:
        // if (order.Payments.Any(p => p.Status == PaymentStatus.Completed)) { return false; }

        if (order.Payments.Any(p => p.Status == PaymentStatus.Completed))
        {
            _logger.LogWarning("Cannot cancel order {OrderId} because it has completed payments.", command.OrderId);
            return false;
        }
        
        // Ownership check if needed? Command has AuthenticatedUserId.
        // If AuthenticatedUserId is provided, we SHOULD check if it matches Order.UserId
        if (command.AuthenticatedUserId.HasValue && order.UserId != command.AuthenticatedUserId.Value)
        {
            _logger.LogWarning("User {UserId} tried to cancel order {OrderId} belonging to {OrderUserId}", command.AuthenticatedUserId, command.OrderId, order.UserId);
            return false; // Or throw Unauthorized? Returning false implies "failed" which is safe enough here.
        }

        return await _workflowService.CancelOrderAsync(order.Id, command.Request.Reason ?? "User Cancellation");
    }
}

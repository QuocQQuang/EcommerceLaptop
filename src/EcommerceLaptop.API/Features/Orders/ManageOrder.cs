using MediatR;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.API.Features.Orders;

// Update Order Status
public record UpdateOrderStatusCommand(int OrderId, UpdateStatusRequest Request) : IRequest<bool>;

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
{
    private readonly IOrderService _orderService;

    public UpdateOrderStatusHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        return await _orderService.UpdateOrderStatusAsync(request.OrderId, request.Request);
    }
}

// Cancel Order
public record CancelOrderCommand(int OrderId, CancelOrderRequest Request) : IRequest<bool>
{
    public int? AuthenticatedUserId { get; init; }
}

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderService _orderService;

    public CancelOrderHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<bool> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        // Service might verify user permissions/ownership, but command includes AuthenticatedUserId just in case logic needs it.
        // The controller logic:
        // var customerId = GetUserId();
        // var success = await _orderService.CancelOrderAsync(orderId, request);
        // It passes request, but NOT customerId to the CancelOrderAsync method directly?
        // Let's check IOrderService signature again.
        // Task<bool> CancelOrderAsync(int orderId, CancelOrderRequest request);
        // Use logic inside service likely triggers generic checks or assumes admin/user context from somewhere else?
        // Or maybe logic is missing the user check inside service?
        // In the Controller, `if (string.IsNullOrEmpty(customerId)) return Unauthorized();`
        // But `CancelOrderAsync` doesn't take customerId.
        // Potentially the service relies on `IHttpContextAccessor` implicitly? 
        // Or it's a gap in the current implementation.
        // Regardless, I am wrapping the existing service call.
        
        return await _orderService.CancelOrderAsync(command.OrderId, command.Request);
    }
}

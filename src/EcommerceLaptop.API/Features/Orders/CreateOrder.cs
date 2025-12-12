using MediatR;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace EcommerceLaptop.API.Features.Orders;

// Command for atomic checkout (direct creation)
public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<AtomicCheckoutResult>
{
    public int? AuthenticatedUserId { get; init; }
}

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, AtomicCheckoutResult>
{
    private readonly IOrderService _orderService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider; // To resolve IAuthService if needed, or inject directly

    public CreateOrderHandler(IOrderService orderService, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
    {
        _orderService = orderService;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<AtomicCheckoutResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        
        // Email Confirmation Check
        if (context != null)
        {
            var isEmailConfirmed = context.User.Claims.FirstOrDefault(c => c.Type == "email_confirmed")?.Value;
            bool verified = false;

            if (!string.IsNullOrEmpty(isEmailConfirmed) && bool.TryParse(isEmailConfirmed, out var confirmed))
            {
                verified = confirmed;
            }
            else
            {
                // Fallback: check profile via AuthService
                using var scope = _serviceProvider.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                if (command.AuthenticatedUserId.HasValue)
                {
                    var profile = await authService.GetUserProfileAsync(command.AuthenticatedUserId.Value);
                    if (profile != null && profile.IsEmailVerified == true)
                    {
                        verified = true;
                    }
                }
            }

            if (!verified)
            {
                return new AtomicCheckoutResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Ti khon ca bn cha xc thc email. Vui lng xc thc email trc khi mua hng." 
                };
            }
        }

        var request = command.Request;
        if (command.AuthenticatedUserId.HasValue)
        {
             request.CustomerId = command.AuthenticatedUserId.Value;
        }

        return await _orderService.CreateOrderAndInitializePaymentAsync(request);
    }
}

// Command for creating order from cart
public record CreateOrderFromCartCommand(int CartId, CreateOrderRequest Request) : IRequest<OrderDto>
{
    public int? AuthenticatedUserId { get; init; }
}

public class CreateOrderFromCartHandler : IRequestHandler<CreateOrderFromCartCommand, OrderDto>
{
    private readonly IOrderService _orderService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public CreateOrderFromCartHandler(IOrderService orderService, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
    {
        _orderService = orderService;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<OrderDto> Handle(CreateOrderFromCartCommand command, CancellationToken cancellationToken)
    {
        if (!command.AuthenticatedUserId.HasValue)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        // Email Confirmation Check (Duplicated logic, could be shared service/method)
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
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
                var profile = await authService.GetUserProfileAsync(command.AuthenticatedUserId.Value);
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

        return await _orderService.CreateOrderFromCartAsync(
            command.CartId, 
            command.AuthenticatedUserId.Value, 
            command.Request.ShippingAddress.ToString() 
        );
    }
}

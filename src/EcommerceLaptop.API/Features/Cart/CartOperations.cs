using MediatR;
using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.API.Features.Cart;

// Apply Discount
public record ApplyDiscountCommand(ApplyDiscountDto Request, string? UserId) : IRequest<CartResponseDto>;

public class ApplyDiscountHandler : IRequestHandler<ApplyDiscountCommand, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public ApplyDiscountHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(ApplyDiscountCommand command, CancellationToken cancellationToken)
    {
        return await _cartService.ApplyDiscountAsync(command.Request, command.UserId);
    }
}

// Remove Discount
public record RemoveDiscountCommand(string? UserId, string? SessionId) : IRequest<CartResponseDto>;

public class RemoveDiscountHandler : IRequestHandler<RemoveDiscountCommand, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public RemoveDiscountHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(RemoveDiscountCommand command, CancellationToken cancellationToken)
    {
        return await _cartService.RemoveDiscountAsync(command.UserId, command.SessionId);
    }
}

// Validate Cart
public record ValidateCartQuery(string? UserId, string? SessionId) : IRequest<CartValidationDto>;

public class ValidateCartHandler : IRequestHandler<ValidateCartQuery, CartValidationDto>
{
    private readonly IShoppingCartService _cartService;

    public ValidateCartHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartValidationDto> Handle(ValidateCartQuery request, CancellationToken cancellationToken)
    {
        return await _cartService.ValidateCartAsync(request.UserId, request.SessionId);
    }
}

// Calculate Shipping
public record CalculateShippingQuery(string Address, string? UserId, string? SessionId) : IRequest<decimal>;

public class CalculateShippingHandler : IRequestHandler<CalculateShippingQuery, decimal>
{
    private readonly IShoppingCartService _cartService;

    public CalculateShippingHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<decimal> Handle(CalculateShippingQuery request, CancellationToken cancellationToken)
    {
        return await _cartService.CalculateShippingAsync(request.UserId, request.SessionId, request.Address);
    }
}

// Migrate Cart
public record MigrateCartCommand(MigrateCartDto Request) : IRequest<CartResponseDto>;

public class MigrateCartHandler : IRequestHandler<MigrateCartCommand, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public MigrateCartHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(MigrateCartCommand command, CancellationToken cancellationToken)
    {
        return await _cartService.MigrateSessionCartToUserAsync(command.Request);
    }
}

// Bulk Cart Operation
public record BulkCartOperationCommand(BulkCartOperationDto Request, string? UserId) : IRequest<CartResponseDto>;

public class BulkCartOperationHandler : IRequestHandler<BulkCartOperationCommand, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public BulkCartOperationHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(BulkCartOperationCommand command, CancellationToken cancellationToken)
    {
        return await _cartService.BulkCartOperationAsync(command.Request, command.UserId);
    }
}

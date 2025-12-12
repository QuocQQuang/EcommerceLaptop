using MediatR;
using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.API.Features.Cart;

// Get Cart
public record GetCartQuery(string? UserId, string? SessionId) : IRequest<CartResponseDto>;

public class GetCartHandler : IRequestHandler<GetCartQuery, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public GetCartHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        return await _cartService.GetCartAsync(request.UserId, request.SessionId);
    }
}

// Get Cart Summary
public record GetCartSummaryQuery(string? UserId, string? SessionId) : IRequest<CartSummaryDto>;

public class GetCartSummaryHandler : IRequestHandler<GetCartSummaryQuery, CartSummaryDto>
{
    private readonly IShoppingCartService _cartService;

    public GetCartSummaryHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartSummaryDto> Handle(GetCartSummaryQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetCartAsync(request.UserId, request.SessionId);
        return cart.Summary;
    }
}

// Get Cart Item Count
public record GetCartItemCountQuery(string? UserId, string? SessionId) : IRequest<int>;

public class GetCartItemCountHandler : IRequestHandler<GetCartItemCountQuery, int>
{
    private readonly IShoppingCartService _cartService;

    public GetCartItemCountHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<int> Handle(GetCartItemCountQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetCartAsync(request.UserId, request.SessionId);
        return cart.Summary.ItemCount;
    }
}

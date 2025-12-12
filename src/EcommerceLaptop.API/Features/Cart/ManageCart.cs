using MediatR;
using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.API.Features.Cart;

// Add to Cart
public record AddToCartCommand(AddToCartDto Request, string? UserId) : IRequest<CartResponseDto>;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public AddToCartHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(AddToCartCommand command, CancellationToken cancellationToken)
    {
        return await _cartService.AddToCartAsync(command.Request, command.UserId);
    }
}

// Update Cart Item
public record UpdateCartItemCommand(UpdateCartItemDto Request, string? UserId) : IRequest<CartResponseDto>;

public class UpdateCartItemHandler : IRequestHandler<UpdateCartItemCommand, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public UpdateCartItemHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(UpdateCartItemCommand command, CancellationToken cancellationToken)
    {
        return await _cartService.UpdateCartItemAsync(command.Request, command.UserId);
    }
}

// Remove From Cart
public record RemoveFromCartCommand(RemoveFromCartDto Request, string? UserId) : IRequest<CartResponseDto>;

public class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public RemoveFromCartHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(RemoveFromCartCommand command, CancellationToken cancellationToken)
    {
        return await _cartService.RemoveFromCartAsync(command.Request, command.UserId);
    }
}

// Clear Cart
public record ClearCartCommand(string? UserId, string? SessionId) : IRequest<CartResponseDto>;

public class ClearCartHandler : IRequestHandler<ClearCartCommand, CartResponseDto>
{
    private readonly IShoppingCartService _cartService;

    public ClearCartHandler(IShoppingCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartResponseDto> Handle(ClearCartCommand command, CancellationToken cancellationToken)
    {
        return await _cartService.ClearCartAsync(command.UserId, command.SessionId);
    }
}

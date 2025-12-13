using MediatR;
using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Cart;

// Get Cart
public record GetCartQuery(string? UserId, string? SessionId) : IRequest<CartResponseDto>;

public class GetCartHandler : IRequestHandler<GetCartQuery, CartResponseDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetCartHandler> _logger;

    public GetCartHandler(ApplicationDbContext context, ILogger<GetCartHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartResponseDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        return await CartHelpers.GetCartAsync(_context, _logger, request.UserId, request.SessionId);
    }
}

// Get Cart Summary
public record GetCartSummaryQuery(string? UserId, string? SessionId) : IRequest<CartSummaryDto>;

public class GetCartSummaryHandler : IRequestHandler<GetCartSummaryQuery, CartSummaryDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetCartSummaryHandler> _logger;

    public GetCartSummaryHandler(ApplicationDbContext context, ILogger<GetCartSummaryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartSummaryDto> Handle(GetCartSummaryQuery request, CancellationToken cancellationToken)
    {
        var cart = await CartHelpers.GetCartAsync(_context, _logger, request.UserId, request.SessionId);
        return cart.Summary;
    }
}

// Get Cart Item Count
public record GetCartItemCountQuery(string? UserId, string? SessionId) : IRequest<int>;

public class GetCartItemCountHandler : IRequestHandler<GetCartItemCountQuery, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetCartItemCountHandler> _logger;

    public GetCartItemCountHandler(ApplicationDbContext context, ILogger<GetCartItemCountHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> Handle(GetCartItemCountQuery request, CancellationToken cancellationToken)
    {
        var cart = await CartHelpers.GetCartAsync(_context, _logger, request.UserId, request.SessionId);
        return cart.Summary.ItemCount;
    }
}

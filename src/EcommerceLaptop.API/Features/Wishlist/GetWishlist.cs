using MediatR;
using EcommerceLaptop.Core.DTOs.Wishlist;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Wishlist;

// Get Wishlist
public record GetWishlistQuery(int UserId) : IRequest<WishlistResultDto>;

public class GetWishlistHandler : IRequestHandler<GetWishlistQuery, WishlistResultDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetWishlistHandler> _logger;

    public GetWishlistHandler(ApplicationDbContext context, ILogger<GetWishlistHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WishlistResultDto> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var wishlistItems = await _context.WishlistItems
                .Include(w => w.Product)
                    .ThenInclude(p => p.Images)
                .Where(w => w.UserId == request.UserId)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new WishlistItemDto
                {
                    Id = w.Id,
                    ProductId = w.ProductId,
                    ProductName = w.Product.Name,
                    Price = w.Product.Price,
                    Images = w.Product.Images.Select(img => new WishlistProductImageDto
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl ?? "",
                        AltText = img.AltText ?? "",
                        IsPrimary = img.IsPrimary
                    }).ToList(),
                    AddedAt = w.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new WishlistResultDto
            {
                Items = wishlistItems,
                TotalCount = wishlistItems.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wishlist for user {UserId}", request.UserId);
            throw;
        }
    }
}

// Check in Wishlist
public record CheckInWishlistQuery(int UserId, int ProductId) : IRequest<bool>;

public class CheckInWishlistHandler : IRequestHandler<CheckInWishlistQuery, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckInWishlistHandler> _logger;

    public CheckInWishlistHandler(ApplicationDbContext context, ILogger<CheckInWishlistHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(CheckInWishlistQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.WishlistItems
                .AnyAsync(w => w.UserId == request.UserId && w.ProductId == request.ProductId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking product {ProductId} in wishlist for user {UserId}", request.ProductId, request.UserId);
            throw;
        }
    }
}

// Get Wishlist Count
public record GetWishlistCountQuery(int UserId) : IRequest<int>;

public class GetWishlistCountHandler : IRequestHandler<GetWishlistCountQuery, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetWishlistCountHandler> _logger;

    public GetWishlistCountHandler(ApplicationDbContext context, ILogger<GetWishlistCountHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> Handle(GetWishlistCountQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.WishlistItems
                .CountAsync(w => w.UserId == request.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wishlist count for user {UserId}", request.UserId);
            throw;
        }
    }
}

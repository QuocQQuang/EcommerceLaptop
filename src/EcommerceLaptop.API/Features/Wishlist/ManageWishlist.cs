using MediatR;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Wishlist;

// Add to Wishlist
public record AddToWishlistCommand(int UserId, int ProductId) : IRequest;

public class AddToWishlistHandler : IRequestHandler<AddToWishlistCommand>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AddToWishlistHandler> _logger;

    public AddToWishlistHandler(ApplicationDbContext context, ILogger<AddToWishlistHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(AddToWishlistCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == command.ProductId, cancellationToken);
            if (!productExists)
            {
                throw new KeyNotFoundException("Khng tm thy sn phm");
            }

            var exists = await _context.WishlistItems
                .AnyAsync(w => w.UserId == command.UserId && w.ProductId == command.ProductId, cancellationToken);

            if (exists)
            {
                return; // Already in wishlist, treat as success
            }

            var wishlistItem = new WishlistItem
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                ProductId = command.ProductId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductId} to wishlist for user {UserId}", command.ProductId, command.UserId);
            throw;
        }
    }
}

// Remove from Wishlist
public record RemoveFromWishlistCommand(int UserId, int ProductId) : IRequest;

public class RemoveFromWishlistHandler : IRequestHandler<RemoveFromWishlistCommand>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RemoveFromWishlistHandler> _logger;

    public RemoveFromWishlistHandler(ApplicationDbContext context, ILogger<RemoveFromWishlistHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(RemoveFromWishlistCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var wishlistItems = await _context.WishlistItems
                .Where(w => w.UserId == command.UserId && w.ProductId == command.ProductId)
                .ToListAsync(cancellationToken);

            if (!wishlistItems.Any())
            {
                throw new KeyNotFoundException("Sn phm khng c trong danh sch yu thch");
            }

            // Remove all matching items just in case of duplicates (though schema should prevent it)
            _context.WishlistItems.RemoveRange(wishlistItems);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from wishlist for user {UserId}", command.ProductId, command.UserId);
            throw;
        }
    }
}

// Clear Wishlist
public record ClearWishlistCommand(int UserId) : IRequest;

public class ClearWishlistHandler : IRequestHandler<ClearWishlistCommand>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClearWishlistHandler> _logger;

    public ClearWishlistHandler(ApplicationDbContext context, ILogger<ClearWishlistHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(ClearWishlistCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var wishlistItems = await _context.WishlistItems
                .Where(w => w.UserId == command.UserId)
                .ToListAsync(cancellationToken);

            if (wishlistItems.Any())
            {
                _context.WishlistItems.RemoveRange(wishlistItems);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing wishlist for user {UserId}", command.UserId);
            throw;
        }
    }
}

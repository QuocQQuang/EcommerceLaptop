using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs.Wishlist;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.Infrastructure.Services;

public class WishlistService : IWishlistService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(ApplicationDbContext context, ILogger<WishlistService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WishlistResultDto> GetWishlistAsync(int userId)
    {
        try
        {
            var wishlistItems = await _context.WishlistItems
                .Include(w => w.Product)
                    .ThenInclude(p => p.Images)
                .Where(w => w.UserId == userId)
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
                .ToListAsync();

            return new WishlistResultDto
            {
                Items = wishlistItems,
                TotalCount = wishlistItems.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wishlist for user {UserId}", userId);
            throw;
        }
    }

    public async Task AddToWishlistAsync(int userId, int productId)
    {
        try
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists)
            {
                throw new KeyNotFoundException("Không tìm thấy sản phẩm");
            }

            var exists = await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (exists)
            {
                return; // Already in wishlist, treat as success
            }

            var wishlistItem = new WishlistItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductId} to wishlist for user {UserId}", productId, userId);
            throw;
        }
    }

    public async Task RemoveFromWishlistAsync(int userId, int productId)
    {
        try
        {
            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (wishlistItem == null)
            {
                throw new KeyNotFoundException("Sản phẩm không có trong danh sách yêu thích");
            }

            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from wishlist for user {UserId}", productId, userId);
            throw;
        }
    }

    public async Task ClearWishlistAsync(int userId)
    {
        try
        {
            var wishlistItems = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .ToListAsync();

            if (wishlistItems.Any())
            {
                _context.WishlistItems.RemoveRange(wishlistItems);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing wishlist for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CheckInWishlistAsync(int userId, int productId)
    {
        try
        {
            return await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking product {ProductId} in wishlist for user {UserId}", productId, userId);
            throw;
        }
    }

    public async Task<int> GetWishlistCountAsync(int userId)
    {
        try
        {
            return await _context.WishlistItems
                .CountAsync(w => w.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wishlist count for user {UserId}", userId);
            throw;
        }
    }
}

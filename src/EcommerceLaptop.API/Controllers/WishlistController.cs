using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetWishlist()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Khng th xc thc ngi dng" });
                }

                var wishlistItems = await _context.WishlistItems
                    .Include(w => w.Product)
                        .ThenInclude(p => p.Images)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.CreatedAt)
                    .Select(w => new
                    {
                        id = w.Id,
                        productId = w.ProductId,
                        product = new
                        {
                            id = w.Product.Id,
                            name = w.Product.Name,
                            price = w.Product.Price,
                            sku = w.Product.SKU,
                            brand = w.Product.Brand,
                            isActive = w.Product.IsActive,
                            images = w.Product.Images.Select(img => new
                            {
                                id = img.Id,
                                imageUrl = img.ImageUrl,
                                altText = img.AltText,
                                isPrimary = img.IsPrimary
                            }).ToList()
                        },
                        addedAt = w.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    items = wishlistItems,
                    totalCount = wishlistItems.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi ly danh sch yu thch", error = ex.Message });
            }
        }

        [HttpPost("{productId}")]
        public async Task<ActionResult> AddToWishlist(int productId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Khng th xc thc ngi dng" });
                }

                // Check if product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound(new { message = "Khng tm thy sn phm" });
                }

                // Check if already in wishlist
                var existingItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (existingItem != null)
                {
                    return BadRequest(new { message = "Sn phm  c trong danh sch yu thch" });
                }

                // Add to wishlist
                var wishlistItem = new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WishlistItems.Add(wishlistItem);
                await _context.SaveChangesAsync();

                return Ok(new { message = " thm vo danh sch yu thch" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi thm vo danh sch yu thch", error = ex.Message });
            }
        }

        [HttpDelete("{productId}")]
        public async Task<ActionResult> RemoveFromWishlist(int productId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Khng th xc thc ngi dng" });
                }

                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (wishlistItem == null)
                {
                    return NotFound(new { message = "Sn phm khng c trong danh sch yu thch" });
                }

                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                return Ok(new { message = " xa khi danh sch yu thch" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi xa khi danh sch yu thch", error = ex.Message });
            }
        }

        [HttpPost("clear")]
        public async Task<ActionResult> ClearWishlist()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Khng th xc thc ngi dng" });
                }

                var wishlistItems = await _context.WishlistItems
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                _context.WishlistItems.RemoveRange(wishlistItems);
                await _context.SaveChangesAsync();

                return Ok(new { message = " xa ton b danh sch yu thch" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi xa danh sch yu thch", error = ex.Message });
            }
        }

        [HttpGet("check/{productId}")]
        public async Task<ActionResult> CheckInWishlist(int productId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Khng th xc thc ngi dng" });
                }

                var isInWishlist = await _context.WishlistItems
                    .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

                return Ok(new { isInWishlist = isInWishlist });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi kim tra danh sch yu thch", error = ex.Message });
            }
        }

        [HttpGet("count")]
        public async Task<ActionResult> GetWishlistCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Khng th xc thc ngi dng" });
                }

                var count = await _context.WishlistItems
                    .CountAsync(w => w.UserId == userId);

                return Ok(new { count = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi m danh sch yu thch", error = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}
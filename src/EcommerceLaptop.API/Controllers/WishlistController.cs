using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Services;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
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

                var result = await _wishlistService.GetWishlistAsync(userId.Value);

                return Ok(new
                {
                    items = result.Items.Select(w => new
                    {
                        id = w.Id,
                        productId = w.ProductId,
                        productName = w.ProductName,
                        price = w.Price,
                        images = w.Images.Select(img => new
                        {
                            id = img.Id,
                            imageUrl = img.ImageUrl,
                            altText = img.AltText,
                            isPrimary = img.IsPrimary
                        }),
                        addedAt = w.AddedAt
                    }),
                    totalCount = result.TotalCount
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

                await _wishlistService.AddToWishlistAsync(userId.Value, productId);
                return Ok(new { message = " thm vo danh sch yu thch" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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

                await _wishlistService.RemoveFromWishlistAsync(userId.Value, productId);
                return Ok(new { message = " xa khi danh sch yu thch" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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

                await _wishlistService.ClearWishlistAsync(userId.Value);
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

                var isInWishlist = await _wishlistService.CheckInWishlistAsync(userId.Value, productId);
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

                var count = await _wishlistService.GetWishlistCountAsync(userId.Value);
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
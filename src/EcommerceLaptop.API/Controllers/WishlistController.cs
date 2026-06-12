using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Services;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WishlistController(IWishlistService wishlistService, ILogger<WishlistController> logger)
    : BaseApiController(logger)
{
    private readonly IWishlistService _wishlistService = wishlistService;

    [HttpGet]
    public async Task<ActionResult> GetWishlist()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Không thể xác thực người dùng" });
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

    [HttpPost("{productId}")]
    public async Task<ActionResult> AddToWishlist(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Không thể xác thực người dùng" });
        }

        await _wishlistService.AddToWishlistAsync(userId.Value, productId);
        return Ok(new { message = "Đã thêm vào danh sách yêu thích" });
    }

    [HttpDelete("{productId}")]
    public async Task<ActionResult> RemoveFromWishlist(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Không thể xác thực người dùng" });
        }

        await _wishlistService.RemoveFromWishlistAsync(userId.Value, productId);
        return Ok(new { message = "Đã xóa khỏi danh sách yêu thích" });
    }

    [HttpPost("clear")]
    public async Task<ActionResult> ClearWishlist()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Không thể xác thực người dùng" });
        }

        await _wishlistService.ClearWishlistAsync(userId.Value);
        return Ok(new { message = "Đã xóa toàn bộ danh sách yêu thích" });
    }

    [HttpGet("check/{productId}")]
    public async Task<ActionResult> CheckInWishlist(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Không thể xác thực người dùng" });
        }

        var isInWishlist = await _wishlistService.CheckInWishlistAsync(userId.Value, productId);
        return Ok(new { isInWishlist = isInWishlist });
    }

    [HttpGet("count")]
    public async Task<ActionResult> GetWishlistCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Không thể xác thực người dùng" });
        }

        var count = await _wishlistService.GetWishlistCountAsync(userId.Value);
        return Ok(new { count = count });
    }

}

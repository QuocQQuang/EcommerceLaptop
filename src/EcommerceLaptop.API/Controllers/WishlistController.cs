using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using EcommerceLaptop.API.Features.Wishlist;

namespace EcommerceLaptop.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WishlistController(ISender sender, ILogger<WishlistController> logger)
    : BaseApiController(logger)
{
    private readonly ISender _sender = sender;

    [HttpGet]
    public async Task<ActionResult> GetWishlist()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Khng th xc thc ngi dng" });
        }

        var result = await _sender.Send(new GetWishlistQuery(userId.Value));

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
            return Unauthorized(new { message = "Khng th xc thc ngi dng" });
        }

        await _sender.Send(new AddToWishlistCommand(userId.Value, productId));
        return Ok(new { message = " thm vo danh sch yu thch" });
    }

    [HttpDelete("{productId}")]
    public async Task<ActionResult> RemoveFromWishlist(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Khng th xc thc ngi dng" });
        }

        await _sender.Send(new RemoveFromWishlistCommand(userId.Value, productId));
        return Ok(new { message = " xa khi danh sch yu thch" });
    }

    [HttpPost("clear")]
    public async Task<ActionResult> ClearWishlist()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Khng th xc thc ngi dng" });
        }

        await _sender.Send(new ClearWishlistCommand(userId.Value));
        return Ok(new { message = " xa ton b danh sch yu thch" });
    }

    [HttpGet("check/{productId}")]
    public async Task<ActionResult> CheckInWishlist(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Khng th xc thc ngi dng" });
        }

        var isInWishlist = await _sender.Send(new CheckInWishlistQuery(userId.Value, productId));
        return Ok(new { isInWishlist = isInWishlist });
    }

    [HttpGet("count")]
    public async Task<ActionResult> GetWishlistCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Khng th xc thc ngi dng" });
        }

        var count = await _sender.Send(new GetWishlistCountQuery(userId.Value));
        return Ok(new { count = count });
    }
}

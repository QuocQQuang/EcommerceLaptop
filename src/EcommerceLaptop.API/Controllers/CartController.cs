using EcommerceLaptop.Core.DTOs.Cart;
using MediatR;
using EcommerceLaptop.API.Features.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Controller for managing shopping cart operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CartController(ISender sender, ILogger<CartController> logger) : BaseApiController(logger)
{
    private readonly ISender _sender = sender;

    /// <summary>
    /// Get current user's cart or guest session cart
    /// </summary>
    /// <param name="sessionId">Session ID for guest users</param>
    /// <returns>Cart details with items and summary</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> GetCart([FromQuery] string? sessionId = null)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var query = new GetCartQuery(userId, sessionId);
        var cart = await _sender.Send(query);
        return Ok(cart);
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    /// <param name="addToCartDto">Item details to add</param>
    /// <returns>Updated cart details</returns>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> AddToCart([FromBody] AddToCartDto addToCartDto)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(addToCartDto.SessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var command = new AddToCartCommand(addToCartDto, userId);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Update cart item quantity or configuration
    /// </summary>
    /// <param name="updateCartDto">Update details</param>
    /// <returns>Updated cart details</returns>
    [HttpPut("items")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> UpdateCartItem([FromBody] UpdateCartItemDto updateCartDto)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(updateCartDto.SessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var command = new UpdateCartItemCommand(updateCartDto, userId);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    /// <param name="removeFromCartDto">Remove details</param>
    /// <returns>Updated cart details</returns>
    [HttpDelete("items")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> RemoveFromCart([FromBody] RemoveFromCartDto removeFromCartDto)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(removeFromCartDto.SessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var command = new RemoveFromCartCommand(removeFromCartDto, userId);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Remove specific cart item by ID
    /// </summary>
    /// <param name="cartItemId">ID of cart item to remove</param>
    /// <param name="sessionId">Session ID for guest users</param>
    /// <returns>Updated cart details</returns>
    [HttpDelete("items/{cartItemId:int}")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> RemoveCartItem(int cartItemId, [FromQuery] string? sessionId = null)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var removeDto = new RemoveFromCartDto 
        { 
            CartItemId = cartItemId, 
            SessionId = sessionId 
        };

        var command = new RemoveFromCartCommand(removeDto, userId);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Clear entire cart
    /// </summary>
    /// <param name="sessionId">Session ID for guest users</param>
    /// <returns>Empty cart</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> ClearCart([FromQuery] string? sessionId = null)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var command = new ClearCartCommand(userId, sessionId);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Apply discount code to cart
    /// </summary>
    /// <param name="discountDto">Discount code details</param>
    /// <returns>Updated cart with discount applied</returns>
    [HttpPost("discount")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> ApplyDiscount([FromBody] ApplyDiscountDto discountDto)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(discountDto.SessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var command = new ApplyDiscountCommand(discountDto, userId);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Remove discount code from cart
    /// </summary>
    /// <param name="sessionId">Session ID for guest users</param>
    /// <returns>Updated cart without discount</returns>
    [HttpDelete("discount")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> RemoveDiscount([FromQuery] string? sessionId = null)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var command = new RemoveDiscountCommand(userId, sessionId);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Validate cart items for availability, pricing, and stock
    /// </summary>
    /// <param name="sessionId">Session ID for guest users</param>
    /// <returns>Validation results</returns>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(CartValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartValidationDto>> ValidateCart([FromQuery] string? sessionId = null)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var query = new ValidateCartQuery(userId, sessionId);
        var validation = await _sender.Send(query);
        return Ok(validation);
    }

    /// <summary>
    /// Migrate guest cart session to authenticated user cart
    /// </summary>
    /// <param name="migrateDto">Migration details</param>
    /// <returns>Migrated user cart</returns>
    [HttpPost("migrate")]
    [Authorize]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> MigrateSessionCart([FromBody] MigrateCartDto migrateDto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Override userId from token for security
        migrateDto.UserId = userId;

        var command = new MigrateCartCommand(migrateDto);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Perform bulk cart operations
    /// </summary>
    /// <param name="bulkOperation">Bulk operation details</param>
    /// <returns>Updated cart after all operations</returns>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(CartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponseDto>> BulkCartOperation([FromBody] BulkCartOperationDto bulkOperation)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(bulkOperation.SessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var command = new BulkCartOperationCommand(bulkOperation, userId);
        var cart = await _sender.Send(command);
        return Ok(cart);
    }

    /// <summary>
    /// Calculate shipping cost for current cart
    /// </summary>
    /// <param name="shippingAddress">Shipping address for calculation</param>
    /// <param name="sessionId">Session ID for guest users</param>
    /// <returns>Calculated shipping cost</returns>
    [HttpPost("shipping")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<decimal>> CalculateShipping([FromBody] string shippingAddress, [FromQuery] string? sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(shippingAddress))
        {
            return BadRequest("Shipping address is required");
        }

        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var query = new CalculateShippingQuery(shippingAddress, userId, sessionId);
        var shippingCost = await _sender.Send(query);
        return Ok(shippingCost);
    }

    /// <summary>
    /// Get cart summary (lightweight version)
    /// </summary>
    /// <param name="sessionId">Session ID for guest users</param>
    /// <returns>Cart summary without full item details</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(CartSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartSummaryDto>> GetCartSummary([FromQuery] string? sessionId = null)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var query = new GetCartSummaryQuery(userId, sessionId);
        var summary = await _sender.Send(query);
        return Ok(summary);
    }

    /// <summary>
    /// Get cart item count
    /// </summary>
    /// <param name="sessionId">Session ID for guest users</param>
    /// <returns>Number of items in cart</returns>
    [HttpGet("count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> GetCartItemCount([FromQuery] string? sessionId = null)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Either authentication or session ID is required");
        }

        var query = new GetCartItemCountQuery(userId, sessionId);
        var count = await _sender.Send(query);
        return Ok(count);
    }

    #region Private Helper Methods

    /// <summary>
    /// Get user ID from JWT token claims
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    private string? GetUserId()
    {
        // Use BaseApiController method if available, or this implementation
        // Since we inherit from BaseApiController but checking if it has GetUserId() returning string?
        // BaseApiController typically has GetCurrentUserId() returning int? or similar.
        // Let's stick to this local implementation to minimize risk, or adapt if BaseApiController has similar.
        // But wait, BaseApiController is used, usually it has GetUserId/GetCurrentUserId.
        // Let's use User.FindFirst directly to match existing logic exactly.
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    #endregion
}

using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Core.Services;
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
public class CartController : ControllerBase
{
    private readonly IShoppingCartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(IShoppingCartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

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
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.GetCartAsync(userId, sessionId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for user {UserId} or session {SessionId}", GetUserId(), sessionId);
            return StatusCode(500, "An error occurred while retrieving the cart");
        }
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
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(addToCartDto.SessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.AddToCartAsync(addToCartDto, userId);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when adding to cart: {@AddToCartDto}", addToCartDto);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when adding to cart: {@AddToCartDto}", addToCartDto);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart: {@AddToCartDto}", addToCartDto);
            return StatusCode(500, "An error occurred while adding the item to cart");
        }
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
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(updateCartDto.SessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.UpdateCartItemAsync(updateCartDto, userId);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating cart item: {@UpdateCartDto}", updateCartDto);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating cart item: {@UpdateCartDto}", updateCartDto);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item: {@UpdateCartDto}", updateCartDto);
            return StatusCode(500, "An error occurred while updating the cart item");
        }
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
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(removeFromCartDto.SessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.RemoveFromCartAsync(removeFromCartDto, userId);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when removing from cart: {@RemoveFromCartDto}", removeFromCartDto);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart: {@RemoveFromCartDto}", removeFromCartDto);
            return StatusCode(500, "An error occurred while removing the item from cart");
        }
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
        try
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

            var cart = await _cartService.RemoveFromCartAsync(removeDto, userId);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when removing cart item {CartItemId}", cartItemId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
            return StatusCode(500, "An error occurred while removing the cart item");
        }
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
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.ClearCartAsync(userId, sessionId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user {UserId} or session {SessionId}", GetUserId(), sessionId);
            return StatusCode(500, "An error occurred while clearing the cart");
        }
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
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(discountDto.SessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.ApplyDiscountAsync(discountDto, userId);
            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid discount code: {@DiscountDto}", discountDto);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount: {@DiscountDto}", discountDto);
            return StatusCode(500, "An error occurred while applying the discount");
        }
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
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.RemoveDiscountAsync(userId, sessionId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing discount for user {UserId} or session {SessionId}", GetUserId(), sessionId);
            return StatusCode(500, "An error occurred while removing the discount");
        }
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
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var validation = await _cartService.ValidateCartAsync(userId, sessionId);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart for user {UserId} or session {SessionId}", GetUserId(), sessionId);
            return StatusCode(500, "An error occurred while validating the cart");
        }
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
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Override userId from token for security
            migrateDto.UserId = userId;

            var cart = await _cartService.MigrateSessionCartToUserAsync(migrateDto);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating cart: {@MigrateDto}", migrateDto);
            return StatusCode(500, "An error occurred while migrating the cart");
        }
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
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(bulkOperation.SessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.BulkCartOperationAsync(bulkOperation, userId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk cart operation: {@BulkOperation}", bulkOperation);
            return StatusCode(500, "An error occurred while performing bulk cart operations");
        }
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
        try
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

            var shippingCost = await _cartService.CalculateShippingAsync(userId, sessionId, shippingAddress);
            return Ok(shippingCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating shipping for user {UserId} or session {SessionId}", GetUserId(), sessionId);
            return StatusCode(500, "An error occurred while calculating shipping");
        }
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
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.GetCartAsync(userId, sessionId);
            return Ok(cart.Summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart summary for user {UserId} or session {SessionId}", GetUserId(), sessionId);
            return StatusCode(500, "An error occurred while retrieving the cart summary");
        }
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
        try
        {
            var userId = GetUserId();
            
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Either authentication or session ID is required");
            }

            var cart = await _cartService.GetCartAsync(userId, sessionId);
            return Ok(cart.Summary.ItemCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart count for user {UserId} or session {SessionId}", GetUserId(), sessionId);
            return StatusCode(500, "An error occurred while retrieving the cart count");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Get user ID from JWT token claims
    /// </summary>
    /// <returns>User ID or null if not authenticated</returns>
    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    #endregion
}

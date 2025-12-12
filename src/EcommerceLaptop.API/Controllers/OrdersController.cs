using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using EcommerceLaptop.API.Features.Orders;

namespace EcommerceLaptop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(
    ISender sender, 
    ILogger<OrdersController> logger) : BaseApiController(logger)
{
    private readonly ISender _sender = sender;
    private new readonly ILogger<OrdersController> _logger = logger;

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(AtomicCheckoutResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AtomicCheckoutResult>> AtomicCheckout([FromBody] CreateOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        // Email verification is handled in the Command Handler
        var command = new CreateOrderCommand(request) { AuthenticatedUserId = userId };
        var result = await _sender.Send(command);

        if (result.IsSuccess)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result.ErrorMessage);
        }
    }

    [HttpPost("from-cart/{cartId}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> CreateFromCart(string cartId, [FromBody] CreateOrderRequest request)
    {
        if (!int.TryParse(cartId, out int parsedCartId))
        {
            return BadRequest("Invalid cart ID format");
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            return BadRequest("Shipping address is required");
        }

        try 
        {
            var command = new CreateOrderFromCartCommand(parsedCartId, request) { AuthenticatedUserId = userId };
            var order = await _sender.Send(command);
            return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
        }
        catch (InvalidOperationException ex) // Catch validation errors like email not verified
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDetailsDto>> GetOrder(int orderId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        // Note: Existing controller logic didn't verify ownership. 
        // We preserve this behavior but the Service ideally should handle it.
        var query = new GetOrderByIdQuery(orderId) { AuthenticatedUserId = userId };
        var order = await _sender.Send(query);
        return Ok(order);
    }

    [HttpPut("{orderId}/status")]
    [Authorize(Policy = "RequirePermission:orders:write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] UpdateStatusRequest request)
    {
        _logger.LogInformation(" UPDATE ORDER STATUS - OrderId: {OrderId}, NewStatus: {NewStatus}, Reason: {Reason}",
            orderId, request.NewStatus, request.Reason);

        var command = new UpdateOrderStatusCommand(orderId, request);
        var success = await _sender.Send(command);
        
        if (!success)
        {
            _logger.LogWarning(" UPDATE FAILED - OrderId: {OrderId}, NewStatus: {NewStatus}",
                orderId, request.NewStatus);
            return NotFound("Order not found or invalid status transition");
        }

        _logger.LogInformation(" UPDATE SUCCESS - OrderId: {OrderId}, NewStatus: {NewStatus}",
            orderId, request.NewStatus);
        return Ok();
    }

    [HttpDelete("{orderId}")]
    [Authorize(Policy = "RequirePermission:orders:write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var command = new CancelOrderCommand(orderId, request) { AuthenticatedUserId = userId };
        var success = await _sender.Send(command);
        
        if (!success)
        {
            // Check if order exists (fetch via query) to provide better error
            // Using MediatR for this too
            var order = await _sender.Send(new GetOrderByIdQuery(orderId));
            
            if (order == null)
            {
                return NotFound("Order not found");
            }

            return BadRequest("Khng th hy n hng  thanh ton. Vui lng lin h h tr  c hon tin.");
        }

        return Ok();
    }

    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(EcommerceLaptop.Core.DTOs.PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcommerceLaptop.Core.DTOs.PagedResult<OrderDto>>> GetCustomerOrders(int customerId, int page = 1, int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == null || userId != customerId)
        {
            return Unauthorized("Access denied");
        }

        var query = new GetCustomerOrdersQuery(customerId, page, pageSize) { AuthenticatedUserId = userId };
        var orders = await _sender.Send(query);
        return Ok(orders);
    }

    #region Admin Endpoints

    /// <summary>
    /// Get all orders for admin management (includes customer and payment data)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "RequirePermission:orders:read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAdminOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = new GetAdminOrdersQuery(page, pageSize, search, status, customerId);
        var result = await _sender.Send(query);

        return Ok(new
        {
            orders = result.Items,
            totalCount = result.TotalCount,
            currentPage = result.Page,
            totalPages = result.TotalPages,
            pageSize = result.PageSize,
            summary = new
            {
                totalAmount = result.Items?.Sum(o => o.TotalAmount) ?? 0,
                averageOrderValue = result.Items?.Count > 0 ? result.Items.Average(o => o.TotalAmount) : 0
            }
        });
    }

    #endregion
}

using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;

namespace EcommerceLaptop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for order operations
public class OrdersController(
    IOrderService orderService,
    IPaymentOrchestrator paymentOrchestrator,
    ILogger<OrdersController> logger) : BaseApiController(logger)
{
    private readonly IOrderService _orderService = orderService;
    private readonly IPaymentOrchestrator _paymentOrchestrator = paymentOrchestrator;
    private new readonly ILogger<OrdersController> _logger = logger;

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(AtomicCheckoutResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AtomicCheckoutResult>> AtomicCheckout([FromBody] CreateOrderRequest request)
    {
        var customerId = GetUserId();
        if (string.IsNullOrEmpty(customerId))
        {
            return Unauthorized();
        }

        request.CustomerId = int.Parse(customerId);

        // TODO: Email confirmation check temporarily disabled
        // Enforce email confirmation before allowing checkout
        /*
        var isEmailConfirmed = User.Claims.FirstOrDefault(c => c.Type == "email_confirmed")?.Value;
        if (string.IsNullOrEmpty(isEmailConfirmed))
        {
            // Fallback: require service to verify user record
            var authService = HttpContext.RequestServices
                .GetRequiredService<EcommerceLaptop.Core.Services.IAuthService>();
            var profile = await authService.GetUserProfileAsync(request.CustomerId);
            if (profile == null || profile.IsEmailVerified == false)
            {
                return BadRequest("Tài khoản của bạn chưa xác thực email. Vui lòng xác thực email trước khi mua hàng.");
            }
        }
        else if (!bool.TryParse(isEmailConfirmed, out var confirmed) || !confirmed)
        {
            return BadRequest("Tài khoản của bạn chưa xác thực email. Vui lòng xác thực email trước khi mua hàng.");
        }
        */
        var result = await _orderService.CreateOrderAndInitializePaymentAsync(request);

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
        // Validate and parse cartId
        if (!int.TryParse(cartId, out int parsedCartId))
        {
            return BadRequest("Invalid cart ID format");
        }

        var customerId = GetUserId();
        if (string.IsNullOrEmpty(customerId))
        {
            return Unauthorized();
        }

        // TODO: Email confirmation check temporarily disabled
        // Enforce email confirmation for order creation from cart as well
        /*
        var isEmailConfirmed2 = User.Claims.FirstOrDefault(c => c.Type == "email_confirmed")?.Value;
        if (string.IsNullOrEmpty(isEmailConfirmed2))
        {
            var authService = HttpContext.RequestServices
                .GetRequiredService<EcommerceLaptop.Core.Services.IAuthService>();
            var profile = await authService.GetUserProfileAsync(int.Parse(customerId));
            if (profile == null || profile.IsEmailVerified == false)
            {
                return BadRequest("Tài khoản của bạn chưa xác thực email. Vui lòng xác thực email trước khi mua hàng.");
            }
        }
        else if (!bool.TryParse(isEmailConfirmed2, out var confirmed2) || !confirmed2)
        {
            return BadRequest("Tài khoản của bạn chưa xác thực email. Vui lòng xác thực email trước khi mua hàng.");
        }
        */

        // Validate shipping address
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            return BadRequest("Shipping address is required");
        }

        var order = await _orderService.CreateOrderFromCartAsync(parsedCartId, int.Parse(customerId), request.ShippingAddress);
        return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDetailsDto>> GetOrder(int orderId)
    {
        var customerId = GetUserId();
        if (string.IsNullOrEmpty(customerId))
        {
            return Unauthorized();
        }

        var order = await _orderService.GetOrderDetailsAsync(orderId);
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

        var success = await _orderService.UpdateOrderStatusAsync(orderId, request);
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
        var customerId = GetUserId();
        if (string.IsNullOrEmpty(customerId))
        {
            return Unauthorized();
        }

        var success = await _orderService.CancelOrderAsync(orderId, request);
        if (!success)
        {
            // Check if order exists to provide better error message
            var order = await _orderService.GetOrderDetailsAsync(orderId);
            if (order == null)
            {
                return NotFound("Order not found");
            }

            // If order exists but cancellation failed, it's likely because it's already paid
            return BadRequest("Không thể hủy đơn hàng đã thanh toán. Vui lòng liên hệ hỗ trợ để được hoàn tiền.");
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
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId) || int.Parse(userId) != customerId)
        {
            return Unauthorized("Access denied");
        }

        var orders = await _orderService.GetCustomerOrdersAsync(customerId, page, pageSize);
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

        var result = await _orderService.GetEnhancedAdminOrdersAsync(page, pageSize, search, status, customerId);

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

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.Utilities.Payment;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Payment Controller for handling payment gateway operations
/// Implements secure payment processing following PCI DSS guidelines
/// and Uncle Bob's Clean Code principles
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : BaseApiController
{
    private readonly IPaymentOrchestrator _paymentOrchestrator;
    private readonly IOrderService _orderService;

    public PaymentController(
        IPaymentOrchestrator paymentOrchestrator,
        IOrderService orderService,
        ILogger<PaymentController> logger) : base(logger)
    {
        _paymentOrchestrator = paymentOrchestrator;
        _orderService = orderService;
    }

    /// <summary>
    /// Initialize payment for an order using the specified gateway
    /// </summary>
    /// <param name="request">Payment initialization request</param>
    /// <returns>Payment initialization result with redirect URL or payment details</returns>
    [HttpPost("initialize")]
    [ProducesResponseType(typeof(PaymentInitializationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentInitializationResult>> InitializePayment(
        [FromBody] InitializePaymentApiRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User authentication required");
            }

            _logger.LogInformation("Initializing payment for order {OrderId} using gateway {Gateway} by user {UserId}",
                request.OrderId, request.Gateway, userId.Value);

            // Fetch order to get amount and currency from database
            var order = await _orderService.GetOrderDetailsAsync(request.OrderId);

            // Determine currency based on gateway
            var paymentCurrency = request.Gateway switch
            {
                PaymentGateway.SePay => "VND",
                _ => "USD" // PayPal, Stripe, etc. use USD
            };

            // Convert amount from USD (stored in DB) to target currency
            var paymentAmount = order.TotalAmount; // This is in USD from DB
            if (paymentCurrency == "VND")
            {
                paymentAmount = PaymentHelpers.ConvertCurrency(paymentAmount, "USD", "VND");
            }

            _logger.LogInformation("Payment currency conversion: Order {OrderId}, Amount USD: {AmountUSD}, Target Currency: {Currency}, Converted Amount: {ConvertedAmount}",
                request.OrderId, order.TotalAmount, paymentCurrency, paymentAmount);

            // Validate payment request first
            var validation = await _paymentOrchestrator.ValidatePaymentRequestAsync(
                request.OrderId, request.Gateway, paymentAmount, paymentCurrency);

            if (!validation.IsValid)
            {
                return BadRequest(new ValidationProblemDetails
                {
                    Title = "Payment validation failed",
                    Detail = string.Join(", ", validation.ValidationErrors)
                });
            }

            // Initialize payment with converted amount and currency
            var result = await _paymentOrchestrator.InitializePaymentAsync(
                request.OrderId,
                request.Gateway,
                request.Method,
                paymentAmount,
                paymentCurrency,
                request.ReturnUrl,
                request.CancelUrl);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Payment initialized successfully for order {OrderId} with transaction {TransactionId}",
                    request.OrderId, result.TransactionId);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Payment initialization failed for order {OrderId}: {ErrorMessage}",
                    request.OrderId, result.ErrorMessage);
                return BadRequest(new ProblemDetails
                {
                    Title = "Payment initialization failed",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing payment for order {OrderId}", request.OrderId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while processing your payment request",
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Retry payment for a pending/unpaid order with enhanced retry logic
    /// </summary>
    /// <param name="request">Retry payment request with order ID and gateway details</param>
    /// <returns>Payment initialization result with retry information</returns>
    [HttpPost("retry")]
    [ProducesResponseType(typeof(PaymentInitializationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentInitializationResult>> RetryPayment(
        [FromBody] RetryPaymentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User authentication required");
            }

            _logger.LogInformation("Retrying payment for order {OrderId} using gateway {Gateway} by user {UserId}",
                request.OrderId, request.Gateway, userId.Value);

            // Fetch order to validate status
            var order = await _orderService.GetOrderDetailsAsync(request.OrderId);
            if (order.Status != OrderStatus.Pending)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Order not eligible for retry",
                    Detail = "Only pending orders can be retried",
                    Status = StatusCodes.Status409Conflict
                });
            }

            // Check if already paid (via order status)
            if (order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Order already paid",
                    Detail = "This order has already been paid",
                    Status = StatusCodes.Status409Conflict
                });
            }

            // Check retry limits
            var retryCount = await PaymentRetryHelpers.GetRetryCountAsync(request.OrderId, _orderService);
            var maxRetries = 3; // Could be configurable
            var retryDelay = TimeSpan.FromMinutes(1); // Could be configurable

            if (retryCount >= maxRetries)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new ProblemDetails
                {
                    Title = "Retry limit exceeded",
                    Detail = $"Maximum retry attempts ({maxRetries}) exceeded for this order",
                    Status = StatusCodes.Status429TooManyRequests
                });
            }

            // Check if enough time has passed since last retry
            var lastRetryTime = await PaymentRetryHelpers.GetLastRetryTimeAsync(request.OrderId, _orderService);
            if (lastRetryTime.HasValue && DateTime.UtcNow - lastRetryTime.Value < retryDelay)
            {
                var remainingTime = retryDelay - (DateTime.UtcNow - lastRetryTime.Value);
                return StatusCode(StatusCodes.Status429TooManyRequests, new ProblemDetails
                {
                    Title = "Retry too soon",
                    Detail = $"Please wait {remainingTime.TotalSeconds:F0} seconds before retrying",
                    Status = StatusCodes.Status429TooManyRequests
                });
            }

            // Determine currency based on gateway
            var paymentCurrency = request.Gateway switch
            {
                PaymentGateway.SePay => "VND",
                _ => "USD" // PayPal, Stripe, etc. use USD
            };

            // Convert amount from USD (stored in DB) to target currency
            var paymentAmount = order.TotalAmount; // This is in USD from DB
            if (paymentCurrency == "VND")
            {
                paymentAmount = PaymentHelpers.ConvertCurrency(paymentAmount, "USD", "VND");
            }

            // Validate payment request
            var validation = await _paymentOrchestrator.ValidatePaymentRequestAsync(
                request.OrderId, request.Gateway, paymentAmount, paymentCurrency);

            if (!validation.IsValid)
            {
                return BadRequest(new ValidationProblemDetails
                {
                    Title = "Payment validation failed",
                    Detail = string.Join(", ", validation.ValidationErrors)
                });
            }

            // Initialize payment as retry with converted amount and currency
            var result = await _paymentOrchestrator.InitializePaymentAsync(
                request.OrderId,
                request.Gateway,
                request.Method,
                paymentAmount,
                paymentCurrency,
                request.ReturnUrl,
                request.CancelUrl);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Payment retry initialized successfully for order {OrderId} with transaction {TransactionId}",
                    request.OrderId, result.TransactionId);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Payment retry failed for order {OrderId}: {ErrorMessage}",
                    request.OrderId, result.ErrorMessage);
                return BadRequest(new ProblemDetails
                {
                    Title = "Payment retry failed",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying payment for order {OrderId}", request.OrderId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while retrying your payment",
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Complete payment processing after customer action
    /// </summary>
    /// <param name="request">Payment completion request</param>
    /// <returns>Payment result</returns>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResult>> CompletePayment(
        [FromBody] CompletePaymentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User authentication required");
            }

            _logger.LogInformation("Completing payment for order {OrderId} with transaction {TransactionId} by user {UserId}",
                request.OrderId, request.TransactionId, userId.Value);

            var result = await _paymentOrchestrator.CompletePaymentAsync(request);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Payment completed successfully for order {OrderId}. Status: {Status}",
                    request.OrderId, result.Status);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Payment completion failed for order {OrderId}: {ErrorMessage}",
                    request.OrderId, result.ErrorMessage);
                return BadRequest(new ProblemDetails
                {
                    Title = "Payment completion failed",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing payment for order {OrderId}", request.OrderId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while completing your payment",
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Capture PayPal payment after customer approval
    /// </summary>
    /// <param name="request">PayPal capture request</param>
    /// <returns>Payment capture result</returns>
    [HttpPost("paypal/capture")]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResult>> CapturePayPalPayment(
        [FromBody] CapturePayPalPaymentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User authentication required");
            }

            _logger.LogInformation("Capturing PayPal payment for order {OrderId} with PayPal order ID {PayPalOrderId} by user {UserId}",
                request.OrderId, request.PayPalOrderId, userId.Value);

            // Create process payment request
            var processPaymentRequest = new ProcessPaymentRequest
            {
                OrderId = request.OrderId,
                TransactionId = request.PayPalOrderId,
                Gateway = PaymentGateway.PayPal,
                GatewaySpecificData = new Dictionary<string, object>
                {
                    ["paypal_order_id"] = request.PayPalOrderId,
                    ["payer_id"] = request.PayerID ?? string.Empty
                }
            };

            var result = await _paymentOrchestrator.ProcessPaymentAsync(processPaymentRequest);

            if (result.IsSuccess)
            {
                _logger.LogInformation("PayPal payment captured successfully for order {OrderId}. Transaction: {TransactionId}",
                    request.OrderId, result.TransactionId);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("PayPal payment capture failed for order {OrderId}: {ErrorMessage}",
                    request.OrderId, result.ErrorMessage);
                return BadRequest(new ProblemDetails
                {
                    Title = "PayPal payment capture failed",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal payment for order {OrderId}", request.OrderId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while capturing PayPal payment",
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Process refund for a completed payment
    /// </summary>
    /// <param name="paymentId">Payment ID to refund</param>
    /// <param name="request">Refund request details</param>
    /// <returns>Refund result</returns>
    [HttpPost("{paymentId:int}/refund")]
    [Authorize(Roles = "Admin,Manager")] // Only admins can process refunds
    [ProducesResponseType(typeof(RefundResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RefundResult>> ProcessRefund(
        int paymentId,
        [FromBody] ProcessRefundApiRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User authentication required");
            }

            var userName = User.Identity?.Name ?? "Unknown";

            _logger.LogInformation("Processing refund for payment {PaymentId} by user {UserId} ({UserName})",
                paymentId, userId.Value, userName);

            var result = await _paymentOrchestrator.RefundPaymentAsync(
                paymentId,
                request.Amount,
                request.Reason,
                userName);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Refund processed successfully for payment {PaymentId}. Refund ID: {RefundId}",
                    paymentId, result.RefundId);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Refund processing failed for payment {PaymentId}: {ErrorMessage}",
                    paymentId, result.ErrorMessage);
                return BadRequest(new ProblemDetails
                {
                    Title = "Refund processing failed",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while processing the refund",
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get payment status and details
    /// </summary>
    /// <param name="paymentId">Payment ID to check</param>
    /// <returns>Payment status result</returns>
    [HttpGet("{paymentId:int}/status")]
    [ProducesResponseType(typeof(PaymentStatusResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentStatusResult>> GetPaymentStatus(int paymentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User authentication required");
            }

            _logger.LogInformation("Getting payment status for payment {PaymentId} by user {UserId}",
                paymentId, userId.Value);

            var result = await _paymentOrchestrator.GetPaymentStatusAsync(paymentId);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to get payment status for payment {PaymentId}: {ErrorMessage}",
                    paymentId, result.ErrorMessage);
                return NotFound(new ProblemDetails
                {
                    Title = "Payment not found",
                    Detail = result.ErrorMessage,
                    Status = StatusCodes.Status404NotFound
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for payment {PaymentId}", paymentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while retrieving payment status",
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get available payment methods for a specific gateway and amount
    /// </summary>
    /// <param name="gateway">Payment gateway</param>
    /// <param name="currency">Currency code (e.g., VND, USD)</param>
    /// <param name="amount">Payment amount</param>
    /// <returns>List of available payment methods</returns>
    [HttpGet("methods")]
    [AllowAnonymous] // Allow anonymous access for payment method discovery
    [ProducesResponseType(typeof(IReadOnlyList<PaymentMethodInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<PaymentMethodInfo>>> GetAvailablePaymentMethods(
        [FromQuery, Required] PaymentGateway gateway,
        [FromQuery, Required] string currency,
        [FromQuery, Required] decimal amount)
    {
        try
        {
            if (amount <= 0)
            {
                return BadRequest(new ValidationProblemDetails
                {
                    Title = "Invalid amount",
                    Detail = "Amount must be greater than zero"
                });
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                return BadRequest(new ValidationProblemDetails
                {
                    Title = "Invalid currency",
                    Detail = "Currency code is required"
                });
            }

            _logger.LogInformation("Getting available payment methods for gateway {Gateway}, currency {Currency}, amount {Amount}",
                gateway, currency, amount);

            var methods = await _paymentOrchestrator.GetAvailablePaymentMethodsAsync(gateway, currency, amount);

            return Ok(methods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available payment methods for gateway {Gateway}", gateway);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while retrieving payment methods",
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get health status of payment gateways
    /// </summary>
    /// <param name="gateway">Optional specific gateway to check (if not provided, checks all)</param>
    /// <returns>Gateway health status</returns>
    [HttpGet("health")]
    [Authorize(Roles = "Admin,Manager")] // Only admins can check gateway health
    [ProducesResponseType(typeof(PaymentGatewayHealthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<PaymentGatewayHealthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetGatewayHealth([FromQuery] PaymentGateway? gateway = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User authentication required");
            }

            if (gateway.HasValue)
            {
                _logger.LogInformation("Checking health for gateway {Gateway} by user {UserId}",
                    gateway.Value, userId.Value);

                var result = await _paymentOrchestrator.GetGatewayHealthAsync(gateway.Value);
                return Ok(result);
            }
            else
            {
                _logger.LogInformation("Checking health for all gateways by user {UserId}", userId.Value);

                var allGateways = Enum.GetValues<PaymentGateway>();
                var healthResults = new List<PaymentGatewayHealthResult>();

                foreach (var gw in allGateways)
                {
                    var result = await _paymentOrchestrator.GetGatewayHealthAsync(gw);
                    healthResults.Add(result);
                }

                return Ok(healthResults);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gateway health status");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while checking gateway health",
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Handle webhook callbacks from payment gateways
    /// </summary>
    /// <param name="gateway">Payment gateway sending the webhook</param>
    /// <returns>Webhook processing result</returns>
    [HttpPost("webhook/{gateway}")]
    [AllowAnonymous] // Webhooks come from external services
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ProcessWebhook(PaymentGateway gateway)
    {
        try
        {
            _logger.LogInformation("Received webhook from gateway {Gateway}", gateway);

            // Read the raw request body
            string payload;
            using (var reader = new StreamReader(Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            // Extract headers
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

            var result = await _paymentOrchestrator.ProcessWebhookAsync(gateway, payload, headers);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Webhook processed successfully for gateway {Gateway}", gateway);

                // Some gateways expect specific response content
                if (!string.IsNullOrEmpty(result.ResponseContent))
                {
                    return Ok(result.ResponseContent);
                }

                return Ok();
            }
            else
            {
                _logger.LogWarning("Webhook processing failed for gateway {Gateway}: {ErrorMessage}",
                    gateway, result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for gateway {Gateway}", gateway);
            return StatusCode(StatusCodes.Status500InternalServerError, "Webhook processing failed");
        }
    }
}

#region API Request Models

public class InitializePaymentApiRequest
{
    [Required(ErrorMessage = "Order ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Order ID must be greater than 0")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Payment gateway is required")]
    public PaymentGateway Gateway { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public PaymentMethod Method { get; set; }

    // Amount and currency are now handled by backend - removed from API request
    // [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    // public decimal? Amount { get; set; }

    // [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be 3 characters")]
    // public string? Currency { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [Url(ErrorMessage = "Return URL must be a valid URL")]
    public string ReturnUrl { get; set; } = string.Empty;

    [Url(ErrorMessage = "Cancel URL must be a valid URL")]
    public string CancelUrl { get; set; } = string.Empty;
}

/// <summary>
/// API request model for refund processing
/// </summary>
public class ProcessRefundApiRequest
{
    [Required(ErrorMessage = "Refund amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Refund reason is required")]
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; } = string.Empty;
}

#endregion

/// <summary>
/// API request model for retrying payment on pending orders
/// </summary>
public class RetryPaymentRequest
{
    [Required(ErrorMessage = "Order ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Order ID must be greater than 0")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Payment gateway is required")]
    public PaymentGateway Gateway { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public PaymentMethod Method { get; set; }

    [Url(ErrorMessage = "Return URL must be a valid URL")]
    public string ReturnUrl { get; set; } = string.Empty;

    [Url(ErrorMessage = "Cancel URL must be a valid URL")]
    public string CancelUrl { get; set; } = string.Empty;
}
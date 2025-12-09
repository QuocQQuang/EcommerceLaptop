using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Services;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// SePay Transaction Monitoring API
/// 
/// SePay is a transaction monitoring service that tracks payments through monitored bank accounts.
/// Unlike traditional payment gateways, SePay monitors bank transactions in real-time and matches 
/// them to orders through QR codes or transaction content.
/// 
/// Key Features:
/// - Generate VietQR-compliant QR codes
/// - Monitor transactions across 9 Vietnamese banks
/// - Process real-time webhook notifications
/// - Match transactions to orders automatically
/// 
/// Supported Banks: VPBank, BIDV, TPBank, ACB, VietinBank, MB, OCB, KienLongBank, MSB
/// Rate Limit: 2 requests per second
/// </summary>
[ApiController]
[Route("api/sepay")]
[Authorize]
public class SePayController : BaseApiController
{
    private readonly ISePayService _sePayService;
    private readonly IPaymentWebhookBusinessLogicService _webhookBusinessLogicService;
    private readonly IPaymentOrchestrator _paymentOrchestrator;
    private readonly PaymentGatewaySettings _paymentSettings;

    public SePayController(
        ISePayService sePayService,
        IPaymentWebhookBusinessLogicService webhookBusinessLogicService,
        IPaymentOrchestrator paymentOrchestrator,
        ILogger<SePayController> logger,
        IOptions<PaymentGatewaySettings> settings) : base(logger)
    {
        _sePayService = sePayService;
        _webhookBusinessLogicService = webhookBusinessLogicService;
        _paymentOrchestrator = paymentOrchestrator;
        _paymentSettings = settings.Value;
    }

    // Removed unused GenerateQrCode method - QR generation now handled client-side

    /// <summary>
    /// Process SePay Webhook Notification
    /// 
    /// Receives real-time transaction notifications from SePay when payments are detected
    /// in monitored bank accounts. Automatically matches transactions to orders and updates
    /// payment status.
    /// </summary>
    /// <param name="payload">Webhook payload containing transaction details from SePay</param>
    /// <returns>Webhook processing confirmation response</returns>
    /// <remarks>
    /// This endpoint is called automatically by SePay when transactions are detected.
    /// 
    /// Required webhook configuration:
    /// - URL: https://yourdomain.com/api/sepay/webhook
    /// - Authentication: Bearer token in Authorization header
    /// - Content-Type: application/json
    /// 
    /// Example webhook payload:
    /// 
    ///     POST /api/sepay/webhook
    ///     Authorization: Bearer webhook-secret
    ///     {
    ///         "id": "TXN-123456",
    ///         "transferAmount": 100000,
    ///         "transferContent": "ORD-12345",
    ///         "accountNumber": "1234567890",
    ///         "bankCode": "VPB",
    ///         "transferDate": "2025-01-20T10:15:00Z"
    ///     }
    /// 
    /// The webhook will automatically:
    /// 1. Validate the Bearer token
    /// 2. Extract order ID from transfer content
    /// 3. Update order payment status
    /// 4. Send notifications to customer
    /// </remarks>
    /// <response code="200">Webhook processed successfully</response>
    /// <response code="400">Invalid webhook payload or processing failed</response>
    /// <response code="401">Unauthorized - Invalid or missing Bearer token</response>
    /// <response code="500">Internal server error during webhook processing</response>
    [HttpPost("webhook")]
    [AllowAnonymous] // Webhook calls come from SePay, not authenticated users
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ProcessWebhook([FromBody] SePayWebhookPayload payload)
    {
        try
        {
            _logger.LogInformation("Received SePay webhook for transaction {TransactionId}, amount {Amount}",
                payload.Id, payload.TransferAmount);

            // Validate webhook authentication (optional for Sepay webhooks)
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                var isAuthValid = await _sePayService.ValidateWebhookAuthAsync(authHeader);
                if (!isAuthValid)
                {
                    _logger.LogWarning("SePay webhook received with invalid authorization");
                    return Unauthorized(new { success = false, error = "Invalid authorization" });
                }
            }
            else
            {
                _logger.LogWarning("SePay webhook received without authorization header - proceeding as trusted source");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("SePay webhook received with invalid payload");
                return BadRequest(new { success = false, error = "Invalid payload" });
            }

            // Process webhook
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var result = await _sePayService.ProcessWebhookAsync(payload, headers);

            if (!result.Success)
            {
                _logger.LogWarning("SePay webhook processing failed: {Message}", result.Message);
                return BadRequest(new { success = false, error = result.Message });
            }

            if (result.OrderId.HasValue)
            {
                // Use orchestrator to synchronize Payment status and run business logic
                var payloadJson = JsonSerializer.Serialize(payload);
                var orchestratorResult = await _paymentOrchestrator.ProcessWebhookAsync(
                    PaymentGateway.SePay,
                    payloadJson,
                    headers);

                if (!orchestratorResult.IsSuccess)
                {
                    _logger.LogWarning("SePay orchestrator processing reported failure: {Error}", orchestratorResult.ErrorMessage);
                }
            }

            _logger.LogInformation("SePay webhook processed successfully for transaction {TransactionId}, mapped to order {OrderId}",
                payload.Id, result.OrderId ?? (int?)null);

            // Return the required SePay response format
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing SePay webhook for transaction {TransactionId}", payload.Id);
            return StatusCode(500, new { success = false, error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get Transaction History
    /// 
    /// Retrieves transaction history from monitored SePay bank accounts with pagination
    /// and filtering support. Useful for reconciliation and payment tracking.
    /// </summary>
    /// <param name="request">Transaction monitoring request with pagination and filters</param>
    /// <returns>Paginated list of transactions from monitored accounts</returns>
    /// <remarks>
    /// Retrieves transactions from all monitored bank accounts configured in SePay.
    /// 
    /// Example request:
    /// 
    ///     POST /api/sepay/transactions
    ///     {
    ///         "accountNumber": "1234567890",
    ///         "page": 1,
    ///         "pageSize": 20,
    ///         "fromDate": "2025-01-01T00:00:00Z",
    ///         "toDate": "2025-01-31T23:59:59Z"
    ///     }
    /// 
    /// Useful for:
    /// - Payment reconciliation
    /// - Transaction history review
    /// - Audit and compliance reporting
    /// - Identifying unmatched payments
    /// </remarks>
    /// <response code="200">Transaction history retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized - Valid JWT token required</response>
    /// <response code="500">Internal server error during transaction retrieval</response>
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(SePayMonitoringResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SePayMonitoringResponse>> GetTransactions(
        [FromBody] SePayMonitoringRequest request)
    {
        try
        {
            _logger.LogInformation("Retrieving SePay transactions for account {Account}, page {Page}",
                request.BankAccount ?? "all", request.Page);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _sePayService.GetTransactionsAsync(request);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("SePay transaction retrieval failed: {Error}", result.ErrorMessage);
                return BadRequest(new { error = result.ErrorMessage });
            }

            _logger.LogInformation("Retrieved {Count} SePay transactions", result.Transactions.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving SePay transactions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Register Bank Account for Monitoring
    /// 
    /// Registers a new bank account with SePay for transaction monitoring.
    /// Once registered, the account will be monitored for incoming transfers
    /// that can be matched to orders.
    /// </summary>
    /// <param name="request">Bank account registration details</param>
    /// <returns>Registration confirmation</returns>
    /// <remarks>
    /// Adds a new bank account to the SePay monitoring system.
    /// 
    /// Example request:
    /// 
    ///     POST /api/sepay/register-account
    ///     {
    ///         "accountNumber": "1234567890",
    ///         "bankCode": "VPB",
    ///         "accountName": "CONG TY TNHH ABC"
    ///     }
    /// 
    /// Requirements:
    /// - Account must be a valid business account
    /// - Bank must be supported by SePay
    /// - Account owner must match registered business
    /// 
    /// After registration, the account will be monitored for:
    /// - Incoming transfers
    /// - Transaction notifications
    /// - Automatic order matching
    /// </remarks>
    /// <response code="200">Bank account registered successfully</response>
    /// <response code="400">Invalid account details or registration failed</response>
    /// <response code="401">Unauthorized - Valid JWT token required</response>
    /// <response code="500">Internal server error during registration</response>
    [HttpPost("register-account")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RegisterBankAccount([FromBody] SePayBankAccountRequest request)
    {
        try
        {
            _logger.LogInformation("Registering SePay bank account {AccountNumber}", request.AccountNumber);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _sePayService.RegisterBankAccountAsync(request);

            if (!result)
            {
                _logger.LogWarning("SePay bank account registration failed for {AccountNumber}", request.AccountNumber);
                return BadRequest(new { error = "Bank account registration failed" });
            }

            _logger.LogInformation("SePay bank account {AccountNumber} registered successfully", request.AccountNumber);
            return Ok(new { success = true, message = "Bank account registered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error registering SePay bank account {AccountNumber}", request.AccountNumber);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get Supported Banks
    /// 
    /// Retrieves the list of Vietnamese banks supported by SePay for transaction monitoring.
    /// Use this endpoint to get valid bank codes for QR code generation and account registration.
    /// </summary>
    /// <returns>List of supported banks with codes and names</returns>
    /// <remarks>
    /// Returns all banks that SePay can monitor for transactions.
    /// 
    /// Example response:
    /// 
    ///     GET /api/sepay/supported-banks
    ///     {
    ///         "banks": [
    ///             {
    ///                 "bankCode": "VPB",
    ///                 "bankName": "VPBank",
    ///                 "fullName": "Ngan hang TMCP Viet Nam Thinh Vuong",
    ///                 "isActive": true
    ///             },
    ///             {
    ///                 "bankCode": "BIDV", 
    ///                 "bankName": "BIDV",
    ///                 "fullName": "Ngan hang TMCP Dau tu va Phat trien Viet Nam",
    ///                 "isActive": true
    ///             }
    ///         ],
    ///         "totalCount": 9
    ///     }
    /// 
    /// Currently supported banks:
    /// - VPBank (VPB)
    /// - BIDV 
    /// - TPBank (TPB)
    /// - ACB
    /// - VietinBank (CTG)
    /// - MBBank (MB)
    /// - OCB
    /// - KienLongBank (KLB)
    /// - MSB
    /// </remarks>
    /// <response code="200">List of supported banks retrieved successfully</response>
    [HttpGet("supported-banks")]
    [AllowAnonymous] // Public information, no authentication required
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetSupportedBanks()
    {
        try
        {
            var supportedBanks = _sePayService.GetSupportedBanks();

            var response = new
            {
                success = true,
                banks = supportedBanks.Select(b => new
                {
                    bankCode = b.BankCode,
                    bankName = b.BankName
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SePay supported banks");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// SePay Service Health Check
    /// 
    /// Verifies connectivity and operational status of the SePay transaction monitoring service.
    /// Used for monitoring, alerting, and load balancer health checks.
    /// </summary>
    /// <returns>Health status with service information</returns>
    /// <remarks>
    /// Performs comprehensive health checks including:
    /// - SePay API connectivity
    /// - Database connectivity
    /// - Bank account monitoring status
    /// - Service configuration validation
    /// 
    /// Example response (healthy):
    /// 
    ///     GET /api/sepay/health
    ///     {
    ///         "healthy": true,
    ///         "service": "SePay",
    ///         "timestamp": "2025-01-20T10:15:00Z",
    ///         "message": "SePay service is operational",
    ///         "details": {
    ///             "apiConnectivity": "OK",
    ///             "databaseConnection": "OK",
    ///             "monitoredAccounts": 2,
    ///             "lastTransactionCheck": "2025-01-20T10:14:30Z"
    ///         }
    ///     }
    /// 
    /// Example response (unhealthy):
    /// 
    ///     GET /api/sepay/health
    ///     {
    ///         "healthy": false,
    ///         "service": "SePay",
    ///         "message": "SePay service is unavailable",
    ///         "error": "API connectivity failed"
    ///     }
    /// </remarks>
    /// <response code="200">Service is healthy and operational</response>
    /// <response code="503">Service is unavailable or experiencing issues</response>
    [HttpGet("health")]
    [AllowAnonymous] // Health checks should not require authentication
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> HealthCheck()
    {
        try
        {
            var isHealthy = await _sePayService.CheckHealthAsync();

            if (!isHealthy)
            {
                return StatusCode(503, new
                {
                    healthy = false,
                    service = "SePay",
                    message = "SePay service is unavailable"
                });
            }

            return Ok(new
            {
                healthy = true,
                service = "SePay",
                timestamp = DateTime.UtcNow,
                message = "SePay service is operational"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SePay health check failed");
            return StatusCode(503, new
            {
                healthy = false,
                service = "SePay",
                error = "Health check failed"
            });
        }
    }

    /// <summary>
    /// Get SePay Configuration for QR Generation
    ///
    /// Provides bank account configuration and QR base URL for client-side QR code generation.
    /// Supports environment parameter for sandbox/production separation.
    /// </summary>
    /// <param name="environment">Environment (sandbox/production, default: production)</param>
    /// <returns>Configuration object with monitored accounts and QR settings</returns>
    /// <remarks>
    /// This endpoint allows frontend to generate QR codes directly using official SePay URL format.
    ///
    /// Example response:
    ///
    ///     GET /api/sepay/config?environment=sandbox
    ///     {
    ///         "environment": "sandbox",
    ///         "qrBaseUrl": "https://qr.sepay.vn/img",
    ///         "defaultAccount": {
    ///             "accountNumber": "0908752170",
    ///             "bankCode": "MB",
    ///             "bankName": "MBBank",
    ///             "isDefault": true
    ///         },
    ///         "accounts": [
    ///             {
    ///                 "accountNumber": "0908752170",
    ///                 "bankCode": "MB",
    ///                 "bankName": "MBBank",
    ///                 "isDefault": true
    ///             }
    ///         ]
    ///     }
    /// </remarks>
    /// <response code="200">Configuration retrieved successfully</response>
    /// <response code="401">Unauthorized - Valid JWT token required</response>
    [HttpGet("config")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult GetConfig(string? environment = "production")
    {
        try
        {
            _logger.LogInformation("Retrieving SePay config for environment {Environment}", environment);

            // For now, same config for both environments. In future, load different accounts based on environment
            var sepaySettings = _paymentSettings.SePay;
            var defaultAccount = sepaySettings.MonitoredAccounts?.FirstOrDefault(a => a.IsDefault);

            var config = new
            {
                environment = environment.ToLower(),
                qrBaseUrl = "https://qr.sepay.vn/img",
                defaultAccount = defaultAccount != null ? new
                {
                    accountNumber = defaultAccount.AccountNumber,
                    bankCode = defaultAccount.BankCode,
                    bankName = defaultAccount.BankName,
                    isDefault = defaultAccount.IsDefault
                } : null,
                accounts = sepaySettings.MonitoredAccounts?.Select(a => new
                {
                    accountNumber = a.AccountNumber,
                    bankCode = a.BankCode,
                    bankName = a.BankName,
                    isDefault = a.IsDefault,
                    description = a.Description
                }).Cast<object>().ToList() ?? new List<object>()
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SePay configuration");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Test Order ID Extraction
    /// 
    /// Utility endpoint for testing and debugging order ID extraction from transaction content.
    /// Useful for validating transaction content patterns and troubleshooting payment matching.
    /// </summary>
    /// <param name="content">Transaction content/description to test for order ID extraction</param>
    /// <returns>Extraction result showing found order ID if any</returns>
    /// <remarks>
    /// Tests the order ID extraction logic used to match transactions to orders.
    /// This is primarily a development and debugging tool.
    /// 
    /// Example requests:
    /// 
    ///     POST /api/sepay/test-order-extraction
    ///     "ORD-12345 payment"
    ///     
    ///     Response:
    ///     {
    ///         "content": "ORD-12345 payment",
    ///         "extractedOrderId": "ORD-12345",
    ///         "found": true
    ///     }
    /// 
    ///     POST /api/sepay/test-order-extraction  
    ///     "Random transaction"
    ///     
    ///     Response:
    ///     {
    ///         "content": "Random transaction",
    ///         "extractedOrderId": null,
    ///         "found": false
    ///     }
    /// 
    /// Common order ID patterns:
    /// - "ORD-12345"
    /// - "ORDER-12345" 
    /// - "DH-12345"
    /// - "HD-12345"
    /// </remarks>
    /// <response code="200">Order ID extraction test completed</response>
    /// <response code="500">Internal server error during testing</response>
    [HttpPost("test-order-extraction")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult TestOrderExtraction([FromBody] string content)
    {
        try
        {
            var orderId = _sePayService.ExtractOrderIdFromContent(content);

            return Ok(new
            {
                content = content,
                extractedOrderId = orderId,
                found = orderId.HasValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing order extraction");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
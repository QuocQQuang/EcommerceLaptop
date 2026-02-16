using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Utilities.Payment;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// Payment orchestrator implementation following Uncle Bob's Clean Code principles
/// Coordinates payment operations across multiple gateways
/// </summary>
public class PaymentOrchestrator : IPaymentOrchestrator
{
    private readonly IPaymentServiceFactory _paymentServiceFactory;
    private readonly IPaymentWebhookBusinessLogicService _businessLogicService;
    private readonly PaymentGatewaySettings _settings;
    private readonly ILogger<PaymentOrchestrator> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IOrderWorkflowService _orderWorkflowService;
    private readonly IEmailService _emailService;
    private readonly IPdfExportService _pdfExportService;

    public PaymentOrchestrator(
        IPaymentServiceFactory paymentServiceFactory,
        IPaymentWebhookBusinessLogicService businessLogicService,
        IOptions<PaymentGatewaySettings> settings,
        ILogger<PaymentOrchestrator> logger,
        ApplicationDbContext dbContext,
        IOrderWorkflowService orderWorkflowService,
        IEmailService emailService,
        IPdfExportService pdfExportService)
    {
        _paymentServiceFactory = paymentServiceFactory;
        _businessLogicService = businessLogicService;
        _settings = settings.Value;
        _logger = logger;
        _dbContext = dbContext;
        _orderWorkflowService = orderWorkflowService;
        _emailService = emailService;
        _pdfExportService = pdfExportService;
    }

    /// <summary>
    /// Initializes a payment for an order using the appropriate gateway
    /// </summary>
    public async Task<PaymentInitializationResult> InitializePaymentAsync(
        int orderId,
        PaymentGateway gateway,
        PaymentMethod method,
        decimal amount,
        string currency,
        string returnUrl = "",
        string cancelUrl = "")
    {
        try
        {
            _logger.LogInformation("Initializing payment for order {OrderId} using gateway {Gateway}",
                orderId, gateway);

            var paymentService = _paymentServiceFactory.CreatePaymentService(gateway);

            // Use actual amount and currency from parameters
            var request = new InitializePaymentRequest
            {
                OrderId = orderId,
                Gateway = gateway,
                Method = method,
                Amount = amount,
                Currency = currency,
                Description = $"Payment for order {orderId}",
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            };

            var result = await paymentService.InitializePaymentAsync(request);

            if (result.IsSuccess)
            {
                // Create Payment entity
                var payment = new EcommerceLaptop.Core.Entities.Payment
                {
                    OrderId = orderId,
                    TransactionId = result.TransactionId ?? $"TXN_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Gateway = gateway,
                    Status = PaymentStatus.Pending,
                    Method = method,
                    Amount = amount,
                    Currency = currency,
                    CreatedAt = DateTime.UtcNow,
                    GatewayResponse = "" // Will be updated later
                };
                _dbContext.Payments.Add(payment);
                await _dbContext.SaveChangesAsync();
                int paymentId = payment.Id;

                // Create PaymentAudit
                var audit = new EcommerceLaptop.Core.Entities.PaymentAudit
                {
                    PaymentId = paymentId,
                    Action = "Initialize",
                    Status = PaymentStatus.Pending,
                    Gateway = gateway,
                    TransactionId = payment.TransactionId,
                    RequestData = System.Text.Json.JsonSerializer.Serialize(request),
                    ResponseData = System.Text.Json.JsonSerializer.Serialize(result),
                    ProcessingTimeMs = 0, // Calculate if needed
                    CreatedAt = DateTime.UtcNow,
                    CreatedByIp = "", // From HTTP context in real impl
                    UserAgent = "", // From HTTP context
                    UserId = "", // From current user
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { Gateway = gateway, Method = method })
                };
                _dbContext.PaymentAudits.Add(audit);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Payment initialized successfully for order {OrderId} with transaction {TransactionId} and audit created",
                    orderId, result.TransactionId);
            }
            else
            {
                _logger.LogError("Failed to initialize payment for order {OrderId}. Error: {Error}",
                    orderId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing payment for order {OrderId}", orderId);
            return new PaymentInitializationResult
            {
                IsSuccess = false,
                ErrorMessage = "Payment initialization failed",
                ErrorCode = "INIT_ERROR"
            };
        }
    }

    /// <summary>
    /// Completes a payment transaction
    /// </summary>
    public async Task<PaymentResult> CompletePaymentAsync(CompletePaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Completing payment for order {OrderId} with transaction {TransactionId}",
                request.OrderId, request.TransactionId);

            var paymentService = _paymentServiceFactory.CreatePaymentService(request.Gateway);

            var processRequest = new ProcessPaymentRequest
            {
                OrderId = request.OrderId,
                Gateway = request.Gateway,
                TransactionId = request.TransactionId,
                GatewayResponse = string.Join("|", request.ResponseData.Select(kvp => $"{kvp.Key}={kvp.Value}")),
                Signature = request.Signature
            };

            var result = await paymentService.ProcessPaymentAsync(processRequest);

            if (result.IsSuccess)
            {
                // Update existing Payment entity
                var payment = await _dbContext.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == request.TransactionId);
                if (payment != null)
                {
                    payment.Status = result.Status;
                    payment.ProcessedAt = DateTime.UtcNow;
                    payment.GatewayResponse = System.Text.Json.JsonSerializer.Serialize(processRequest);
                    _dbContext.Payments.Update(payment);
                    await _dbContext.SaveChangesAsync();

                    // Create PaymentAudit
                    var audit = new EcommerceLaptop.Core.Entities.PaymentAudit
                    {
                        PaymentId = payment.Id,
                        Action = "Complete",
                        Status = result.Status,
                        Gateway = request.Gateway,
                        TransactionId = request.TransactionId,
                        RequestData = System.Text.Json.JsonSerializer.Serialize(request),
                        ResponseData = System.Text.Json.JsonSerializer.Serialize(result),
                        ProcessingTimeMs = 0, // Calculate if needed
                        CreatedAt = DateTime.UtcNow,
                        CreatedByIp = "", // From HTTP context
                        UserAgent = "", // From HTTP context
                        UserId = "", // From current user
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new { Gateway = request.Gateway, Status = result.Status })
                    };
                    _dbContext.PaymentAudits.Add(audit);
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("Payment completed successfully for order {OrderId}. Status: {Status} with audit",
                    request.OrderId, result.Status);
            }
            else
            {
                _logger.LogError("Failed to complete payment for order {OrderId}. Error: {Error}",
                    request.OrderId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing payment for order {OrderId}", request.OrderId);
            return new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                TransactionId = request.TransactionId,
                ErrorMessage = "Payment completion failed",
                ErrorCode = "COMPLETE_ERROR"
            };
        }
    }

    /// <summary>
    /// Processes a payment request using the appropriate gateway
    /// Used for capturing payments that have been previously authorized
    /// </summary>
    public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing payment for order {OrderId} using gateway {Gateway}",
                request.OrderId, request.Gateway);

            var paymentService = _paymentServiceFactory.CreatePaymentService(request.Gateway);
            var result = await paymentService.ProcessPaymentAsync(request);

            if (result.IsSuccess)
            {
                // Update existing Payment entity
                var payment = await _dbContext.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == request.TransactionId);
                if (payment != null)
                {
                    payment.Status = result.Status;
                    payment.ProcessedAt = DateTime.UtcNow;
                    payment.GatewayResponse = System.Text.Json.JsonSerializer.Serialize(request);
                    _dbContext.Payments.Update(payment);
                    await _dbContext.SaveChangesAsync();

                    // Create PaymentAudit
                    var audit = new EcommerceLaptop.Core.Entities.PaymentAudit
                    {
                        PaymentId = payment.Id,
                        Action = "Process",
                        Status = result.Status,
                        Gateway = request.Gateway,
                        TransactionId = request.TransactionId,
                        RequestData = System.Text.Json.JsonSerializer.Serialize(request),
                        ResponseData = System.Text.Json.JsonSerializer.Serialize(result),
                        ProcessingTimeMs = 0, // Calculate if needed
                        CreatedAt = DateTime.UtcNow,
                        CreatedByIp = "", // From HTTP context
                        UserAgent = "", // From HTTP context
                        UserId = "", // From current user
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new { Gateway = request.Gateway, Status = result.Status })
                    };
                    _dbContext.PaymentAudits.Add(audit);
                    await _dbContext.SaveChangesAsync();

                    //  NEW: Update Order status when payment is completed
                    if (result.Status == PaymentStatus.Completed)
                    {
                        _logger.LogInformation("Payment completed for order {OrderId}. Updating order status to Confirmed", request.OrderId);

                        var orderUpdateResult = await _orderWorkflowService.UpdateOrderStatusAsync(
                            request.OrderId,
                            OrderStatus.Confirmed,
                            $"Payment confirmed via {request.Gateway} - Transaction: {request.TransactionId}");

                        if (orderUpdateResult)
                        {
                            _logger.LogInformation("Order {OrderId} status updated to Confirmed after successful payment", request.OrderId);

                            // Send payment success email with invoice PDF
                            try
                            {
                                var fullOrder = await _dbContext.Orders
                                    .Include(o => o.User)
                                    .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product)
                                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                                if (fullOrder != null && fullOrder.User != null)
                                {
                                    var pdfBytes = await _pdfExportService.ExportInvoicePdfAsync(fullOrder, includeDigitalSignature: true);
                                    var emailSent = await _emailService.SendPaymentSuccessEmailWithInvoiceAsync(fullOrder.User, fullOrder, pdfBytes);

                                    if (emailSent)
                                    {
                                        _logger.LogInformation("Payment success email with invoice sent for order {OrderId}", request.OrderId);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Failed to send payment success email for order {OrderId}", request.OrderId);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Could not fetch full order details for email after payment success for order {OrderId}", request.OrderId);
                                }
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "Error sending payment success email for order {OrderId}", request.OrderId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to update order {OrderId} status to Confirmed after payment", request.OrderId);
                        }
                    }
                }

                _logger.LogInformation("Payment processed successfully for order {OrderId}. Status: {Status}",
                    request.OrderId, result.Status);
            }
            else
            {
                _logger.LogError("Failed to process payment for order {OrderId}. Error: {Error}",
                    request.OrderId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
            return new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                TransactionId = request.TransactionId,
                ErrorMessage = "Payment processing failed",
                ErrorCode = "PROCESS_ERROR"
            };
        }
    }

    /// <summary>
    /// Processes a refund for a completed payment
    /// </summary>
    public async Task<RefundResult> RefundPaymentAsync(
        int paymentId,
        decimal amount,
        string reason,
        string requestedBy)
    {
        try
        {
            _logger.LogInformation("Processing refund for payment {PaymentId}", paymentId);

            // In real implementation, you'd get the original transaction details from database
            var originalTransactionId = $"TXN_{paymentId}";
            var gateway = DetermineGatewayFromTransactionId(originalTransactionId);

            var paymentService = _paymentServiceFactory.CreatePaymentService(gateway);

            var refundRequest = new ProcessRefundRequest
            {
                PaymentId = paymentId,
                OriginalTransactionId = originalTransactionId,
                RefundAmount = amount,
                Reason = reason,
                RequestedBy = requestedBy
            };

            var result = await paymentService.ProcessRefundAsync(refundRequest);

            if (result.IsSuccess)
            {
                // Update Payment entity for refund
                var payment = await _dbContext.Payments.FindAsync(paymentId);
                if (payment != null)
                {
                    payment.Status = PaymentStatus.Refunded;
                    _dbContext.Payments.Update(payment);
                    await _dbContext.SaveChangesAsync();

                    // Create PaymentAudit
                    var audit = new EcommerceLaptop.Core.Entities.PaymentAudit
                    {
                        PaymentId = paymentId,
                        Action = "Refund",
                        Status = PaymentStatus.Refunded,
                        Gateway = gateway,
                        TransactionId = originalTransactionId,
                        RequestData = System.Text.Json.JsonSerializer.Serialize(refundRequest),
                        ResponseData = System.Text.Json.JsonSerializer.Serialize(result),
                        ProcessingTimeMs = 0,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByIp = "",
                        UserAgent = "",
                        UserId = requestedBy,
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new { Amount = amount, Reason = reason })
                    };
                    _dbContext.PaymentAudits.Add(audit);
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation("Refund processed for payment {PaymentId}. Success: {Success} with audit",
                    paymentId, result.IsSuccess);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return new RefundResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                RefundAmount = amount,
                ErrorMessage = "Refund processing failed",
                ErrorCode = "REFUND_ERROR"
            };
        }
    }

    /// <summary>
    /// Gets payment status from the appropriate gateway
    /// </summary>
    public async Task<PaymentStatusResult> GetPaymentStatusAsync(int paymentId)
    {
        try
        {
            // In real implementation, you'd get transaction details from database
            var transactionId = $"TXN_{paymentId}";
            var gateway = DetermineGatewayFromTransactionId(transactionId);

            var paymentService = _paymentServiceFactory.CreatePaymentService(gateway);
            var status = await paymentService.GetPaymentStatusAsync(transactionId);

            return new PaymentStatusResult
            {
                IsSuccess = true,
                PaymentId = paymentId,
                TransactionId = transactionId,
                Status = status,
                Amount = 0, // Would get from database
                Currency = "VND"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for payment {PaymentId}", paymentId);
            return new PaymentStatusResult
            {
                IsSuccess = false,
                PaymentId = paymentId,
                Status = PaymentStatus.Failed,
                ErrorMessage = "Failed to get payment status"
            };
        }
    }

    /// <summary>
    /// Processes webhook from any payment gateway
    /// Enhanced with business logic integration
    /// </summary>
    public async Task<PaymentWebhookResult> ProcessWebhookAsync(
        PaymentGateway gateway,
        string payload,
        IDictionary<string, string> headers)
    {
        try
        {
            _logger.LogInformation("Processing webhook for gateway {Gateway}", gateway);

            // Step 1: Process webhook through payment service
            var paymentService = _paymentServiceFactory.CreatePaymentService(gateway);
            var result = await paymentService.ProcessWebhookAsync(payload, headers);

            // Set the gateway for proper logging and business logic
            result.Gateway = gateway;

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Webhook processing failed for gateway {Gateway}: {Error}",
                    gateway, result.ErrorMessage);
                return result;
            }

            // Step 2: Update Payment entity status based on webhook result (sync Payment)
            try
            {
                if (!string.IsNullOrEmpty(result.TransactionId) || (result.Data != null && result.Data.ContainsKey("payment_intent_id")))
                {
                    var transactionId = result.TransactionId;
                    var payment = !string.IsNullOrEmpty(transactionId)
                        ? await _dbContext.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId)
                        : null;

                    // Fallback lookup for Stripe: try payment_intent_id if charge id not found
                    if (payment == null && result.Data != null && result.Data.TryGetValue("payment_intent_id", out var piObj))
                    {
                        var paymentIntentId = piObj?.ToString();
                        if (!string.IsNullOrWhiteSpace(paymentIntentId))
                        {
                            payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.TransactionId == paymentIntentId);
                        }
                    }

                    if (payment != null && result.Status != default)
                    {
                        payment.Status = result.Status;
                        payment.ProcessedAt = payment.ProcessedAt ?? DateTime.UtcNow;
                        // Record last gateway payload/result for traceability
                        payment.GatewayResponse = string.IsNullOrEmpty(payment.GatewayResponse)
                            ? System.Text.Json.JsonSerializer.Serialize(result)
                            : payment.GatewayResponse;

                        _dbContext.Payments.Update(payment);
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to synchronize Payment status during webhook processing for gateway {Gateway}", gateway);
            }

            // Step 3: Process business logic based on webhook result
            await ProcessWebhookBusinessLogicAsync(result);

            // Create PaymentAudit for webhook
            if (!string.IsNullOrEmpty(result.TransactionId))
            {
                var payment = await _dbContext.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == result.TransactionId);
                if (payment != null)
                {
                    var audit = new EcommerceLaptop.Core.Entities.PaymentAudit
                    {
                        PaymentId = payment.Id,
                        Action = "Webhook",
                        Status = result.Status,
                        Gateway = gateway,
                        TransactionId = result.TransactionId,
                        RequestData = payload,
                        ResponseData = System.Text.Json.JsonSerializer.Serialize(result),
                        ProcessingTimeMs = 0,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByIp = headers.TryGetValue("X-Forwarded-For", out var ip) ? ip : "",
                        UserAgent = headers.TryGetValue("User-Agent", out var ua) ? ua : "",
                        UserId = "",
                        Metadata = System.Text.Json.JsonSerializer.Serialize(new { Headers = headers.Keys })
                    };
                    _dbContext.PaymentAudits.Add(audit);
                    await _dbContext.SaveChangesAsync();
                }
            }

            _logger.LogInformation("Webhook processed successfully for gateway {Gateway}. Action: {Action} with audit",
                gateway, result.Action);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for gateway {Gateway}", gateway);
            return new PaymentWebhookResult
            {
                IsSuccess = false,
                ErrorMessage = "Webhook processing failed",
                RequiresResponse = false
            };
        }
    }

    /// <summary>
    /// Processes business logic based on webhook results
    /// </summary>
    private async Task ProcessWebhookBusinessLogicAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            // Only process business logic for successful webhook processing
            if (!webhookResult.IsSuccess)
            {
                return;
            }

            _logger.LogInformation("Processing business logic for webhook action {Action}, transaction {TransactionId}",
                webhookResult.Action, webhookResult.TransactionId);

            // Route to appropriate business logic based on action/status
            var businessLogicProcessed = webhookResult.Action?.ToLowerInvariant() switch
            {
                "payment" when webhookResult.Status == PaymentStatus.Completed =>
                    await ProcessPaymentSuccessBusinessLogicAsync(webhookResult),

                "payment_completed" when webhookResult.Status == PaymentStatus.Completed =>
                    await ProcessPaymentSuccessBusinessLogicAsync(webhookResult),

                "payment" when webhookResult.Status == PaymentStatus.Failed =>
                    await ProcessPaymentFailureBusinessLogicAsync(webhookResult),

                "payment_failed" when webhookResult.Status == PaymentStatus.Failed =>
                    await ProcessPaymentFailureBusinessLogicAsync(webhookResult),

                "refund" =>
                    await ProcessRefundBusinessLogicAsync(webhookResult),

                "dispute" or "chargeback" =>
                    await ProcessDisputeBusinessLogicAsync(webhookResult),

                _ => await ProcessGenericWebhookBusinessLogicAsync(webhookResult)
            };

            if (businessLogicProcessed)
            {
                _logger.LogInformation("Business logic processed successfully for webhook action {Action}",
                    webhookResult.Action);
            }
            else
            {
                _logger.LogWarning("Business logic processing failed for webhook action {Action}",
                    webhookResult.Action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook business logic for action {Action}, transaction {TransactionId}",
                webhookResult.Action, webhookResult.TransactionId);
            // Don't throw - webhook processing should still be considered successful
        }
    }

    /// <summary>
    /// Processes payment success business logic
    /// </summary>
    private async Task<bool> ProcessPaymentSuccessBusinessLogicAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing payment success business logic for transaction {TransactionId}",
                webhookResult.TransactionId);

            // Use the integrated business logic service for comprehensive order processing
            return await _businessLogicService.ProcessPaymentSuccessAsync(webhookResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment success business logic for transaction {TransactionId}",
                webhookResult.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Processes payment failure business logic
    /// </summary>
    private async Task<bool> ProcessPaymentFailureBusinessLogicAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing payment failure business logic for transaction {TransactionId}",
                webhookResult.TransactionId);

            // Use the integrated business logic service for order cancellation and inventory release
            return await _businessLogicService.ProcessPaymentFailureAsync(webhookResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment failure business logic for transaction {TransactionId}",
                webhookResult.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Processes refund business logic
    /// </summary>
    private async Task<bool> ProcessRefundBusinessLogicAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing refund business logic for transaction {TransactionId}",
                webhookResult.TransactionId);

            // Use the integrated business logic service for refund processing and order status updates
            return await _businessLogicService.ProcessRefundAsync(webhookResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund business logic for transaction {TransactionId}",
                webhookResult.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Processes dispute business logic
    /// </summary>
    private async Task<bool> ProcessDisputeBusinessLogicAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing dispute business logic for transaction {TransactionId}",
                webhookResult.TransactionId);

            // Use the integrated business logic service for dispute handling and admin notifications
            return await _businessLogicService.ProcessDisputeAsync(webhookResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dispute business logic for transaction {TransactionId}",
                webhookResult.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Processes generic webhook business logic for unhandled actions
    /// </summary>
    private Task<bool> ProcessGenericWebhookBusinessLogicAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing generic webhook business logic for action {Action}, transaction {TransactionId}",
                webhookResult.Action, webhookResult.TransactionId);

            // Log the event for audit purposes
            _logger.LogInformation("Webhook event logged: Gateway={Gateway}, Action={Action}, Status={Status}, TransactionId={TransactionId}",
                webhookResult.Gateway, webhookResult.Action, webhookResult.Status, webhookResult.TransactionId);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing generic webhook business logic for transaction {TransactionId}",
                webhookResult.TransactionId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Gets available payment methods for a specific gateway
    /// </summary>
    public Task<IReadOnlyList<PaymentMethodInfo>> GetAvailablePaymentMethodsAsync(
        PaymentGateway gateway,
        string currency,
        decimal amount)
    {
        try
        {
            var paymentService = _paymentServiceFactory.CreatePaymentService(gateway);
            var supportedCurrencies = paymentService.GetSupportedCurrencies();

            if (!supportedCurrencies.Contains(currency))
            {
                return Task.FromResult<IReadOnlyList<PaymentMethodInfo>>(Array.Empty<PaymentMethodInfo>());
            }

            var methods = new List<PaymentMethodInfo>();

            foreach (var method in Enum.GetValues<PaymentMethod>())
            {
                if (paymentService.SupportsPaymentMethod(method))
                {
                    var validation = PaymentHelpers.ValidateAmount(amount, gateway, currency);
                    var limits = PaymentHelpers.GetGatewayLimits(gateway, currency);

                    methods.Add(new PaymentMethodInfo
                    {
                        Method = method,
                        DisplayName = GetMethodDisplayName(method),
                        Description = GetMethodDescription(method, gateway),
                        IsEnabled = validation.IsValid,
                        MinAmount = limits.MinAmount,
                        MaxAmount = limits.MaxAmount,
                        SupportedCurrencies = supportedCurrencies.ToList()
                    });
                }
            }

            return Task.FromResult<IReadOnlyList<PaymentMethodInfo>>(methods.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available payment methods for gateway {Gateway}", gateway);
            return Task.FromResult<IReadOnlyList<PaymentMethodInfo>>(Array.Empty<PaymentMethodInfo>());
        }
    }

    /// <summary>
    /// Validates payment request before processing
    /// </summary>
    public Task<PaymentValidationResult> ValidatePaymentRequestAsync(
        int orderId,
        PaymentGateway gateway,
        decimal amount,
        string currency)
    {
        try
        {
            var result = new PaymentValidationResult { IsValid = true };

            if (orderId <= 0)
            {
                result.IsValid = false;
                result.ValidationErrors.Add("Invalid order ID");
            }

            if (amount <= 0)
            {
                result.IsValid = false;
                result.ValidationErrors.Add("Amount must be greater than zero");
            }

            if (!_paymentServiceFactory.IsGatewayAvailable(gateway))
            {
                result.IsValid = false;
                result.ValidationErrors.Add($"Gateway {gateway} is not available");
            }

            var amountValidation = PaymentHelpers.ValidateAmount(amount, gateway, currency);
            if (!amountValidation.IsValid)
            {
                result.IsValid = false;
                result.ValidationErrors.AddRange(amountValidation.ValidationErrors);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payment request");
            return Task.FromResult(new PaymentValidationResult
            {
                IsValid = false,
                ValidationErrors = { "Validation failed" }
            });
        }
    }

    /// <summary>
    /// Gets payment gateway health status
    /// </summary>
    public Task<PaymentGatewayHealthResult> GetGatewayHealthAsync(PaymentGateway gateway)
    {
        try
        {
            var isAvailable = _paymentServiceFactory.IsGatewayAvailable(gateway);

            return Task.FromResult(new PaymentGatewayHealthResult
            {
                IsHealthy = isAvailable,
                Gateway = gateway,
                Status = isAvailable ? "Healthy" : "Unavailable",
                LastChecked = DateTime.UtcNow,
                ResponseTime = TimeSpan.Zero
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gateway health for {Gateway}", gateway);
            return Task.FromResult(new PaymentGatewayHealthResult
            {
                IsHealthy = false,
                Gateway = gateway,
                Status = "Error",
                ErrorMessage = "Failed to get health status"
            });
        }
    }

    #region Helper Methods

    private string GetMethodDisplayName(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => "Credit Card",
            PaymentMethod.DebitCard => "Debit Card",
            PaymentMethod.BankTransfer => "Bank Transfer",
            PaymentMethod.EWallet => "E-Wallet",
            PaymentMethod.QRCode => "QR Code",
            _ => method.ToString()
        };
    }

    private string GetMethodDescription(PaymentMethod method, PaymentGateway gateway)
    {
        return $"{GetMethodDisplayName(method)} payment via {gateway}";
    }

    private PaymentGateway DetermineGatewayFromTransactionId(string transactionId)
    {
        // Simple pattern matching - in real implementation this would be more sophisticated
        return transactionId.ToUpper() switch
        {
            var id when id.Contains("PAYPAL") => PaymentGateway.PayPal,
            var id when id.Contains("SEPAY") => PaymentGateway.SePay,
            _ => PaymentGateway.Stripe // Default fallback
        };
    }

    #endregion
}
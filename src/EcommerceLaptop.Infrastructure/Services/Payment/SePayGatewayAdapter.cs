using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Utilities.Payment;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// Adapter that wraps SePayService to implement IPaymentGatewayService
/// This allows SePay to be used as a payment gateway despite being designed as a monitoring service
/// </summary>
public class SePayGatewayAdapter : IPaymentGatewayService
{
    private readonly ISePayService _sePayService;
    private readonly ILogger<SePayGatewayAdapter> _logger;

    public PaymentGateway Gateway => PaymentGateway.SePay;

    public SePayGatewayAdapter(ISePayService sePayService, ILogger<SePayGatewayAdapter> logger)
    {
        _sePayService = sePayService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes a payment by generating a QR code for bank transfer
    /// </summary>
    public async Task<PaymentInitializationResult> InitializePaymentAsync(InitializePaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Initializing SePay payment for order {OrderId}", request.OrderId);

            // SePay always uses VND - ensure currency is correct
            var amount = request.Amount;
            var currency = request.Currency;

            if (currency != "VND")
            {
                _logger.LogWarning("SePay received non-VND currency {Currency}, converting to VND", currency);
                amount = PaymentHelpers.ConvertCurrency(amount, currency, "VND");
                currency = "VND";
            }

            // Generate QR code for bank transfer payment
            var qrRequest = new SePayQrCodeRequest
            {
                OrderId = request.OrderId,
                Amount = amount,
                Description = request.Description ?? $"Payment for order {request.OrderId}",
                BankAccount = "" // Use default bank account
            };

            var qrResult = await _sePayService.GenerateQrCodeAsync(qrRequest);

            if (qrResult.IsSuccess)
            {
                return new PaymentInitializationResult
                {
                    IsSuccess = true,
                    TransactionId = $"sepay_{request.OrderId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    // Don't set PaymentUrl to prevent redirect - keep QR code inline
                    QrCodeData = qrResult.QrCodeData,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["qrCodeUrl"] = qrResult.QrCodeUrl,
                        ["qrCodeData"] = qrResult.QrCodeData,
                        ["bankAccount"] = qrResult.BankAccount,
                        ["bankName"] = qrResult.BankName,
                        ["instructions"] = "Scan the QR code with your banking app to make the payment",
                        ["paymentType"] = "qr_inline", // Flag to indicate inline QR display
                        ["amount"] = request.Amount,
                        ["currency"] = "VND"
                    }
                };
            }
            else
            {
                return new PaymentInitializationResult
                {
                    IsSuccess = false,
                    ErrorMessage = qrResult.ErrorMessage ?? "Failed to generate payment QR code",
                    ErrorCode = "SEPAY_QR_FAILED"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SePay payment for order {OrderId}", request.OrderId);
            return new PaymentInitializationResult
            {
                IsSuccess = false,
                ErrorMessage = "Payment initialization failed",
                ErrorCode = "SEPAY_INIT_ERROR"
            };
        }
    }

    /// <summary>
    /// SePay doesn't process payments directly - payments are made via bank transfer
    /// This method returns pending status as payment verification happens via webhooks
    /// </summary>
    public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        _logger.LogInformation("SePay payment processing for transaction {TransactionId} - monitoring via webhooks",
            request.TransactionId);

        // SePay doesn't process payments directly
        // Payments are made by users via bank transfer, then monitored via webhooks
        return await Task.FromResult(new PaymentResult
        {
            IsSuccess = true,
            TransactionId = request.TransactionId,
            Status = PaymentStatus.Pending,
            ErrorMessage = "Payment is pending - waiting for bank transfer confirmation"
        });
    }

    /// <summary>
    /// SePay refunds would need to be handled manually via bank transfer
    /// </summary>
    public async Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request)
    {
        _logger.LogWarning("SePay refund requested for transaction {TransactionId} - manual processing required",
            request.OriginalTransactionId);

        // SePay refunds need to be handled manually via bank transfer
        return await Task.FromResult(new RefundResult
        {
            IsSuccess = false,
            ErrorMessage = "SePay refunds must be processed manually via bank transfer",
            ErrorCode = "SEPAY_MANUAL_REFUND_REQUIRED"
        });
    }

    /// <summary>
    /// Gets payment status - in real implementation this would check the monitoring data
    /// </summary>
    public async Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        _logger.LogInformation("Checking SePay payment status for transaction {TransactionId}", transactionId);

        // In real implementation, this would:
        // 1. Query the database for webhook notifications related to this transaction
        // 2. Check the monitoring data from SePay
        // For now, return pending as the actual status check would require webhook data
        return await Task.FromResult(PaymentStatus.Pending);
    }

    /// <summary>
    /// Validates SePay webhook signature
    /// </summary>
    public async Task<bool> ValidateWebhookAsync(string payload, string signature, IDictionary<string, string> headers)
    {
        try
        {
            // Use the SePay service's webhook validation
            var authHeader = headers.ContainsKey("Authorization") ? headers["Authorization"] : "";
            return await _sePayService.ValidateWebhookAuthAsync(authHeader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SePay webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Processes SePay webhook notifications
    /// </summary>
    public async Task<PaymentWebhookResult> ProcessWebhookAsync(string payload, IDictionary<string, string> headers)
    {
        try
        {
            _logger.LogInformation("Processing SePay webhook");

            // Parse the webhook payload into SePay format
            var sePayPayload = System.Text.Json.JsonSerializer.Deserialize<SePayWebhookPayload>(payload);
            if (sePayPayload == null)
            {
                return new PaymentWebhookResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid SePay webhook payload",
                    Gateway = PaymentGateway.SePay
                };
            }

            var result = await _sePayService.ProcessWebhookAsync(sePayPayload, headers);

            return new PaymentWebhookResult
            {
                IsSuccess = result.Success,
                TransactionId = result.PaymentCode ?? $"sepay_{result.OrderId}",
                Status = result.Success ? PaymentStatus.Completed : PaymentStatus.Failed,
                Action = "payment",
                Gateway = PaymentGateway.SePay,
                ErrorMessage = result.Success ? "" : result.Message,
                Data = new Dictionary<string, object>
                {
                    ["order_id"] = result.OrderId ?? 0,
                    ["paymentCode"] = result.PaymentCode ?? "",
                    ["processedAt"] = result.ProcessedAt,
                    ["message"] = result.Message
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SePay webhook");
            return new PaymentWebhookResult
            {
                IsSuccess = false,
                ErrorMessage = "Webhook processing failed",
                Gateway = PaymentGateway.SePay
            };
        }
    }

    /// <summary>
    /// Checks if SePay supports the specified payment method
    /// </summary>
    public bool SupportsPaymentMethod(PaymentMethod method)
    {
        // SePay supports QR code and bank transfer payments
        return method == PaymentMethod.QRCode || method == PaymentMethod.BankTransfer;
    }

    /// <summary>
    /// Gets supported currencies for SePay (Vietnamese Dong only)
    /// </summary>
    public IReadOnlyList<string> GetSupportedCurrencies()
    {
        return new List<string> { "VND" }.AsReadOnly();
    }

    /// <summary>
    /// Checks if SePay service is available
    /// </summary>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            return await _sePayService.CheckHealthAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SePay health check failed");
            return false;
        }
    }
}
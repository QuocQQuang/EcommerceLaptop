using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.DTOs.Payment;

#region Request DTOs

/// <summary>
/// Request to initialize a payment transaction
/// </summary>
public class InitializePaymentRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public PaymentGateway Gateway { get; set; }

    [Required]
    public PaymentMethod Method { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "VND";

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string CustomerEmail { get; set; } = string.Empty;

    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(20)]
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>
    /// Gateway-specific parameters
    /// </summary>
    public Dictionary<string, object> GatewaySpecificData { get; set; } = new();

    /// <summary>
    /// Return URL after payment completion
    /// </summary>
    [StringLength(500)]
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cancel URL if payment is cancelled
    /// </summary>
    [StringLength(500)]
    public string CancelUrl { get; set; } = string.Empty;
}

/// <summary>
/// Enhanced process payment request with gateway-specific features
/// </summary>
public class ProcessPaymentRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public PaymentGateway Gateway { get; set; }

    [Required]
    [StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Raw payment response from gateway
    /// </summary>
    public string GatewayResponse { get; set; } = string.Empty;

    /// <summary>
    /// Signature for validation
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gateway-specific processing data
    /// </summary>
    public Dictionary<string, object> ProcessingData { get; set; } = new();

    /// <summary>
    /// Gateway-specific data for payment processing
    /// </summary>
    public Dictionary<string, object> GatewaySpecificData { get; set; } = new();
}

/// <summary>
/// Request to process a refund transaction
/// </summary>
public class ProcessRefundRequest
{
    [Required]
    public int PaymentId { get; set; }

    [Required]
    [StringLength(100)]
    public string OriginalTransactionId { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than 0")]
    public decimal RefundAmount { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(100)]
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gateway-specific refund parameters
    /// </summary>
    public Dictionary<string, object> GatewaySpecificData { get; set; } = new();
}





/// <summary>
/// Request for PayPal express checkout
/// </summary>
public class ExpressCheckoutRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string ReturnUrl { get; set; } = string.Empty;

    [StringLength(500)]
    public string CancelUrl { get; set; } = string.Empty;

    public bool RequireShipping { get; set; } = false;
}

/// <summary>
/// Request for capturing PayPal payment after approval
/// </summary>
public class CapturePayPalPaymentRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    [StringLength(100)]
    public string PayPalOrderId { get; set; } = string.Empty;

    [StringLength(100)]
    public string? PayerID { get; set; }

    [StringLength(500)]
    public string? PaymentSource { get; set; }
}

#endregion

#region Result DTOs

/// <summary>
/// Result of payment initialization
/// </summary>
public class PaymentInitializationResult
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Result of payment processing
/// </summary>
public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string GatewayResponse { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of refund processing
/// </summary>
public class RefundResult
{
    public bool IsSuccess { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public string OriginalTransactionId { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string GatewayResponse { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}

/// <summary>
/// Result of webhook processing
/// </summary>
public class PaymentWebhookResult
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string Action { get; set; } = string.Empty; // payment, refund, chargeback
    public PaymentGateway Gateway { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public bool RequiresResponse { get; set; } = true;
    public string ResponseContent { get; set; } = string.Empty;
}

/// <summary>
/// QR code generation result
/// </summary>
public class QrCodeResult
{
    public bool IsSuccess { get; set; }
    public string QrCodeData { get; set; } = string.Empty;
    public string QrCodeImageUrl { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}



/// <summary>
/// PayPal express checkout result
/// </summary>
public class ExpressCheckoutResult
{
    public bool IsSuccess { get; set; }
    public string Token { get; set; } = string.Empty;
    public string ApprovalUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Currency conversion result
/// </summary>
public class CurrencyConversionResult
{
    public bool IsSuccess { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime RateTimestamp { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}



#endregion

#region Orchestrator DTOs

/// <summary>
/// Request to complete a payment transaction
/// </summary>
public class CompletePaymentRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public PaymentGateway Gateway { get; set; }

    [Required]
    [StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gateway response data
    /// </summary>
    public Dictionary<string, string> ResponseData { get; set; } = new();

    /// <summary>
    /// Signature for validation
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Payment status query result
/// </summary>
public class PaymentStatusResult
{
    public bool IsSuccess { get; set; }
    public int PaymentId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> GatewayData { get; set; } = new();
}

/// <summary>
/// Payment method information
/// </summary>
public class PaymentMethodInfo
{
    public PaymentMethod Method { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public List<string> SupportedCurrencies { get; set; } = new();
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// Payment validation result
/// </summary>
public class PaymentValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> ValidationData { get; set; } = new();
}

/// <summary>
/// Payment gateway health status
/// </summary>
public class PaymentGatewayHealthResult
{
    public bool IsHealthy { get; set; }
    public PaymentGateway Gateway { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> HealthData { get; set; } = new();
}

/// <summary>
/// Mobile deep link result for payment apps
/// </summary>
public class MobileDeepLinkResult
{
    public bool IsSuccess { get; set; }
    public string DeepLink { get; set; } = string.Empty;
    public string AppStoreUrl { get; set; } = string.Empty;
    public string PlayStoreUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Transaction limits for payment gateway
/// </summary>
public class TransactionLimitsResult
{
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public Dictionary<string, object> AdditionalLimits { get; set; } = new();
}

/// <summary>
/// Gateway health status for orchestrator
/// </summary>
public class GatewayHealthStatus
{
    public bool IsHealthy { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public DateTime LastChecked { get; set; }
}

#endregion

#region SePay Specific DTOs

/// <summary>
/// SePay QR Code generation request
/// </summary>
public class SePayQrCodeRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    [Range(1000, 1000000000, ErrorMessage = "Amount must be between 1,000 and 1,000,000,000 VND")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(100)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string BankAccount { get; set; } = string.Empty; // If empty, use default account

    [StringLength(3)]
    public string Currency { get; set; } = "VND";
}

/// <summary>
/// SePay QR Code generation response
/// </summary>
public class SePayQrCodeResponse
{
    public bool IsSuccess { get; set; }
    public string QrCodeUrl { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty; // Raw VietQR data
    public string BankAccount { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// SePay webhook payload according to official documentation
/// https://docs.sepay.vn/tich-hop-webhooks.html
/// </summary>
public class SePayWebhookPayload
{
    /// <summary>
    /// ID giao dịch trên SePay
    /// </summary>
    [Required]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Brand name của ngân hàng
    /// </summary>
    [Required]
    [StringLength(50)]
    [JsonPropertyName("gateway")]
    public string Gateway { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian xảy ra giao dịch phía ngân hàng
    /// Format: "2023-03-25 14:02:37"
    /// </summary>
    [Required]
    [JsonPropertyName("transactionDate")]
    public string TransactionDate { get; set; } = string.Empty;

    /// <summary>
    /// Số tài khoản ngân hàng
    /// </summary>
    [Required]
    [StringLength(50)]
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Mã code thanh toán (sepay tự nhận diện dựa vào cấu hình)
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Nội dung chuyển khoản
    /// </summary>
    [Required]
    [StringLength(500)]
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Loại giao dịch. "in" là tiền vào, "out" là tiền ra
    /// </summary>
    [Required]
    [StringLength(10)]
    [JsonPropertyName("transferType")]
    public string TransferType { get; set; } = string.Empty;

    /// <summary>
    /// Số tiền giao dịch
    /// </summary>
    [Required]
    [JsonPropertyName("transferAmount")]
    public decimal TransferAmount { get; set; }

    /// <summary>
    /// Số dư tài khoản (lũy kế)
    /// </summary>
    [JsonPropertyName("accumulated")]
    public decimal Accumulated { get; set; }

    /// <summary>
    /// Tài khoản ngân hàng phụ (tài khoản nhận danh)
    /// </summary>
    [JsonPropertyName("subAccount")]
    public string? SubAccount { get; set; }

    /// <summary>
    /// Mã tham chiếu của tin nhắn SMS
    /// </summary>
    [Required]
    [StringLength(100)]
    [JsonPropertyName("referenceCode")]
    public string ReferenceCode { get; set; } = string.Empty;

    /// <summary>
    /// Toàn bộ nội dung tin nhắn SMS
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// SePay webhook processing result
/// </summary>
public class SePayWebhookResult
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Transaction processed successfully";
    public int? OrderId { get; set; }
    public string? PaymentCode { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string ResponseContent { get; set; } = string.Empty;
}

/// <summary>
/// SePay transaction monitoring request
/// </summary>
public class SePayMonitoringRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? BankAccount { get; set; }
    public string? TransferType { get; set; } = "in"; // Default to incoming only
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// SePay transaction monitoring response
/// </summary>
public class SePayMonitoringResponse
{
    public bool IsSuccess { get; set; }
    public List<SePayTransactionInfo> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// SePay transaction information
/// </summary>
public class SePayTransactionInfo
{
    public int Id { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransferType { get; set; } = string.Empty;
    public string ReferenceCode { get; set; } = string.Empty;
    public bool IsProcessed { get; set; } = false;
    public int? OrderId { get; set; }
}

/// <summary>
/// SePay bank account registration request
/// </summary>
public class SePayBankAccountRequest
{
    [Required]
    [StringLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string BankCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AccountName { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;

    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
}

#endregion

#region Stripe-specific DTOs

/// <summary>
/// Request to create Stripe Payment Intent
/// </summary>
public class CreatePaymentIntentRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "usd";

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string CustomerEmail { get; set; } = string.Empty;

    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(50)]
    public string PaymentMethodId { get; set; } = string.Empty;

    public List<string> PaymentMethodTypes { get; set; } = new() { "card" };

    public bool ConfirmationMethod { get; set; } = true; // automatic confirmation

    [StringLength(500)]
    public string ReturnUrl { get; set; } = string.Empty;

    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Result from Stripe Payment Intent creation
/// </summary>
public class StripePaymentIntentResult
{
    public bool IsSuccess { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Result from payment confirmation
/// </summary>
public class PaymentConfirmationResult
{
    public bool IsSuccess { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public DateTime? ConfirmedAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Result from Stripe Setup Intent creation
/// </summary>
public class StripeSetupIntentResult
{
    public bool IsSuccess { get; set; }
    public string SetupIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Result from payment method operations
/// </summary>
public class PaymentMethodResult
{
    public bool IsSuccess { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public bool IsAttached { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> PaymentMethodDetails { get; set; } = new();
}

#endregion
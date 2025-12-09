using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Payment;

namespace EcommerceLaptop.Core.Services.Payment;

/// <summary>
/// SePay transaction monitoring service interface
/// SePay is a transaction monitoring service, not a traditional payment gateway
/// It monitors bank account transactions and notifies via webhooks
/// </summary>
public interface ISePayService
{
    /// <summary>
    /// Gateway identifier for SePay
    /// </summary>
    PaymentGateway Gateway { get; }

    /// <summary>
    /// Generates QR code for bank transfer payment
    /// Uses SePay's QR generation service: https://qr.sepay.vn/img
    /// </summary>
    /// <param name="request">QR code generation request</param>
    /// <returns>QR code URL and VietQR data</returns>
    Task<SePayQrCodeResponse> GenerateQrCodeAsync(SePayQrCodeRequest request);

    /// <summary>
    /// Processes webhook notification from SePay
    /// Validates incoming transaction data and maps to orders
    /// </summary>
    /// <param name="payload">SePay webhook payload</param>
    /// <param name="headers">HTTP headers from webhook request</param>
    /// <returns>Webhook processing result</returns>
    Task<SePayWebhookResult> ProcessWebhookAsync(SePayWebhookPayload payload, IDictionary<string, string> headers);

    /// <summary>
    /// Validates webhook authentication
    /// SePay uses Bearer token authentication
    /// </summary>
    /// <param name="authHeader">Authorization header value</param>
    /// <returns>True if authentication is valid</returns>
    Task<bool> ValidateWebhookAuthAsync(string authHeader);

    /// <summary>
    /// Monitors bank account transactions
    /// Retrieves transaction history from SePay API
    /// </summary>
    /// <param name="request">Transaction monitoring request</param>
    /// <returns>List of transactions from monitored accounts</returns>
    Task<SePayMonitoringResponse> GetTransactionsAsync(SePayMonitoringRequest request);

    /// <summary>
    /// Registers a new bank account for monitoring
    /// Adds account to SePay monitoring system
    /// </summary>
    /// <param name="request">Bank account registration request</param>
    /// <returns>Registration result</returns>
    Task<bool> RegisterBankAccountAsync(SePayBankAccountRequest request);

    /// <summary>
    /// Gets list of supported banks for SePay integration
    /// Based on official documentation: VPBank, BIDV, TPBank, ACB, VietinBank, MB, OCB, KienLongBank, MSB
    /// </summary>
    /// <returns>List of supported bank codes and names</returns>
    IReadOnlyList<(string BankCode, string BankName)> GetSupportedBanks();

    /// <summary>
    /// Checks service health and API connectivity
    /// </summary>
    /// <returns>Health check result</returns>
    Task<bool> CheckHealthAsync();

    /// <summary>
    /// Extracts order ID from transaction description
    /// Parses payment content to find order references
    /// </summary>
    /// <param name="content">Transaction description/content</param>
    /// <returns>Extracted order ID if found</returns>
    int? ExtractOrderIdFromContent(string content);

    /// <summary>
    /// Formats order description for QR code generation
    /// Creates standardized description format for transaction tracking
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="customerName">Customer name (optional)</param>
    /// <returns>Formatted description for bank transfer</returns>
    string FormatOrderDescription(int orderId, string? customerName = null);
}
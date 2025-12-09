using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Payment;

namespace EcommerceLaptop.Core.Services.Payment;

/// <summary>
/// Payment orchestrator service interface following Uncle Bob's Clean Code principles
/// Coordinates payment operations across multiple gateways using the Facade pattern
/// </summary>
public interface IPaymentOrchestrator
{
    /// <summary>
    /// Initializes a payment for an order using the appropriate gateway
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="gateway">Preferred payment gateway</param>
    /// <param name="method">Payment method</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="currency">Payment currency</param>
    /// <param name="returnUrl">Return URL after payment</param>
    /// <param name="cancelUrl">Cancel URL if payment cancelled</param>
    /// <returns>Payment initialization result</returns>
    Task<PaymentInitializationResult> InitializePaymentAsync(
        int orderId,
        PaymentGateway gateway,
        PaymentMethod method,
        decimal amount,
        string currency,
        string returnUrl = "",
        string cancelUrl = "");

    /// <summary>
    /// Completes a payment transaction
    /// </summary>
    /// <param name="request">Payment completion request</param>
    /// <returns>Payment completion result</returns>
    Task<PaymentResult> CompletePaymentAsync(CompletePaymentRequest request);

    /// <summary>
    /// Processes a payment request using the appropriate gateway
    /// Used for capturing payments that have been previously authorized
    /// </summary>
    /// <param name="request">Payment processing request</param>
    /// <returns>Payment processing result</returns>
    Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);

    /// <summary>
    /// Processes a refund for a completed payment
    /// </summary>
    /// <param name="paymentId">Original payment identifier</param>
    /// <param name="amount">Refund amount</param>
    /// <param name="reason">Refund reason</param>
    /// <param name="requestedBy">User requesting refund</param>
    /// <returns>Refund processing result</returns>
    Task<RefundResult> RefundPaymentAsync(
        int paymentId,
        decimal amount,
        string reason,
        string requestedBy);

    /// <summary>
    /// Gets payment status from the appropriate gateway
    /// </summary>
    /// <param name="paymentId">Payment identifier</param>
    /// <returns>Current payment status</returns>
    Task<PaymentStatusResult> GetPaymentStatusAsync(int paymentId);

    /// <summary>
    /// Processes webhook from any payment gateway
    /// </summary>
    /// <param name="gateway">Gateway that sent the webhook</param>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="headers">HTTP headers</param>
    /// <returns>Webhook processing result</returns>
    Task<PaymentWebhookResult> ProcessWebhookAsync(
        PaymentGateway gateway,
        string payload,
        IDictionary<string, string> headers);

    /// <summary>
    /// Gets available payment methods for a specific gateway
    /// </summary>
    /// <param name="gateway">Payment gateway</param>
    /// <param name="currency">Currency code</param>
    /// <param name="amount">Payment amount</param>
    /// <returns>List of available payment methods</returns>
    Task<IReadOnlyList<PaymentMethodInfo>> GetAvailablePaymentMethodsAsync(
        PaymentGateway gateway,
        string currency,
        decimal amount);

    /// <summary>
    /// Validates payment request before processing
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="gateway">Payment gateway</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="currency">Payment currency</param>
    /// <returns>Validation result</returns>
    Task<PaymentValidationResult> ValidatePaymentRequestAsync(
        int orderId,
        PaymentGateway gateway,
        decimal amount,
        string currency);

    /// <summary>
    /// Gets payment gateway health status
    /// </summary>
    /// <param name="gateway">Payment gateway to check</param>
    /// <returns>Gateway health status</returns>
    Task<PaymentGatewayHealthResult> GetGatewayHealthAsync(PaymentGateway gateway);
}

/// <summary>
/// Payment service factory interface following the Factory pattern
/// </summary>
public interface IPaymentServiceFactory
{
    /// <summary>
    /// Creates appropriate payment service for the specified gateway
    /// </summary>
    /// <param name="gateway">Payment gateway</param>
    /// <returns>Payment service instance</returns>
    IPaymentGatewayService CreatePaymentService(PaymentGateway gateway);

    /// <summary>
    /// Gets all available payment gateways
    /// </summary>
    /// <returns>List of available gateways</returns>
    IReadOnlyList<PaymentGateway> GetAvailableGateways();

    /// <summary>
    /// Checks if a gateway is currently enabled and available
    /// </summary>
    /// <param name="gateway">Payment gateway</param>
    /// <returns>True if gateway is available</returns>
    bool IsGatewayAvailable(PaymentGateway gateway);

    /// <summary>
    /// Gets SePay transaction monitoring service
    /// Note: SePay is a transaction monitoring service, not a traditional payment gateway
    /// </summary>
    /// <returns>SePay service instance or null if not available</returns>
    ISePayService? GetSePayService();

    /// <summary>
    /// Checks if SePay transaction monitoring is available
    /// </summary>
    /// <returns>True if SePay service is available</returns>
    bool IsSePayAvailable();
}
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Payment;

namespace EcommerceLaptop.Core.Services.Payment;

/// <summary>
/// Common interface for all payment gateway services
/// Follows Interface Segregation Principle (SOLID) for payment operations
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Gateway identifier for this service
    /// </summary>
    PaymentGateway Gateway { get; }

    /// <summary>
    /// Initializes a payment transaction
    /// </summary>
    /// <param name="request">Payment initialization request</param>
    /// <returns>Payment initialization result with redirect URL or payment data</returns>
    Task<PaymentInitializationResult> InitializePaymentAsync(InitializePaymentRequest request);

    /// <summary>
    /// Processes a payment transaction
    /// </summary>
    /// <param name="request">Payment processing request</param>
    /// <returns>Payment processing result</returns>
    Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);

    /// <summary>
    /// Processes a refund transaction
    /// </summary>
    /// <param name="request">Refund processing request</param>
    /// <returns>Refund processing result</returns>
    Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request);

    /// <summary>
    /// Gets the current status of a payment transaction
    /// </summary>
    /// <param name="transactionId">Transaction identifier</param>
    /// <returns>Current payment status</returns>
    Task<PaymentStatus> GetPaymentStatusAsync(string transactionId);

    /// <summary>
    /// Validates webhook signature from the payment gateway
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="signature">Webhook signature</param>
    /// <param name="headers">HTTP headers from webhook request</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> ValidateWebhookAsync(string payload, string signature, IDictionary<string, string> headers);

    /// <summary>
    /// Processes webhook notification from the payment gateway
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="headers">HTTP headers from webhook request</param>
    /// <returns>Webhook processing result</returns>
    Task<PaymentWebhookResult> ProcessWebhookAsync(string payload, IDictionary<string, string> headers);

    /// <summary>
    /// Checks if the gateway supports the specified payment method
    /// </summary>
    /// <param name="method">Payment method to check</param>
    /// <returns>True if supported</returns>
    bool SupportsPaymentMethod(PaymentMethod method);

    /// <summary>
    /// Gets supported currencies for this gateway
    /// </summary>
    /// <returns>List of supported currency codes</returns>
    IReadOnlyList<string> GetSupportedCurrencies();
}

/// <summary>
/// PayPal-specific payment service interface
/// International payment and multi-currency support
/// </summary>
public interface IPayPalService : IPaymentGatewayService
{
    /// <summary>
    /// Creates PayPal express checkout session
    /// </summary>
    /// <param name="request">Express checkout request</param>
    /// <returns>Express checkout result with approval URL</returns>
    Task<ExpressCheckoutResult> CreateExpressCheckoutAsync(ExpressCheckoutRequest request);

    /// <summary>
    /// Processes currency conversion for international payments
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency</param>
    /// <param name="toCurrency">Target currency</param>
    /// <returns>Converted amount and exchange rate</returns>
    Task<CurrencyConversionResult> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency);
}

/// <summary>
/// Stripe-specific payment service interface
/// Modern payment processing with Payment Intents and advanced features
/// </summary>
public interface IStripeService : IPaymentGatewayService
{
    /// <summary>
    /// Creates Stripe Payment Intent for secure payment processing
    /// </summary>
    /// <param name="request">Payment Intent creation request</param>
    /// <returns>Payment Intent result with client secret</returns>
    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);

    /// <summary>
    /// Confirms Payment Intent after client-side authentication
    /// </summary>
    /// <param name="paymentIntentId">Payment Intent identifier</param>
    /// <returns>Payment confirmation result</returns>
    Task<PaymentConfirmationResult> ConfirmPaymentIntentAsync(string paymentIntentId);

    /// <summary>
    /// Creates setup intent for future payments (subscription/recurring)
    /// </summary>
    /// <param name="customerId">Stripe customer identifier</param>
    /// <param name="paymentMethodTypes">Supported payment method types</param>
    /// <returns>Setup intent result</returns>
    Task<StripeSetupIntentResult> CreateSetupIntentAsync(string customerId, List<string> paymentMethodTypes);

    /// <summary>
    /// Processes payment method attachment to customer
    /// </summary>
    /// <param name="customerId">Stripe customer identifier</param>
    /// <param name="paymentMethodId">Payment method identifier</param>
    /// <returns>Payment method attachment result</returns>
    Task<PaymentMethodResult> AttachPaymentMethodAsync(string customerId, string paymentMethodId);

    /// <summary>
    /// Gets supported payment methods for the merchant account
    /// </summary>
    /// <returns>List of supported payment method types</returns>
    Task<List<string>> GetSupportedPaymentMethodsAsync();
}
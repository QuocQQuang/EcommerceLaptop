namespace EcommerceLaptop.Core.Configuration;

/// <summary>
/// Payment gateway configuration settings following Uncle Bob's Clean Code principles
/// Implements Single Responsibility Principle for each gateway configuration
/// </summary>
public class PaymentGatewaySettings
{
    public PayPalSettings PayPal { get; set; } = new();
    public SePaySettings SePay { get; set; } = new();
    public StripeSettings Stripe { get; set; } = new();
}

/// <summary>
/// PayPal payment gateway configuration
/// International payment support with multi-currency
/// </summary>
public class PayPalSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox"; // sandbox or live
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookId { get; set; } = string.Empty;
    public List<string> SupportedCurrencies { get; set; } = new() { "USD", "EUR", "JPY", "VND" };
    public int TimeoutInMinutes { get; set; } = 15;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// SePay transaction monitoring configuration
/// Bank account monitoring and QR code generation for Vietnamese payments
/// </summary>
public class SePaySettings
{
    public string ApiToken { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string QrBaseUrl { get; set; } = "https://qr.sepay.vn/img";
    public string ApiBaseUrl { get; set; } = "https://my.sepay.vn/userapi";
    public List<SePayBankAccount> MonitoredAccounts { get; set; } = new();
    public int TimeoutInSeconds { get; set; } = 8; // SePay webhook timeout
    public bool IsEnabled { get; set; } = true;
    public int RateLimitPerSecond { get; set; } = 2; // SePay API rate limit
}

/// <summary>
/// SePay bank account configuration for monitoring
/// Represents a specific bank account that SePay monitors for transactions
/// </summary>
public class SePayBankAccount
{
    public string AccountNumber { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty; // From SePay banks.json
    public string BankName { get; set; } = string.Empty; // Display name
    public string AccountName { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Stripe payment gateway configuration
/// Modern payment processing with Payment Intents API
/// </summary>
public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2025-06-20"; // Latest stable API version
    public string Environment { get; set; } = "test"; // test or live
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public List<string> SupportedCurrencies { get; set; } = new() { "USD", "EUR", "GBP", "VND", "JPY", "CAD", "AUD" };
    public List<string> SupportedPaymentMethods { get; set; } = new() { "card", "klarna", "afterpay_clearpay" };
    public int TimeoutInMinutes { get; set; } = 15;
    public bool IsEnabled { get; set; } = true;
    public bool CaptureMethod { get; set; } = true; // true for immediate capture, false for manual capture
    public string ConfirmationMethod { get; set; } = "automatic"; // automatic or manual
}
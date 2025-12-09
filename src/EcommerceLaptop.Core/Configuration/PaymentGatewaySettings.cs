namespace EcommerceLaptop.Core.Configuration;

/// <summary>
/// Payment gateway configuration settings following Uncle Bob's Clean Code principles
/// Implements Single Responsibility Principle for each gateway configuration
/// </summary>
public class PaymentGatewaySettings
{
    public VnPaySettings VnPay { get; set; } = new();
    public MoMoSettings MoMo { get; set; } = new();
    public PayPalSettings PayPal { get; set; } = new();
    public ZaloPaySettings ZaloPay { get; set; } = new();
    public SePaySettings SePay { get; set; } = new();
    public StripeSettings Stripe { get; set; } = new();
}

/// <summary>
/// VnPay payment gateway configuration
/// Follows Anders Hejlsberg's C# design patterns with immutable properties
/// </summary>
public class VnPaySettings
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string Version { get; set; } = "2.1.0";
    public string Command { get; set; } = "pay";
    public string CurrCode { get; set; } = "VND";
    public string Locale { get; set; } = "vn";
    public int TimeoutInMinutes { get; set; } = 15;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// MoMo payment gateway configuration
/// Security-first approach with RSA key management
/// </summary>
public class MoMoSettings
{
    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty; // For signature validation
    public int TimeoutInMinutes { get; set; } = 15;
    public bool IsEnabled { get; set; } = true;
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
/// ZaloPay payment gateway configuration
/// Vietnamese mobile payment integration
/// </summary>
public class ZaloPaySettings
{
    public string AppId { get; set; } = string.Empty;
    public string Key1 { get; set; } = string.Empty; // For MAC generation
    public string Key2 { get; set; } = string.Empty; // For callback validation
    public string PaymentUrl { get; set; } = string.Empty;
    public string CreateOrderUrl { get; set; } = string.Empty;
    public string QueryUrl { get; set; } = string.Empty;
    public string RefundUrl { get; set; } = string.Empty;
    public string GetQrUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
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
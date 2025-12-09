using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Payment;
using System.Globalization;

namespace EcommerceLaptop.Core.Utilities.Payment;

/// <summary>
/// Payment helper utilities following Uncle Bob's Clean Code principles
/// Provides common operations for payment processing across all gateways
/// </summary>
public static class PaymentHelpers
{
    /// <summary>
    /// Generates unique transaction ID following best practices
    /// </summary>
    /// <param name="prefix">Gateway-specific prefix</param>
    /// <returns>Unique transaction identifier</returns>
    public static string GenerateTransactionId(string prefix = "TXN")
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1000, 9999);
        return $"{prefix}_{timestamp}_{random}";
    }

    /// <summary>
    /// Validates payment amount against gateway limits
    /// </summary>
    /// <param name="amount">Payment amount</param>
    /// <param name="gateway">Payment gateway</param>
    /// <param name="currency">Currency code</param>
    /// <returns>Validation result</returns>
    public static PaymentValidationResult ValidateAmount(decimal amount, PaymentGateway gateway, string currency)
    {
        var result = new PaymentValidationResult { IsValid = true };

        if (amount <= 0)
        {
            result.IsValid = false;
            result.ValidationErrors.Add("Payment amount must be greater than 0");
            return result;
        }

        // Gateway-specific limits (Vietnamese regulations)
        var limits = GetGatewayLimits(gateway, currency);

        if (amount < limits.MinAmount)
        {
            result.IsValid = false;
            result.ValidationErrors.Add($"Amount below minimum limit of {limits.MinAmount:N0} {currency}");
        }

        if (amount > limits.MaxAmount)
        {
            result.IsValid = false;
            result.ValidationErrors.Add($"Amount exceeds maximum limit of {limits.MaxAmount:N0} {currency}");
        }

        // Add warnings for high amounts
        if (amount > limits.MaxAmount * 0.8m)
        {
            result.Warnings.Add("Transaction amount is close to gateway limit");
        }

        // Special handling for SePay: Allow large amounts but add warning
        if (gateway == PaymentGateway.SePay && currency == "VND" && amount > 1_000_000_000m) // 1B VND
        {
            result.Warnings.Add("Large transaction amount detected. Please ensure sufficient funds and consider splitting into multiple payments if needed.");
        }

        return result;
    }

    /// <summary>
    /// Gets gateway-specific transaction limits
    /// </summary>
    /// <param name="gateway">Payment gateway</param>
    /// <param name="currency">Currency code</param>
    /// <returns>Transaction limits</returns>
    public static PaymentLimits GetGatewayLimits(PaymentGateway gateway, string currency)
    {
        return gateway switch
        {
            PaymentGateway.VnPay when currency == "VND" => new PaymentLimits
            {
                MinAmount = 10000m,           // 10,000 VND
                MaxAmount = 500_000_000m,     // 500M VND per transaction
                DailyLimit = 2_000_000_000m   // 2B VND per day
            },
            PaymentGateway.MoMo when currency == "VND" => new PaymentLimits
            {
                MinAmount = 10000m,           // 10,000 VND
                MaxAmount = 500_000_000m,      // 50M VND per transaction
                DailyLimit = 100_000_000m     // 100M VND per day
            },
            PaymentGateway.ZaloPay when currency == "VND" => new PaymentLimits
            {
                MinAmount = 1000m,            // 1,000 VND
                MaxAmount = 20_000_000m,      // 20M VND per transaction
                DailyLimit = 100_000_000m     // 100M VND per day
            },
            PaymentGateway.SePay when currency == "VND" => new PaymentLimits
            {
                MinAmount = 1m,               // 1 VND minimum for testing
                MaxAmount = 5_000_000_000m,    // 5B VND per transaction (increased for large orders)
                DailyLimit = 20_000_000_000m  // 20B VND per day (increased proportionally)
            },
            PaymentGateway.PayPal when currency == "USD" => new PaymentLimits
            {
                MinAmount = 1m,               // $1 USD
                MaxAmount = 10_000m,          // $10,000 USD per transaction
                DailyLimit = 60_000m          // $60,000 USD per day
            },
            PaymentGateway.PayPal when currency == "VND" => new PaymentLimits
            {
                MinAmount = 1m,               // Reduced from 23000 for testing
                MaxAmount = 230_000_000m,     // ~$10,000 USD equivalent
                DailyLimit = 1_380_000_000m   // ~$60,000 USD equivalent
            },
            PaymentGateway.Stripe when currency == "USD" => new PaymentLimits
            {
                MinAmount = 0.50m,            // $0.50 USD minimum
                MaxAmount = 999_999.99m,      // $999,999.99 USD per transaction
                DailyLimit = 10_000_000m      // $10M USD per day
            },
            PaymentGateway.Stripe when currency == "VND" => new PaymentLimits
            {
                MinAmount = 1m,               // Reduced from 11500 for testing
                MaxAmount = 23_000_000_000m,  // ~$999,999.99 USD equivalent
                DailyLimit = 230_000_000_000m // ~$10M USD equivalent
            },
            _ => new PaymentLimits
            {
                MinAmount = 1m,
                MaxAmount = 1_000_000m,
                DailyLimit = 10_000_000m
            }
        };
    }

    /// <summary>
    /// Converts currency amount using exchange rates
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency</param>
    /// <param name="toCurrency">Target currency</param>
    /// <returns>Converted amount</returns>
    public static decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        // Centralized exchange rates - synchronized with frontend
        var exchangeRates = new Dictionary<string, decimal>
        {
            ["USD_VND"] = 24000m,  // Updated to match frontend
            ["EUR_VND"] = 26000m,  // Updated rate
            ["JPY_VND"] = 160m,    // Updated rate
            ["VND_USD"] = 1m / 24000m,
            ["VND_EUR"] = 1m / 26000m,
            ["VND_JPY"] = 1m / 160m
        };

        var rateKey = $"{fromCurrency}_{toCurrency}";
        if (exchangeRates.TryGetValue(rateKey, out var rate))
        {
            return Math.Round(amount * rate, 2);
        }

        throw new NotSupportedException($"Currency conversion from {fromCurrency} to {toCurrency} is not supported");
    }

    /// <summary>
    /// Formats payment amount for display according to currency
    /// </summary>
    /// <param name="amount">Amount to format</param>
    /// <param name="currency">Currency code</param>
    /// <returns>Formatted amount string</returns>
    public static string FormatAmount(decimal amount, string currency)
    {
        return currency.ToUpperInvariant() switch
        {
            "VND" => amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ",
            "USD" => amount.ToString("C", CultureInfo.GetCultureInfo("en-US")),
            "EUR" => amount.ToString("C", CultureInfo.GetCultureInfo("de-DE")),
            "JPY" => amount.ToString("N0", CultureInfo.GetCultureInfo("ja-JP")) + " ",
            _ => $"{amount:N2} {currency}"
        };
    }

    /// <summary>
    /// Validates payment method compatibility with gateway
    /// </summary>
    /// <param name="method">Payment method</param>
    /// <param name="gateway">Payment gateway</param>
    /// <returns>True if compatible</returns>
    public static bool IsPaymentMethodSupported(PaymentMethod method, PaymentGateway gateway)
    {
        var supportedMethods = gateway switch
        {
            PaymentGateway.VnPay => new[]
            {
                PaymentMethod.CreditCard,
                PaymentMethod.DebitCard,
                PaymentMethod.BankTransfer,
                PaymentMethod.QRCode,
                PaymentMethod.Installment
            },
            PaymentGateway.MoMo => new[]
            {
                PaymentMethod.EWallet,
                PaymentMethod.QRCode,
                PaymentMethod.BankTransfer
            },
            PaymentGateway.PayPal => new[]
            {
                PaymentMethod.CreditCard,
                PaymentMethod.DebitCard,
                PaymentMethod.EWallet
            },
            PaymentGateway.ZaloPay => new[]
            {
                PaymentMethod.EWallet,
                PaymentMethod.QRCode,
                PaymentMethod.BankTransfer
            },
            PaymentGateway.Stripe => new[]
            {
                PaymentMethod.CreditCard,
                PaymentMethod.DebitCard,
                PaymentMethod.EWallet,
                PaymentMethod.BankTransfer
            },
            _ => Array.Empty<PaymentMethod>()
        };

        return supportedMethods.Contains(method);
    }

    /// <summary>
    /// Gets payment method display information
    /// </summary>
    /// <param name="method">Payment method</param>
    /// <param name="gateway">Payment gateway</param>
    /// <returns>Payment method info</returns>
    public static PaymentMethodInfo GetPaymentMethodInfo(PaymentMethod method, PaymentGateway gateway)
    {
        var isSupported = IsPaymentMethodSupported(method, gateway);
        var limits = GetGatewayLimits(gateway, "VND");

        return new PaymentMethodInfo
        {
            Method = method,
            DisplayName = GetMethodDisplayName(method),
            Description = GetMethodDescription(method, gateway),
            IsEnabled = isSupported,
            MinAmount = limits.MinAmount,
            MaxAmount = limits.MaxAmount,
            SupportedCurrencies = GetSupportedCurrencies(gateway),
            AdditionalInfo = GetMethodAdditionalInfo(method, gateway)
        };
    }

    /// <summary>
    /// Gets display name for payment method
    /// </summary>
    private static string GetMethodDisplayName(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => "Credit Card",
            PaymentMethod.DebitCard => "Debit Card",
            PaymentMethod.EWallet => "E-Wallet",
            PaymentMethod.BankTransfer => "Bank Transfer",
            PaymentMethod.QRCode => "QR Code Payment",
            PaymentMethod.Installment => "Installment Payment",
            _ => method.ToString()
        };
    }

    /// <summary>
    /// Gets description for payment method
    /// </summary>
    private static string GetMethodDescription(PaymentMethod method, PaymentGateway gateway)
    {
        return (method, gateway) switch
        {
            (PaymentMethod.CreditCard, PaymentGateway.VnPay) => "Pay with Visa, MasterCard, or JCB credit card",
            (PaymentMethod.QRCode, PaymentGateway.VnPay) => "Scan QR code with your banking app",
            (PaymentMethod.EWallet, PaymentGateway.MoMo) => "Pay with MoMo e-wallet",
            (PaymentMethod.EWallet, PaymentGateway.ZaloPay) => "Pay with ZaloPay e-wallet",
            (PaymentMethod.Installment, PaymentGateway.VnPay) => "Pay in installments (3-24 months)",
            _ => $"Pay using {GetMethodDisplayName(method)}"
        };
    }

    /// <summary>
    /// Gets supported currencies for gateway
    /// </summary>
    private static List<string> GetSupportedCurrencies(PaymentGateway gateway)
    {
        return gateway switch
        {
            PaymentGateway.VnPay => new List<string> { "VND" },
            PaymentGateway.MoMo => new List<string> { "VND" },
            PaymentGateway.ZaloPay => new List<string> { "VND" },
            PaymentGateway.PayPal => new List<string> { "USD", "EUR", "JPY", "VND" },
            _ => new List<string> { "VND" }
        };
    }

    /// <summary>
    /// Gets additional information for payment method
    /// </summary>
    private static Dictionary<string, object> GetMethodAdditionalInfo(PaymentMethod method, PaymentGateway gateway)
    {
        var info = new Dictionary<string, object>();

        if (method == PaymentMethod.Installment && gateway == PaymentGateway.VnPay)
        {
            info["availableTerms"] = new[] { 3, 6, 12, 18, 24 };
            info["interestRates"] = new Dictionary<int, decimal>
            {
                [3] = 0m,     // 0% for 3 months
                [6] = 2.5m,   // 2.5% for 6 months
                [12] = 5.99m, // 5.99% for 12 months
                [18] = 8.99m, // 8.99% for 18 months
                [24] = 11.99m // 11.99% for 24 months
            };
        }

        if (method == PaymentMethod.QRCode)
        {
            info["qrCodeExpiry"] = 900; // 15 minutes
            info["mobileOptimized"] = true;
        }

        return info;
    }
}

/// <summary>
/// Payment limits data structure
/// </summary>
public record PaymentLimits
{
    public decimal MinAmount { get; init; }
    public decimal MaxAmount { get; init; }
    public decimal DailyLimit { get; init; }
}

/// <summary>
/// Payment processing context for audit trails
/// </summary>
public class PaymentProcessingContext
{
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Payment processing result with audit information
/// </summary>
public class PaymentProcessingResult
{
    public PaymentResult Result { get; set; } = new();
    public PaymentProcessingContext Context { get; set; } = new();
    public TimeSpan ProcessingTime => DateTime.UtcNow - Context.StartTime;
    public bool ShouldAudit { get; set; } = true;
}
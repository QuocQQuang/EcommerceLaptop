using FluentValidation;
using EcommerceLaptop.Core.Configuration;

namespace EcommerceLaptop.Core.Validators;

/// <summary>
/// Payment gateway configuration validator following Clean Code principles
/// Ensures all payment gateways are properly configured for security
/// </summary>
public class PaymentGatewaySettingsValidator : AbstractValidator<PaymentGatewaySettings>
{
    public PaymentGatewaySettingsValidator()
    {
        RuleFor(x => x.VnPay)
            .SetValidator(new VnPaySettingsValidator())
            .When(x => x.VnPay.IsEnabled);

        RuleFor(x => x.MoMo)
            .SetValidator(new MoMoSettingsValidator())
            .When(x => x.MoMo.IsEnabled);

        RuleFor(x => x.PayPal)
            .SetValidator(new PayPalSettingsValidator())
            .When(x => x.PayPal.IsEnabled);

        RuleFor(x => x.ZaloPay)
            .SetValidator(new ZaloPaySettingsValidator())
            .When(x => x.ZaloPay.IsEnabled);
    }
}

/// <summary>
/// VnPay configuration validator
/// Ensures Vietnamese payment compliance and security requirements
/// </summary>
public class VnPaySettingsValidator : AbstractValidator<VnPaySettings>
{
    public VnPaySettingsValidator()
    {
        RuleFor(x => x.TmnCode)
            .NotEmpty()
            .WithMessage("VnPay TMN Code is required");

        RuleFor(x => x.HashSecret)
            .NotEmpty()
            .MinimumLength(32)
            .WithMessage("VnPay Hash Secret must be at least 32 characters");

        RuleFor(x => x.PaymentUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .WithMessage("VnPay Payment URL must be a valid HTTPS URL");

        RuleFor(x => x.ReturnUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .WithMessage("VnPay Return URL must be a valid HTTPS URL");

        RuleFor(x => x.NotifyUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .WithMessage("VnPay Notify URL must be a valid HTTPS URL");

        RuleFor(x => x.TimeoutInMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(60)
            .WithMessage("VnPay timeout must be between 1 and 60 minutes");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}

/// <summary>
/// MoMo configuration validator
/// Ensures e-wallet integration security and compliance
/// </summary>
public class MoMoSettingsValidator : AbstractValidator<MoMoSettings>
{
    public MoMoSettingsValidator()
    {
        RuleFor(x => x.PartnerCode)
            .NotEmpty()
            .WithMessage("MoMo Partner Code is required");

        RuleFor(x => x.AccessKey)
            .NotEmpty()
            .WithMessage("MoMo Access Key is required");

        RuleFor(x => x.SecretKey)
            .NotEmpty()
            .MinimumLength(32)
            .WithMessage("MoMo Secret Key must be at least 32 characters");

        RuleFor(x => x.PaymentUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .WithMessage("MoMo Payment URL must be a valid HTTPS URL");

        RuleFor(x => x.PublicKey)
            .NotEmpty()
            .WithMessage("MoMo Public Key is required for signature validation");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               uri.Scheme == Uri.UriSchemeHttps;
    }
}

/// <summary>
/// PayPal configuration validator
/// Ensures international payment gateway security
/// </summary>
public class PayPalSettingsValidator : AbstractValidator<PayPalSettings>
{
    public PayPalSettingsValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("PayPal Client ID is required");

        RuleFor(x => x.ClientSecret)
            .NotEmpty()
            .MinimumLength(32)
            .WithMessage("PayPal Client Secret must be at least 32 characters");

        RuleFor(x => x.Environment)
            .NotEmpty()
            .Must(env => env == "sandbox" || env == "live")
            .WithMessage("PayPal Environment must be 'sandbox' or 'live'");

        RuleFor(x => x.SupportedCurrencies)
            .NotEmpty()
            .WithMessage("At least one supported currency must be specified");

        RuleFor(x => x.WebhookId)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.WebhookUrl))
            .WithMessage("PayPal Webhook ID is required when Webhook URL is provided");
    }
}

/// <summary>
/// ZaloPay configuration validator
/// Ensures Vietnamese mobile payment security
/// </summary>
public class ZaloPaySettingsValidator : AbstractValidator<ZaloPaySettings>
{
    public ZaloPaySettingsValidator()
    {
        RuleFor(x => x.AppId)
            .NotEmpty()
            .WithMessage("ZaloPay App ID is required");

        RuleFor(x => x.Key1)
            .NotEmpty()
            .MinimumLength(32)
            .WithMessage("ZaloPay Key1 must be at least 32 characters");

        RuleFor(x => x.Key2)
            .NotEmpty()
            .MinimumLength(32)
            .WithMessage("ZaloPay Key2 must be at least 32 characters");

        RuleFor(x => x.PaymentUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .WithMessage("ZaloPay Payment URL must be a valid HTTPS URL");

        RuleFor(x => x.QueryUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .WithMessage("ZaloPay Query URL must be a valid HTTPS URL");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               uri.Scheme == Uri.UriSchemeHttps;
    }
}
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
        RuleFor(x => x.PayPal)
            .SetValidator(new PayPalSettingsValidator())
            .When(x => x.PayPal.IsEnabled);
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
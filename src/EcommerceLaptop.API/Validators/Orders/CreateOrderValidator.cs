using FluentValidation;
using EcommerceLaptop.API.Features.Orders;

namespace EcommerceLaptop.API.Validators.Orders;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Request.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID is invalid.");

        RuleFor(x => x.Request.CartId)
            .GreaterThan(0).WithMessage("Cart ID is required.");

        RuleFor(x => x.Request.ShippingAddress)
            .NotEmpty().WithMessage("Shipping address is required.");
            
        RuleFor(x => x.Request.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required.")
            .Must(method => new[] { "COD", "VnPay", "MoMo", "PayPal", "SePay" }.Contains(method))
            .WithMessage("Invalid payment method.");
    }
}

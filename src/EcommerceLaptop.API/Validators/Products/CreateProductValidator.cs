using FluentValidation;
using EcommerceLaptop.API.Features.Products;

namespace EcommerceLaptop.API.Validators.Products;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.RequestBody)
            .Must(x => x.ValueKind == System.Text.Json.JsonValueKind.Object)
            .WithMessage("Request body must be a valid JSON object.");
    }
}

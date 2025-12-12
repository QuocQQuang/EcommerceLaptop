using MediatR;
using AutoMapper;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.Products;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Infrastructure.Services.Security;
using System.Text.Json;
using System.Security.Claims;

namespace EcommerceLaptop.API.Features.Products;

public record CreateProductCommand(JsonElement RequestBody) : IRequest<ProductDto>
{
    public int? AuthenticatedUserId { get; init; }
}

public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly IMapper _mapper;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateProductHandler(
        IAsyncRepository<Product> productRepository,
        IMapper mapper,
        IAuditLoggingService auditLoggingService,
        IHttpContextAccessor httpContextAccessor)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _auditLoggingService = auditLoggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rawJson = command.RequestBody.GetRawText();
        
        // Deserialize to base request first to get ProductType
        var baseRequest = JsonSerializer.Deserialize<CreateProductRequest>(rawJson, options);

        if (baseRequest == null || string.IsNullOrEmpty(baseRequest.ProductType))
        {
            throw new ArgumentException("ProductType is required.");
        }

        // Check SKU uniqueness
        // WAS: _productService.IsSKUUniqueAsync(baseRequest.SKU)
        if (!string.IsNullOrEmpty(baseRequest.SKU))
        {
             var count = await _productRepository.CountAsync(new ProductBySkuSpecification(baseRequest.SKU));
             if (count > 0)
             {
                 throw new ArgumentException("SKU already exists");
             }
        }

        Product product = MapToProduct(command.RequestBody, baseRequest.ProductType);

        // WAS: _productService.CreateProductAsync(product)
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        var createdProduct = await _productRepository.AddAsync(product);
        
        var productDto = _mapper.Map<ProductDto>(createdProduct);

        // Log admin product creation activity
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            var context = _httpContextAccessor.HttpContext;
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString();
            var userAgent = context?.Request.Headers["User-Agent"].ToString();

            await _auditLoggingService.LogAdminActivityAsync(
                userId.Value,
                "product_created",
                $"Admin created product: {createdProduct.Name} (ID: {createdProduct.Id}, SKU: {createdProduct.SKU})",
                ipAddress ?? "Unknown",
                userAgent ?? "Unknown",
                targetResource: $"Product:{createdProduct.Id}"
            );
        }

        return productDto;
    }

    private int? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return null;

        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (idClaim != null && int.TryParse(idClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }

    private Product MapToProduct(JsonElement request, string productType)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rawJson = request.GetRawText();

        return productType.ToLower() switch
        {
            "laptop" => JsonSerializer.Deserialize<Laptop>(rawJson, options) ?? throw new JsonException("Failed to deserialize to Laptop."),
            "accessory" => JsonSerializer.Deserialize<Accessory>(rawJson, options) ?? throw new JsonException("Failed to deserialize to Accessory."),
            "bundle" => JsonSerializer.Deserialize<Bundle>(rawJson, options) ?? throw new JsonException("Failed to deserialize to Bundle."),
            _ => throw new ArgumentException("Invalid product type specified.")
        };
    }
}

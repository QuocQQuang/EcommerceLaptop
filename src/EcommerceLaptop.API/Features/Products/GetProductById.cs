using MediatR;
using AutoMapper;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.Products;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Infrastructure.Services.Security;
using System.Security.Claims;

namespace EcommerceLaptop.API.Features.Products;

public record GetProductByIdQuery(int Id) : IRequest<ProductDto>
{
    public int? AuthenticatedUserId { get; init; }
}

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly IMapper _mapper;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetProductByIdHandler(
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

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // Get product with Details
        var product = await _productRepository.GetEntityWithSpec(new ProductWithDetailsSpecification(request.Id));
        
        if (product is null)
        {
            throw new KeyNotFoundException($"Product with ID {request.Id} not found");
        }
            
        if (!product.IsActive)
        {
             throw new KeyNotFoundException($"Product with ID {request.Id} not found");
        }

        // Load variants if this is a base product
        if (product.IsBaseProduct)
        {
            var variants = await _productRepository.GetAsync(new ProductVariantsSpecification(request.Id));
            product.Variants = variants.ToList();
        }

        // Log product view activity
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            var context = _httpContextAccessor.HttpContext;
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString();
            var userAgent = context?.Request.Headers["User-Agent"].ToString();

            await _auditLoggingService.LogUserActivityAsync(
                userId.Value,
                "product_view",
                $"User viewed product: {product.Name} (ID: {request.Id})",
                ipAddress ?? "Unknown",
                userAgent ?? "Unknown"
            );
        }

        var productDto = _mapper.Map<ProductDto>(product);
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
}

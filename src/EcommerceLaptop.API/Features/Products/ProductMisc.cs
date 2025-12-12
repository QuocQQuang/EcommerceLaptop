using MediatR;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.Products;
using EcommerceLaptop.Core.Specifications.Order;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using AutoMapper;

namespace EcommerceLaptop.API.Features.Products;

// Get Product Specifications
public record GetProductSpecificationsQuery(int ProductId) : IRequest<Dictionary<string, object>>;

public class GetProductSpecificationsHandler : IRequestHandler<GetProductSpecificationsQuery, Dictionary<string, object>>
{
    private readonly IAsyncRepository<Product> _productRepository;

    public GetProductSpecificationsHandler(IAsyncRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Dictionary<string, object>> Handle(GetProductSpecificationsQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return new Dictionary<string, object>();

        var specs = new Dictionary<string, object>
        {
            ["Id"] = product.Id,
            ["Name"] = product.Name,
            ["Brand"] = product.Brand,
            ["Model"] = product.Model,
            ["Price"] = product.Price,
            ["SKU"] = product.SKU,
            ["Type"] = product.GetType().Name
        };

        switch (product)
        {
             case Laptop laptop:
                specs["Series"] = laptop.Series;
                specs["CPU"] = new { Brand = laptop.CpuBrand, Model = laptop.CpuModel };
                break;
             case Accessory accessory:
                specs["AccessoryType"] = accessory.AccessoryType;
                break;
             case Bundle bundle:
                specs["BundleType"] = bundle.BundleType;
                break;
        }

        return specs;
    }
}

// Validate Compatibility
public record ValidateCompatibilityQuery(int ProductId, int AccessoryId) : IRequest<object>;

public class ValidateCompatibilityHandler : IRequestHandler<ValidateCompatibilityQuery, object>
{
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly IAsyncRepository<Accessory> _accessoryRepository;

    public ValidateCompatibilityHandler(IAsyncRepository<Product> productRepository, IAsyncRepository<Accessory> accessoryRepository)
    {
        _productRepository = productRepository;
        _accessoryRepository = accessoryRepository;
    }

    public async Task<object> Handle(ValidateCompatibilityQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        var accessory = await _accessoryRepository.GetByIdAsync(request.AccessoryId);

        bool isCompatible = false;
        if (product != null && accessory != null)
        {
            if (string.IsNullOrEmpty(accessory.Compatibility))
            {
                 isCompatible = true;
            }
            else
            {
                isCompatible = accessory.Compatibility.Contains(product.Brand) ||
                               accessory.Compatibility.Contains(product.Model) ||
                               accessory.Compatibility.Contains(product.GetType().Name);
            }
        }
        
        return new { IsCompatible = isCompatible };
    }
}

// Get Recommendations
public record GetRecommendationsQuery(int UserId, int Count = 5) : IRequest<IEnumerable<ProductDto>>;

public class GetRecommendationsHandler : IRequestHandler<GetRecommendationsQuery, IEnumerable<ProductDto>>
{
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly IAsyncRepository<OrderItem> _orderItemRepository;
    private readonly IMapper _mapper;

    public GetRecommendationsHandler(
        IAsyncRepository<Product> productRepository, 
        IAsyncRepository<OrderItem> orderItemRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _orderItemRepository = orderItemRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var count = request.Count;

        var userOrderItems = await _orderItemRepository.GetAsync(new OrderItemsByUserSpecification(userId));

        IEnumerable<Product> recommendations;

        if (!userOrderItems.Any())
        {
            var popularSpec = new RecommendedProductsSpecification(count);
            recommendations = await _productRepository.GetAsync(popularSpec);
        }
        else
        {
            var preferredBrands = userOrderItems.Select(oi => oi.Product.Brand).Distinct().ToList();
            var excludeIds = userOrderItems.Select(oi => oi.ProductId).ToList();

            var spec = new RecommendedProductsSpecification(preferredBrands, new List<string>(), excludeIds, count);
            recommendations = await _productRepository.GetAsync(spec);
        }
        
        return _mapper.Map<IEnumerable<ProductDto>>(recommendations);
    }
}

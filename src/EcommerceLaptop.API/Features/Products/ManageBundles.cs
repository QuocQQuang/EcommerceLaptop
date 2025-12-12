using MediatR;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.Products;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using AutoMapper;

namespace EcommerceLaptop.API.Features.Products;

// Create Bundle
public record CreateBundleCommand(CreateBundleRequest Request) : IRequest<BundleDto>;

public class CreateBundleHandler : IRequestHandler<CreateBundleCommand, BundleDto>
{
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly IAsyncRepository<Bundle> _bundleRepository;
    private readonly IMapper _mapper;

    public CreateBundleHandler(
        IAsyncRepository<Product> productRepository, 
        IAsyncRepository<Bundle> bundleRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _bundleRepository = bundleRepository;
        _mapper = mapper;
    }

    public async Task<BundleDto> Handle(CreateBundleCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        
        // Logic from ProductService.CreateBundleAsync
        var products = await _productRepository.GetAsync(new ProductsByIdsSpecification(req.ProductIds));

        if (products.Count != req.ProductIds.Count())
        {
            throw new ArgumentException("One or more products not found or inactive");
        }

        // Calculate price locally
        var totalOriginalPrice = products.Sum(p => p.Price);
        var discountAmount = totalOriginalPrice * (req.DiscountPercentage / 100);
        var finalPrice = totalOriginalPrice - discountAmount;
        var totalPrice = Math.Max(finalPrice, totalOriginalPrice * 0.1m);

        var bundle = new Bundle
        {
            Name = req.Name,
            Description = req.Description,
            Price = totalPrice,
            SKU = $"BUNDLE-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            IsActive = true,
            BundleType = "Standard",
            DiscountPercentage = req.DiscountPercentage,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BundleItems = new List<BundleItem>()
        };

        foreach (var product in products)
        {
            bundle.BundleItems.Add(new BundleItem
            {
                ProductId = product.Id,
                Quantity = 1,
                DiscountPercentage = req.DiscountPercentage
            });
        }

        var createdBundle = await _bundleRepository.AddAsync(bundle);
        return _mapper.Map<BundleDto>(createdBundle);
    }
}

// Calculate Bundle Price
public record CalculateBundlePriceQuery(CalculateBundlePriceRequest Request) : IRequest<object>; 

public class CalculateBundlePriceHandler : IRequestHandler<CalculateBundlePriceQuery, object>
{
    private readonly IAsyncRepository<Product> _productRepository;

    public CalculateBundlePriceHandler(IAsyncRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<object> Handle(CalculateBundlePriceQuery query, CancellationToken cancellationToken)
    {
        var productIds = query.Request.ProductIds;
        var discountPercentage = query.Request.DiscountPercentage;

        var products = await _productRepository.GetAsync(new ProductsByIdsSpecification(productIds));

        if (!products.Any())
             return new { CalculatedPrice = 0 };

        var totalOriginalPrice = products.Sum(p => p.Price);
        var discountAmount = totalOriginalPrice * (discountPercentage / 100);
        var finalPrice = totalOriginalPrice - discountAmount;
        var price = Math.Max(finalPrice, totalOriginalPrice * 0.1m);

        return new { CalculatedPrice = price };
    }
}

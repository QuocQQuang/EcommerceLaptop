using MediatR;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.Products;
using EcommerceLaptop.Core.Entities;
using AutoMapper;

namespace EcommerceLaptop.API.Features.Products;

// Advanced Laptop Search
public record GetAdvancedLaptopsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Brand = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? CpuBrand = null,
    string? CpuGeneration = null,
    int? MinCpuCores = null,
    int? MinRamGB = null,
    int? MaxRamGB = null,
    string? RamType = null,
    string? StorageType = null,
    int? MinStorageGB = null,
    string? GpuType = null,
    string? GpuBrand = null,
    decimal? MinDisplaySize = null,
    decimal? MaxDisplaySize = null,
    string? DisplayResolution = null,
    int? MinRefreshRate = null,
    bool? Touchscreen = null,
    string? TargetAudience = null
) : IRequest<PagedResult<LaptopDto>>;

public class GetAdvancedLaptopsHandler : IRequestHandler<GetAdvancedLaptopsQuery, PagedResult<LaptopDto>>
{
    private readonly IAsyncRepository<Laptop> _laptopRepository;
    private readonly IMapper _mapper;

    public GetAdvancedLaptopsHandler(IAsyncRepository<Laptop> laptopRepository, IMapper mapper)
    {
        _laptopRepository = laptopRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<LaptopDto>> Handle(GetAdvancedLaptopsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = (request.PageSize < 1 || request.PageSize > 100) ? 20 : request.PageSize;

        var spec = new AdvancedLaptopSpecification(
            request.Search, request.Brand, request.MinPrice, request.MaxPrice, 
            request.CpuBrand, request.CpuGeneration, request.MinCpuCores,
            request.MinRamGB, request.MaxRamGB, request.RamType, request.StorageType, request.MinStorageGB,
            request.GpuType, request.GpuBrand, request.MinDisplaySize, request.MaxDisplaySize, 
            request.DisplayResolution, request.MinRefreshRate,
            request.Touchscreen, request.TargetAudience,
            skip: (page - 1) * pageSize, take: pageSize);

        var countSpec = new AdvancedLaptopSpecification(
            request.Search, request.Brand, request.MinPrice, request.MaxPrice, 
            request.CpuBrand, request.CpuGeneration, request.MinCpuCores,
            request.MinRamGB, request.MaxRamGB, request.RamType, request.StorageType, request.MinStorageGB,
            request.GpuType, request.GpuBrand, request.MinDisplaySize, request.MaxDisplaySize, 
            request.DisplayResolution, request.MinRefreshRate,
            request.Touchscreen, request.TargetAudience);

        var totalCount = await _laptopRepository.CountAsync(countSpec);
        var items = await _laptopRepository.GetAsync(spec);

        var laptopDtos = _mapper.Map<List<LaptopDto>>(items);

        return new PagedResult<LaptopDto>
        {
            Items = laptopDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

// Compatible Accessories Search
public record GetCompatibleAccessoriesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? AccessoryType = null,
    string? Compatibility = null,
    int? ProductId = null
) : IRequest<PagedResult<AccessoryDto>>;

public class GetCompatibleAccessoriesHandler : IRequestHandler<GetCompatibleAccessoriesQuery, PagedResult<AccessoryDto>>
{
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly IAsyncRepository<Accessory> _accessoryRepository;
    private readonly IMapper _mapper;

    public GetCompatibleAccessoriesHandler(
        IAsyncRepository<Product> productRepository,
        IAsyncRepository<Accessory> accessoryRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _accessoryRepository = accessoryRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<AccessoryDto>> Handle(GetCompatibleAccessoriesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = (request.PageSize < 1 || request.PageSize > 100) ? 20 : request.PageSize;

        Product? targetProduct = null;
        if (request.ProductId.HasValue)
        {
            targetProduct = await _productRepository.GetByIdAsync(request.ProductId.Value);
        }

        var spec = new AccessorySpecification(
            request.Search, request.AccessoryType, request.Compatibility, request.ProductId, targetProduct,
            skip: (page - 1) * pageSize, take: pageSize, includeCompatibilityLogic: true);

        var countSpec = new AccessorySpecification(
            request.Search, request.AccessoryType, request.Compatibility, request.ProductId, targetProduct, includeCompatibilityLogic: true);

        var totalCount = await _accessoryRepository.CountAsync(countSpec);
        var items = await _accessoryRepository.GetAsync(spec);

        var accessoryDtos = _mapper.Map<List<AccessoryDto>>(items);

        return new PagedResult<AccessoryDto>
        {
            Items = accessoryDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

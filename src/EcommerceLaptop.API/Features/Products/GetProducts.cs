using MediatR;
using AutoMapper;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.Products;

namespace EcommerceLaptop.API.Features.Products;

public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Type = null,
    string? Brand = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? SortBy = null
) : IRequest<PagedResult<ProductDto>>;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly IMapper _mapper;

    public GetProductsHandler(IAsyncRepository<Product> productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Enforce valid page/pageSize
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = (request.PageSize < 1 || request.PageSize > 100) ? 20 : request.PageSize;

        // Force IsActive = true for public API
        bool? isActive = true;

        var spec = new ProductFilterSpecification(
            request.Search, request.Brand, request.MinPrice, request.MaxPrice, 
            request.Category, isActive, request.SortBy, 
            skip: (page - 1) * pageSize, take: pageSize);

        var countSpec = new ProductFilterSpecification(
            request.Search, request.Brand, request.MinPrice, request.MaxPrice, 
            request.Category, isActive, request.SortBy);
        
        var totalCount = await _productRepository.CountAsync(countSpec);
        var items = await _productRepository.GetAsync(spec);

        var productDtos = _mapper.Map<List<ProductDto>>(items);
        
        return new PagedResult<ProductDto>
        {
            Items = productDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

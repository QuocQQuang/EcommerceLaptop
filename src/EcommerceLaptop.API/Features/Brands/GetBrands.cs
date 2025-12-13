using MediatR;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Brands;

public class BrandWithProductCount 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ProductCount { get; set; }
}

public class BrandSelectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// Get Brands With Product Count
public record GetBrandsWithProductCountQuery : IRequest<List<BrandWithProductCount>>;

public class GetBrandsWithProductCountHandler : IRequestHandler<GetBrandsWithProductCountQuery, List<BrandWithProductCount>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetBrandsWithProductCountHandler> _logger;

    public GetBrandsWithProductCountHandler(ApplicationDbContext context, ILogger<GetBrandsWithProductCountHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BrandWithProductCount>> Handle(GetBrandsWithProductCountQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _context.ProductBrands
                .Select(b => new BrandWithProductCount
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    Description = b.Description,
                    LogoUrl = b.LogoUrl,
                    Website = b.Website,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    ProductCount = b.Products.Count()
                })
                .OrderBy(b => b.Id)
                .ThenBy(b => b.Name)
                .ToListAsync(cancellationToken);

            return brands;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands with product counts");
            throw;
        }
    }
}

// Get All Brands
public record GetAllBrandsQuery : IRequest<List<ProductBrand>>;

public class GetAllBrandsHandler : IRequestHandler<GetAllBrandsQuery, List<ProductBrand>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetAllBrandsHandler> _logger;

    public GetAllBrandsHandler(ApplicationDbContext context, ILogger<GetAllBrandsHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProductBrand>> Handle(GetAllBrandsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _context.ProductBrands
                .OrderBy(b => b.Id)
                .ThenBy(b => b.Name)
                .ToListAsync(cancellationToken);

            return brands;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all brands");
            throw;
        }
    }
}

// Get Brand By Id
public record GetBrandByIdQuery(int Id) : IRequest<ProductBrand?>;

public class GetBrandByIdHandler : IRequestHandler<GetBrandByIdQuery, ProductBrand?>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetBrandByIdHandler> _logger;

    public GetBrandByIdHandler(ApplicationDbContext context, ILogger<GetBrandByIdHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductBrand?> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

            return brand;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand with ID {BrandId}", request.Id);
            throw;
        }
    }
}

// Get Brand By Slug
public record GetBrandBySlugQuery(string Slug) : IRequest<ProductBrand?>;

public class GetBrandBySlugHandler : IRequestHandler<GetBrandBySlugQuery, ProductBrand?>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetBrandBySlugHandler> _logger;

    public GetBrandBySlugHandler(ApplicationDbContext context, ILogger<GetBrandBySlugHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductBrand?> Handle(GetBrandBySlugQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Slug == request.Slug, cancellationToken);

            return brand;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand with slug {Slug}", request.Slug);
            throw;
        }
    }
}

// Get Brands For Select
public record GetBrandsForSelectQuery : IRequest<List<BrandSelectItem>>;

public class GetBrandsForSelectHandler : IRequestHandler<GetBrandsForSelectQuery, List<BrandSelectItem>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetBrandsForSelectHandler> _logger;

    public GetBrandsForSelectHandler(ApplicationDbContext context, ILogger<GetBrandsForSelectHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BrandSelectItem>> Handle(GetBrandsForSelectQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var brands = await _context.ProductBrands
                .Where(b => b.IsActive)
                .Select(b => new BrandSelectItem
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    IsActive = b.IsActive
                })
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            return brands;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands for select");
            throw;
        }
    }
}

// Check Brand Slug Unique
public record CheckBrandSlugUniqueQuery(string Slug, int? ExcludeId = null) : IRequest<bool>;

public class CheckBrandSlugUniqueHandler : IRequestHandler<CheckBrandSlugUniqueQuery, bool>
{
    private readonly ApplicationDbContext _context;

    public CheckBrandSlugUniqueHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(CheckBrandSlugUniqueQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ProductBrands.Where(b => b.Slug == request.Slug);

        if (request.ExcludeId.HasValue)
        {
            query = query.Where(b => b.Id != request.ExcludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }
}

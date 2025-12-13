using MediatR;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Categories;

// Get Category Tree
public record GetCategoryTreeQuery : IRequest<List<ProductCategory>>;

public class GetCategoryTreeHandler : IRequestHandler<GetCategoryTreeQuery, List<ProductCategory>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetCategoryTreeHandler> _logger;

    public GetCategoryTreeHandler(ApplicationDbContext context, ILogger<GetCategoryTreeHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProductCategory>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Children)
                .Where(c => c.ParentId == null) // Get root categories
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category tree");
            throw; // Let GlobalExceptionHandler handle it
        }
    }
}

// Get All Categories (Flat)
public record GetAllCategoriesQuery : IRequest<List<ProductCategory>>;

public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, List<ProductCategory>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetAllCategoriesHandler> _logger;

    public GetAllCategoriesHandler(ApplicationDbContext context, ILogger<GetAllCategoriesHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProductCategory>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Parent)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            throw;
        }
    }
}

// Get Category By Id
public record GetCategoryByIdQuery(int Id) : IRequest<ProductCategory?>;

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, ProductCategory?>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetCategoryByIdHandler> _logger;

    public GetCategoryByIdHandler(ApplicationDbContext context, ILogger<GetCategoryByIdHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductCategory?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _context.ProductCategories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category with ID {CategoryId}", request.Id);
            throw;
        }
    }
}

// Get Category By Slug
public record GetCategoryBySlugQuery(string Slug) : IRequest<ProductCategory?>;

public class GetCategoryBySlugHandler : IRequestHandler<GetCategoryBySlugQuery, ProductCategory?>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetCategoryBySlugHandler> _logger;

    public GetCategoryBySlugHandler(ApplicationDbContext context, ILogger<GetCategoryBySlugHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductCategory?> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _context.ProductCategories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Slug == request.Slug, cancellationToken);

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category with slug {Slug}", request.Slug);
            throw;
        }
    }
}

// Get Categories With Product Count
public record GetCategoriesWithProductCountQuery : IRequest<List<CategoryWithProductCount>>;

public class CategoryWithProductCount 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ProductCount { get; set; }
}

public class GetCategoriesWithProductCountHandler : IRequestHandler<GetCategoriesWithProductCountQuery, List<CategoryWithProductCount>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetCategoriesWithProductCountHandler> _logger;

    public GetCategoriesWithProductCountHandler(ApplicationDbContext context, ILogger<GetCategoriesWithProductCountHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CategoryWithProductCount>> Handle(GetCategoriesWithProductCountQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _context.ProductCategories
                .Select(c => new CategoryWithProductCount
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    IsActive = c.IsActive,
                    ParentId = c.ParentId,
                    SortOrder = c.SortOrder,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    ProductCount = c.Products.Count()
                })
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories with product counts");
            throw;
        }
    }
}

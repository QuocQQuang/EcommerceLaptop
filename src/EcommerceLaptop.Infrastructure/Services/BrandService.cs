using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Service for managing product brands
/// </summary>
public class BrandService : IBrandService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BrandService> _logger;

    public BrandService(ApplicationDbContext context, ILogger<BrandService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all brands with product counts
    /// </summary>
    public async Task<ServiceResult<List<BrandWithProductCount>>> GetBrandsWithProductCountAsync()
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
                    // No sort order property
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    ProductCount = b.Products.Count()
                })
                .OrderBy(b => b.Id)
                .ThenBy(b => b.Name)
                .ToListAsync();

            return ServiceResult<List<BrandWithProductCount>>.Success(brands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands with product counts");
            return ServiceResult<List<BrandWithProductCount>>.Failure("Error retrieving brands with product counts");
        }
    }

    /// <summary>
    /// Get all brands
    /// </summary>
    public async Task<ServiceResult<List<ProductBrand>>> GetAllBrandsAsync()
    {
        try
        {
            var brands = await _context.ProductBrands
                .OrderBy(b => b.Id)
                .ThenBy(b => b.Name)
                .ToListAsync();

            return ServiceResult<List<ProductBrand>>.Success(brands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all brands");
            return ServiceResult<List<ProductBrand>>.Failure("Error retrieving brands");
        }
    }

    /// <summary>
    /// Get brand by ID
    /// </summary>
    public async Task<ServiceResult<ProductBrand?>> GetBrandByIdAsync(int id)
    {
        try
        {
            var brand = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);

            return ServiceResult<ProductBrand?>.Success(brand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand with ID {BrandId}", id);
            return ServiceResult<ProductBrand?>.Failure("Error retrieving brand");
        }
    }

    /// <summary>
    /// Get brand by slug
    /// </summary>
    public async Task<ServiceResult<ProductBrand?>> GetBrandBySlugAsync(string slug)
    {
        try
        {
            var brand = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Slug == slug);

            return ServiceResult<ProductBrand?>.Success(brand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand with slug {Slug}", slug);
            return ServiceResult<ProductBrand?>.Failure("Error retrieving brand");
        }
    }

    /// <summary>
    /// Create a new brand
    /// </summary>
    public async Task<ServiceResult<ProductBrand>> CreateBrandAsync(ProductBrand brand)
    {
        try
        {
            // Check if slug is unique
            if (!await IsSlugUniqueAsync(brand.Slug))
            {
                return ServiceResult<ProductBrand>.Failure("Brand slug must be unique");
            }

            // No sort order needed - using Id

            brand.CreatedAt = DateTime.UtcNow;
            brand.UpdatedAt = DateTime.UtcNow;

            _context.ProductBrands.Add(brand);
            await _context.SaveChangesAsync();

            return ServiceResult<ProductBrand>.Success(brand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating brand {BrandName}", brand.Name);
            return ServiceResult<ProductBrand>.Failure("Error creating brand");
        }
    }

    /// <summary>
    /// Update an existing brand
    /// </summary>
    public async Task<ServiceResult<ProductBrand>> UpdateBrandAsync(ProductBrand brand)
    {
        try
        {
            var existing = await _context.ProductBrands.FindAsync(brand.Id);
            if (existing == null)
            {
                return ServiceResult<ProductBrand>.Failure("Brand not found");
            }

            // Check if slug is unique (excluding current brand)
            if (!await IsSlugUniqueAsync(brand.Slug, brand.Id))
            {
                return ServiceResult<ProductBrand>.Failure("Brand slug must be unique");
            }

            // Update properties
            existing.Name = brand.Name;
            existing.Slug = brand.Slug;
            existing.Description = brand.Description;
            existing.LogoUrl = brand.LogoUrl;
            existing.Website = brand.Website;
            existing.IsActive = brand.IsActive;
            // Sort order not used
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<ProductBrand>.Success(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand with ID {BrandId}", brand.Id);
            return ServiceResult<ProductBrand>.Failure("Error updating brand");
        }
    }

    /// <summary>
    /// Delete a brand
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteBrandAsync(int id)
    {
        try
        {
            var brand = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                return ServiceResult<bool>.Failure("Brand not found");
            }

            // Check if brand has products
            if (brand.Products.Any())
            {
                return ServiceResult<bool>.Failure("Cannot delete brand that has products assigned to it");
            }

            _context.ProductBrands.Remove(brand);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand with ID {BrandId}", id);
            return ServiceResult<bool>.Failure("Error deleting brand");
        }
    }

    /// <summary>
    /// Check if brand slug is unique
    /// </summary>
    public async Task<bool> IsSlugUniqueAsync(string slug, int? excludeId = null)
    {
        var query = _context.ProductBrands.Where(b => b.Slug == slug);

        if (excludeId.HasValue)
        {
            query = query.Where(b => b.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    /// <summary>
    /// Reassign products from one brand to another, then delete the original brand
    /// </summary>
    public async Task<ServiceResult<bool>> ReassignProductsAndDeleteBrandAsync(int brandIdToDelete, int newBrandId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var brandToDelete = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == brandIdToDelete);

            if (brandToDelete == null)
            {
                return ServiceResult<bool>.Failure("Brand to delete not found");
            }

            var newBrand = await _context.ProductBrands
                .FirstOrDefaultAsync(b => b.Id == newBrandId);

            if (newBrand == null)
            {
                return ServiceResult<bool>.Failure("Target brand not found");
            }

            // Reassign all products to the new brand
            var products = await _context.Products
                .Where(p => p.BrandId == brandIdToDelete)
                .ToListAsync();

            foreach (var product in products)
            {
                product.BrandId = newBrandId;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the brand
            _context.ProductBrands.Remove(brandToDelete);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Reassigned {ProductCount} products from brand {OldBrandName} to {NewBrandName} and deleted old brand",
                products.Count, brandToDelete.Name, newBrand.Name);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error reassigning products and deleting brand with ID {BrandId}", brandIdToDelete);
            return ServiceResult<bool>.Failure("Error reassigning products and deleting brand");
        }
    }

    /// <summary>
    /// Force delete a brand (deactivate all associated products)
    /// </summary>
    public async Task<ServiceResult<bool>> ForceDeleteBrandAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var brand = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                return ServiceResult<bool>.Failure("Brand not found");
            }

            // Deactivate all products of this brand
            var products = await _context.Products
                .Where(p => p.BrandId == id)
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the brand
            _context.ProductBrands.Remove(brand);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogWarning("Force deleted brand {BrandName} and deactivated {ProductCount} associated products",
                brand.Name, products.Count);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error force deleting brand with ID {BrandId}", id);
            return ServiceResult<bool>.Failure("Error force deleting brand");
        }
    }

    /// <summary>
    /// Get brands for dropdown/select lists
    /// </summary>
    public async Task<ServiceResult<List<BrandSelectItem>>> GetBrandsForSelectAsync()
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
                .ToListAsync();

            return ServiceResult<List<BrandSelectItem>>.Success(brands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands for select");
            return ServiceResult<List<BrandSelectItem>>.Failure("Error retrieving brands for select");
        }
    }
}
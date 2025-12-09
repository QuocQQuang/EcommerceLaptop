using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Data;

using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Service for managing product categories with hierarchical support
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ApplicationDbContext context, ILogger<CategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories in a hierarchical tree structure
    /// </summary>
    public async Task<ServiceResult<List<ProductCategory>>> GetCategoryTreeAsync()
    {
        try
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Children)
                .Where(c => c.ParentId == null) // Get root categories
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return ServiceResult<List<ProductCategory>>.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category tree");
            return ServiceResult<List<ProductCategory>>.Failure("Error retrieving categories");
        }
    }

    /// <summary>
    /// Get all categories as a flat list
    /// </summary>
    public async Task<ServiceResult<List<ProductCategory>>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Parent)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return ServiceResult<List<ProductCategory>>.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return ServiceResult<List<ProductCategory>>.Failure("Error retrieving categories");
        }
    }

    /// <summary>
    /// Get category by ID with parent and children
    /// </summary>
    public async Task<ServiceResult<ProductCategory?>> GetCategoryByIdAsync(int id)
    {
        try
        {
            var category = await _context.ProductCategories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            return ServiceResult<ProductCategory?>.Success(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category with ID {CategoryId}", id);
            return ServiceResult<ProductCategory?>.Failure("Error retrieving category");
        }
    }

    /// <summary>
    /// Get category by slug
    /// </summary>
    public async Task<ServiceResult<ProductCategory?>> GetCategoryBySlugAsync(string slug)
    {
        try
        {
            var category = await _context.ProductCategories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Slug == slug);

            return ServiceResult<ProductCategory?>.Success(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category with slug {Slug}", slug);
            return ServiceResult<ProductCategory?>.Failure("Error retrieving category");
        }
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    public async Task<ServiceResult<ProductCategory>> CreateCategoryAsync(ProductCategory category)
    {
        try
        {
            // Normalize or generate slug
            category.Slug = (category.Slug ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                category.Slug = Slugify(category.Name);
            }
            else
            {
                category.Slug = Slugify(category.Slug);
            }

            // Ensure slug uniqueness (append numeric suffix if necessary)
            category.Slug = await GenerateUniqueSlug(category.Slug);

            // Validate parent exists if specified
            if (category.ParentId.HasValue)
            {
                var parentExists = await _context.ProductCategories
                    .AnyAsync(c => c.Id == category.ParentId.Value);

                if (!parentExists)
                {
                    return ServiceResult<ProductCategory>.Failure("Parent category not found");
                }
            }

            // Set default sort order if not provided
            if (category.SortOrder == 0)
            {
                var maxOrder = await _context.ProductCategories
                    .Where(c => c.ParentId == category.ParentId)
                    .MaxAsync(c => (int?)c.SortOrder) ?? 0;

                category.SortOrder = maxOrder + 1;
            }

            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();

            return ServiceResult<ProductCategory>.Success(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return ServiceResult<ProductCategory>.Failure("Error creating category");
        }
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    public async Task<ServiceResult<ProductCategory>> UpdateCategoryAsync(ProductCategory category)
    {
        try
        {
            var existing = await _context.ProductCategories.FindAsync(category.Id);
            if (existing == null)
            {
                return ServiceResult<ProductCategory>.Failure("Category not found");
            }

            // Normalize slug
            category.Slug = (category.Slug ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                category.Slug = Slugify(category.Name);
            }
            else
            {
                category.Slug = Slugify(category.Slug);
            }

            // Validate slug uniqueness (excluding current category)
            var existingSlug = await _context.ProductCategories
                .AnyAsync(c => c.Slug == category.Slug && c.Id != category.Id);

            if (existingSlug)
            {
                return ServiceResult<ProductCategory>.Failure("Category slug already exists");
            }

            // Validate parent exists if specified and not creating circular reference
            if (category.ParentId.HasValue)
            {
                if (category.ParentId == category.Id)
                {
                    return ServiceResult<ProductCategory>.Failure("Category cannot be its own parent");
                }

                var parentExists = await _context.ProductCategories
                    .AnyAsync(c => c.Id == category.ParentId.Value);

                if (!parentExists)
                {
                    return ServiceResult<ProductCategory>.Failure("Parent category not found");
                }

                // Check for circular references
                if (await IsCircularReference(category.Id, category.ParentId.Value))
                {
                    return ServiceResult<ProductCategory>.Failure("Circular reference detected");
                }
            }

            // Update properties
            existing.Name = category.Name;
            existing.Slug = category.Slug;
            existing.Description = category.Description;
            existing.ImageUrl = category.ImageUrl;
            existing.IsActive = category.IsActive;
            existing.ParentId = category.ParentId;
            existing.SortOrder = category.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<ProductCategory>.Success(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {CategoryId}", category.Id);
            return ServiceResult<ProductCategory>.Failure("Error updating category");
        }
    }

    /// <summary>
    /// Generate a URL-friendly slug from the input text
    /// </summary>
    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Normalize and remove diacritics
        var normalized = input.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var cleaned = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);

        // Replace non-alphanumeric characters with hyphens
        var result = System.Text.RegularExpressions.Regex.Replace(cleaned, "[^A-Za-z0-9]+", "-").Trim('-');

        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Ensure the slug is unique by appending a numeric suffix when collisions occur
    /// </summary>
    private async Task<string> GenerateUniqueSlug(string baseSlug)
    {
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "category";

        var slug = baseSlug;
        var index = 1;
        while (await _context.ProductCategories.AnyAsync(c => c.Slug == slug))
        {
            slug = $"{baseSlug}-{index}";
            index++;
        }

        return slug;
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteCategoryAsync(int id)
    {
        try
        {
            var category = await _context.ProductCategories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return ServiceResult<bool>.Failure("Category not found");
            }

            // Check if category has products
            var hasProducts = await _context.Products
                .AnyAsync(p => p.CategoryId == id);

            if (hasProducts)
            {
                return ServiceResult<bool>.Failure("Cannot delete category with associated products");
            }

            // Check if category has children
            if (category.Children.Any())
            {
                return ServiceResult<bool>.Failure("Cannot delete category with child categories");
            }

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
            return ServiceResult<bool>.Failure("Error deleting category");
        }
    }

    /// <summary>
    /// Get categories with product counts
    /// </summary>
    public async Task<ServiceResult<List<CategoryWithProductCount>>> GetCategoriesWithProductCountAsync()
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
                .ToListAsync();

            return ServiceResult<List<CategoryWithProductCount>>.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories with product counts");
            return ServiceResult<List<CategoryWithProductCount>>.Failure("Error retrieving category statistics");
        }
    }

    /// <summary>
    /// Reorder categories
    /// </summary>
    public async Task<ServiceResult<bool>> ReorderCategoriesAsync(List<CategoryReorderRequest> reorderRequests)
    {
        try
        {
            foreach (var request in reorderRequests)
            {
                var category = await _context.ProductCategories.FindAsync(request.Id);
                if (category != null)
                {
                    category.SortOrder = request.SortOrder;
                    if (request.ParentId.HasValue && request.ParentId != category.ParentId)
                    {
                        // Check for circular reference if changing parent
                        if (await IsCircularReference(category.Id, request.ParentId.Value))
                        {
                            return ServiceResult<bool>.Failure($"Cannot move category to create circular reference");
                        }
                        category.ParentId = request.ParentId;
                    }
                    category.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories");
            return ServiceResult<bool>.Failure("Error reordering categories");
        }
    }

    /// <summary>
    /// Reassign products from one category to another, then delete the original category
    /// </summary>
    public async Task<ServiceResult<bool>> ReassignProductsAndDeleteCategoryAsync(int categoryIdToDelete, int newCategoryId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var categoryToDelete = await _context.ProductCategories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == categoryIdToDelete);

            if (categoryToDelete == null)
            {
                return ServiceResult<bool>.Failure("Category to delete not found");
            }

            var newCategory = await _context.ProductCategories
                .FirstOrDefaultAsync(c => c.Id == newCategoryId);

            if (newCategory == null)
            {
                return ServiceResult<bool>.Failure("Target category not found");
            }

            // Check if category has children - cannot delete category with children
            if (categoryToDelete.Children.Any())
            {
                return ServiceResult<bool>.Failure("Cannot delete category with child categories. Please reassign or delete child categories first.");
            }

            // Reassign all products to the new category
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryIdToDelete)
                .ToListAsync();

            foreach (var product in products)
            {
                product.CategoryId = newCategoryId;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the category
            _context.ProductCategories.Remove(categoryToDelete);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Reassigned {ProductCount} products from category {OldCategoryName} to {NewCategoryName} and deleted old category",
                products.Count, categoryToDelete.Name, newCategory.Name);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error reassigning products and deleting category with ID {CategoryId}", categoryIdToDelete);
            return ServiceResult<bool>.Failure("Error reassigning products and deleting category");
        }
    }

    /// <summary>
    /// Force delete a category (deactivate all associated products and move children to parent)
    /// </summary>
    public async Task<ServiceResult<bool>> ForceDeleteCategoryAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var category = await _context.ProductCategories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return ServiceResult<bool>.Failure("Category not found");
            }

            // Move child categories to parent category (or make them root categories)
            var childCategories = await _context.ProductCategories
                .Where(c => c.ParentId == id)
                .ToListAsync();

            foreach (var child in childCategories)
            {
                child.ParentId = category.ParentId; // This could be null (root level)
                child.UpdatedAt = DateTime.UtcNow;
            }

            // Deactivate all products of this category
            var products = await _context.Products
                .Where(p => p.CategoryId == id)
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the category
            _context.ProductCategories.Remove(category);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogWarning("Force deleted category {CategoryName}, moved {ChildCount} child categories to parent level, and deactivated {ProductCount} associated products",
                category.Name, childCategories.Count, products.Count);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error force deleting category with ID {CategoryId}", id);
            return ServiceResult<bool>.Failure("Error force deleting category");
        }
    }

    /// <summary>
    /// Check for circular references in parent-child relationships
    /// </summary>
    private async Task<bool> IsCircularReference(int categoryId, int parentId)
    {
        var current = await _context.ProductCategories.FindAsync(parentId);
        while (current != null)
        {
            if (current.Id == categoryId)
                return true;

            if (current.ParentId.HasValue)
                current = await _context.ProductCategories.FindAsync(current.ParentId.Value);
            else
                break;
        }
        return false;
    }
}
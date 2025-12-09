using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.DTOs;

namespace EcommerceLaptop.Core.Interfaces.Services;

/// <summary>
/// Interface for category management service
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Get all categories in a hierarchical tree structure
    /// </summary>
    Task<ServiceResult<List<ProductCategory>>> GetCategoryTreeAsync();

    /// <summary>
    /// Get all categories as a flat list
    /// </summary>
    Task<ServiceResult<List<ProductCategory>>> GetAllCategoriesAsync();

    /// <summary>
    /// Get category by ID with parent and children
    /// </summary>
    Task<ServiceResult<ProductCategory?>> GetCategoryByIdAsync(int id);

    /// <summary>
    /// Get category by slug
    /// </summary>
    Task<ServiceResult<ProductCategory?>> GetCategoryBySlugAsync(string slug);

    /// <summary>
    /// Create a new category
    /// </summary>
    Task<ServiceResult<ProductCategory>> CreateCategoryAsync(ProductCategory category);

    /// <summary>
    /// Update an existing category
    /// </summary>
    Task<ServiceResult<ProductCategory>> UpdateCategoryAsync(ProductCategory category);

    /// <summary>
    /// Delete a category
    /// </summary>
    Task<ServiceResult<bool>> DeleteCategoryAsync(int id);

    /// <summary>
    /// Reassign products from one category to another, then delete the original category
    /// </summary>
    Task<ServiceResult<bool>> ReassignProductsAndDeleteCategoryAsync(int categoryIdToDelete, int newCategoryId);

    /// <summary>
    /// Force delete a category (deactivate all associated products)
    /// </summary>
    Task<ServiceResult<bool>> ForceDeleteCategoryAsync(int id);

    /// <summary>
    /// Get categories with product counts
    /// </summary>
    Task<ServiceResult<List<CategoryWithProductCount>>> GetCategoriesWithProductCountAsync();

    /// <summary>
    /// Reorder categories
    /// </summary>
    Task<ServiceResult<bool>> ReorderCategoriesAsync(List<CategoryReorderRequest> reorderRequests);
}
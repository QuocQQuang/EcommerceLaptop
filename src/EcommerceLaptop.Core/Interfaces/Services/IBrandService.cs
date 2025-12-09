using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.DTOs;

namespace EcommerceLaptop.Core.Interfaces.Services;

public interface IBrandService
{
    /// <summary>
    /// Get all brands with product counts
    /// </summary>
    Task<ServiceResult<List<BrandWithProductCount>>> GetBrandsWithProductCountAsync();

    /// <summary>
    /// Get all brands
    /// </summary>
    Task<ServiceResult<List<ProductBrand>>> GetAllBrandsAsync();

    /// <summary>
    /// Get brand by ID
    /// </summary>
    Task<ServiceResult<ProductBrand?>> GetBrandByIdAsync(int id);

    /// <summary>
    /// Get brand by slug
    /// </summary>
    Task<ServiceResult<ProductBrand?>> GetBrandBySlugAsync(string slug);

    /// <summary>
    /// Create a new brand
    /// </summary>
    Task<ServiceResult<ProductBrand>> CreateBrandAsync(ProductBrand brand);

    /// <summary>
    /// Update an existing brand
    /// </summary>
    Task<ServiceResult<ProductBrand>> UpdateBrandAsync(ProductBrand brand);

    /// <summary>
    /// Delete a brand
    /// </summary>
    Task<ServiceResult<bool>> DeleteBrandAsync(int id);

    /// <summary>
    /// Reassign products from one brand to another, then delete the original brand
    /// </summary>
    Task<ServiceResult<bool>> ReassignProductsAndDeleteBrandAsync(int brandIdToDelete, int newBrandId);

    /// <summary>
    /// Force delete a brand (deactivate all associated products)
    /// </summary>
    Task<ServiceResult<bool>> ForceDeleteBrandAsync(int id);

    /// <summary>
    /// Check if brand slug is unique
    /// </summary>
    Task<bool> IsSlugUniqueAsync(string slug, int? excludeId = null);

    /// <summary>
    /// Get brands for dropdown/select lists
    /// </summary>
    Task<ServiceResult<List<BrandSelectItem>>> GetBrandsForSelectAsync();
}
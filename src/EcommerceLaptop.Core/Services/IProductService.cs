using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Product service interface for managing products, laptops, accessories, and bundles
/// Implements repository pattern with clean architecture principles
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets product by ID with related data
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product with images and inventory</returns>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>
    /// Gets product by ID with type-specific details (TPT optimized)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product with full type-specific details</returns>
    Task<Product?> GetByIdWithDetailsAsync(int id);

    /// <summary>
    /// Gets paginated list of products with filtering and search
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="searchTerm">Search term for name, brand, model</param>
    /// <param name="productType">Filter by product type (Laptop, Accessory, Bundle)</param>
    /// <param name="brand">Filter by brand</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="isActive">Filter by active status</param>
    /// <returns>Paginated product list</returns>
    Task<PagedResult<Product>> GetProductsAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? productType = null,
        string? brand = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isActive = null,
        string? sortBy = null);

    /// <summary>
    /// Gets laptops with specific filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="brand">Brand filter</param>
    /// <param name="minPrice">Minimum price</param>
    /// <param name="maxPrice">Maximum price</param>
    /// <param name="cpuBrand">CPU brand filter</param>
    /// <param name="ramCapacityGB">RAM capacity filter</param>
    /// <param name="storageType">Storage type filter</param>
    /// <returns>Paginated laptop list</returns>
    Task<PagedResult<Laptop>> GetLaptopsAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? cpuBrand = null,
        int? ramCapacityGB = null,
        string? storageType = null);

    /// <summary>
    /// Gets accessories with filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="accessoryType">Accessory type filter</param>
    /// <param name="compatibility">Compatibility filter</param>
    /// <returns>Paginated accessory list</returns>
    Task<PagedResult<Accessory>> GetAccessoriesAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? accessoryType = null,
        string? compatibility = null);

    /// <summary>
    /// Gets bundles with filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="bundleType">Bundle type filter</param>
    /// <returns>Paginated bundle list</returns>
    Task<PagedResult<Bundle>> GetBundlesAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? bundleType = null);

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="product">Product to create</param>
    /// <returns>Created product</returns>
    Task<Product> CreateProductAsync(Product product);

    /// <summary>
    /// Updates existing product
    /// </summary>
    /// <param name="product">Product to update</param>
    /// <returns>Updated product</returns>
    Task<Product> UpdateProductAsync(Product product);

    /// <summary>
    /// Deletes product (soft delete)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteProductAsync(int id);

    /// <summary>
    /// Gets product by SKU
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <returns>Product if found</returns>
    Task<Product?> GetBySKUAsync(string sku);

    /// <summary>
    /// Checks if SKU is unique
    /// </summary>
    /// <param name="sku">SKU to check</param>
    /// <param name="excludeProductId">Product ID to exclude from check</param>
    /// <returns>True if SKU is unique</returns>
    Task<bool> IsSKUUniqueAsync(string sku, int? excludeProductId = null);

    /// <summary>
    /// Gets featured products
    /// </summary>
    /// <param name="count">Number of products to return</param>
    /// <returns>Featured products</returns>
    Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 10);

    /// <summary>
    /// Gets related products based on category and brand
    /// </summary>
    /// <param name="productId">Base product ID</param>
    /// <param name="count">Number of related products to return</param>
    /// <returns>Related products</returns>
    Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int count = 5);

    /// <summary>
    /// Gets products by brand
    /// </summary>
    /// <param name="brand">Brand name</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Products by brand</returns>
    Task<PagedResult<Product>> GetProductsByBrandAsync(string brand, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets all unique brands
    /// </summary>
    /// <returns>List of brands</returns>
    Task<IEnumerable<string>> GetBrandsAsync();

    /// <summary>
    /// Updates product images
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="images">Image list</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateProductImagesAsync(int productId, IEnumerable<ProductImage> images);

    /// <summary>
    /// Deletes a product image
    /// </summary>
    /// <param name="imageId">Image ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteProductImageAsync(int imageId);

    /// <summary>
    /// Gets a product image by ID
    /// </summary>
    /// <param name="imageId">Image ID</param>
    /// <returns>Product image if found</returns>
    Task<ProductImage?> GetProductImageAsync(int imageId);

    // T005 Enhanced Product Catalog Operations

    /// <summary>
    /// Gets laptops with advanced hardware filtering
    /// </summary>
    Task<PagedResult<Laptop>> GetLaptopsWithAdvancedFilteringAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? cpuBrand = null,
        string? cpuGeneration = null,
        int? minCpuCores = null,
        int? minRamCapacityGB = null,
        int? maxRamCapacityGB = null,
        string? ramType = null,
        string? storageType = null,
        int? minStorageCapacityGB = null,
        string? gpuType = null,
        string? gpuBrand = null,
        decimal? minDisplaySize = null,
        decimal? maxDisplaySize = null,
        string? displayResolution = null,
        int? minRefreshRate = null,
        bool? touchscreen = null,
        string? targetAudience = null);

    /// <summary>
    /// Gets accessories with compatibility filtering
    /// </summary>
    Task<PagedResult<Accessory>> GetAccessoriesWithCompatibilityAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? accessoryType = null,
        string? compatibility = null,
        int? compatibleProductId = null);

    /// <summary>
    /// Creates a product bundle with dynamic pricing
    /// </summary>
    Task<Bundle> CreateBundleAsync(string name, string description, IEnumerable<int> productIds,
        decimal discountPercentage, DateTime? validFrom = null, DateTime? validTo = null);

    /// <summary>
    /// Calculates bundle pricing dynamically
    /// </summary>
    Task<decimal> CalculateBundlePriceAsync(IEnumerable<int> productIds, decimal discountPercentage);

    /// <summary>
    /// Gets product specifications as structured data
    /// </summary>
    Task<Dictionary<string, object>> GetProductSpecificationsAsync(int productId);

    /// <summary>
    /// Validates product compatibility
    /// </summary>
    Task<bool> ValidateProductCompatibilityAsync(int productId, int accessoryId);

    /// <summary>
    /// Gets product recommendations based on user preferences
    /// </summary>
    Task<IEnumerable<Product>> GetRecommendedProductsAsync(int userId, int count = 5);

    /// <summary>
    /// Gets products for admin management with full details
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="search">Search term</param>
    /// <param name="type">Product type filter</param>
    /// <param name="brand">Brand filter</param>
    /// <param name="status">Status filter (active, inactive, etc.)</param>
    /// <returns>Paginated product list for admin</returns>
    Task<PagedResult<Product>> GetAdminProductsAsync(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        string? type = null,
        string? brand = null,
        string? status = null,
        string? productType = null);

    /// <summary>
    /// Bulk updates product pricing
    /// </summary>
    Task<bool> BulkUpdatePricingAsync(IEnumerable<int> productIds, decimal priceAdjustmentPercentage);

    // Variant Management Methods

    /// <summary>
    /// Gets variants for a specific product
    /// </summary>
    /// <param name="productId">Base product ID</param>
    /// <returns>List of variants</returns>
    Task<IEnumerable<Product>> GetVariantsAsync(int productId);

    /// <summary>
    /// Gets base product for a variant
    /// </summary>
    /// <param name="variantId">Variant ID</param>
    /// <returns>Base product if found</returns>
    Task<Product?> GetBaseProductAsync(int variantId);

    /// <summary>
    /// Creates a new variant for a product
    /// </summary>
    /// <param name="baseProductId">Base product ID</param>
    /// <param name="variant">Variant to create</param>
    /// <returns>Created variant</returns>
    Task<Product> CreateVariantAsync(int baseProductId, Product variant);

    /// <summary>
    /// Updates a variant
    /// </summary>
    /// <param name="variantId">Variant ID</param>
    /// <param name="updatedVariant">Updated variant data</param>
    /// <returns>Updated variant if found</returns>
    Task<Product?> UpdateVariantAsync(int variantId, Product updatedVariant);

    /// <summary>
    /// Deletes a variant
    /// </summary>
    /// <param name="variantId">Variant ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteVariantAsync(int variantId);

    /// <summary>
    /// Gets products with their variants (for product listings)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="productType">Product type filter</param>
    /// <param name="brand">Brand filter</param>
    /// <param name="category">Category filter</param>
    /// <param name="minPrice">Minimum price</param>
    /// <param name="maxPrice">Maximum price</param>
    /// <param name="isActive">Active status filter</param>
    /// <param name="sortBy">Sort criteria</param>
    /// <returns>Paginated products with variants</returns>
    Task<PagedResult<Product>> GetProductsWithVariantsAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? productType = null,
        string? brand = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isActive = null,
        string? sortBy = null);
}

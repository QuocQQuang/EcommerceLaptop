using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Advanced product service interface for T005 enhanced functionality
/// Extends base IProductService with additional product variant and validation methods
/// </summary>
public interface IAdvancedProductService : IProductService
{
    /// <summary>
    /// Gets product variants for a base product
    /// </summary>
    Task<IEnumerable<Product>> GetProductVariantsAsync(int baseProductId);

    /// <summary>
    /// Creates a product variant
    /// </summary>
    Task<Product> CreateProductVariantAsync(int baseProductId, string variantName, 
        Dictionary<string, object> specifications, decimal priceAdjustment);
}

using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class ProductByVariantSkuSpecification : BaseSpecification<Product>
{
    public ProductByVariantSkuSpecification(string variantSku, int? excludeProductId = null) 
        : base(p => p.VariantSku == variantSku && (!excludeProductId.HasValue || p.Id != excludeProductId.Value))
    {
    }
}

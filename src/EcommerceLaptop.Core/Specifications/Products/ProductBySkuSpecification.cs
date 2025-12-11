using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class ProductBySkuSpecification : BaseSpecification<Product>
{
    public ProductBySkuSpecification(string sku, int? excludeProductId = null) 
        : base(p => p.SKU == sku && (!excludeProductId.HasValue || p.Id != excludeProductId.Value))
    {
    }
}

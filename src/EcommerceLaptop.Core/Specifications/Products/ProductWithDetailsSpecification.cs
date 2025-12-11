using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class ProductWithDetailsSpecification : BaseSpecification<Product>
{
    public ProductWithDetailsSpecification(int id) : base(p => p.Id == id)
    {
        AddInclude(p => p.Images);
        AddInclude(p => p.Inventory);
        AddInclude(p => p.Category);
    }

    public ProductWithDetailsSpecification(string sku) : base(p => p.SKU == sku)
    {
        AddInclude(p => p.Images);
        AddInclude(p => p.Inventory);
        AddInclude(p => p.Category);
    }
}

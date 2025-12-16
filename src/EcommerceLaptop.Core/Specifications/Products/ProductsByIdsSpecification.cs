using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class ProductsByIdsSpecification : BaseSpecification<Product>
{
    public ProductsByIdsSpecification(IEnumerable<int> ids) 
        : base(p => ids.Contains(p.Id))
    {
        AddInclude(p => p.Inventory);
        AddInclude(p => p.Category);
        AddInclude(p => p.Images);
        ApplyNoTracking();
    }
}

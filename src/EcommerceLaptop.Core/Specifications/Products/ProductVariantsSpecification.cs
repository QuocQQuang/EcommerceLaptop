using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class ProductVariantsSpecification : BaseSpecification<Product>
{
    public ProductVariantsSpecification(int parentProductId) 
        : base(p => p.ParentProductId == parentProductId && p.IsActive)
    {
        AddInclude(p => p.Images);
        AddInclude(p => p.Inventory);
        AddInclude(p => p.Category);
    }
}

using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class ProductByIdWithSubclassIncludesSpecification<T> : BaseSpecification<T> where T : Product
{
    public ProductByIdWithSubclassIncludesSpecification(int id) : base(p => p.Id == id)
    {
        AddInclude(p => p.Images);
        AddInclude(p => p.Inventory);
        AddInclude(p => p.Category);
        
        // Specific includes for subclasses can be added if we know T
        if (typeof(T) == typeof(Bundle))
        {
             AddInclude("BundleItems");
             AddInclude("BundleItems.Product");
        }
    }
}

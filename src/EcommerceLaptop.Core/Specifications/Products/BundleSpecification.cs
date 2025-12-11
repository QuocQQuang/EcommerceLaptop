using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class BundleSpecification : BaseSpecification<Bundle>
{
    public BundleSpecification(
        string? searchTerm,
        string? bundleType,
        int? skip = null,
        int? take = null)
        : base(b => 
            (string.IsNullOrEmpty(searchTerm) || 
             b.Name.Contains(searchTerm) || 
             b.Brand.Contains(searchTerm) || 
             b.Description.Contains(searchTerm)) &&
            (string.IsNullOrEmpty(bundleType) || b.BundleType == bundleType) &&
            b.IsActive
        )
    {
        AddInclude(b => b.Images);
        AddInclude(b => b.BundleItems);
        AddInclude($"{nameof(Bundle.BundleItems)}.{nameof(BundleItem.Product)}");
        AddInclude(b => b.Category);

        AddOrderBy(b => b.BundleType);
        AddOrderBy(b => b.Name);

        if (skip.HasValue && take.HasValue)
        {
            ApplyPaging(skip.Value, take.Value);
        }
    }
}

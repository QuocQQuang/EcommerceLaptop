using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class AccessorySpecification : BaseSpecification<Accessory>
{
    public AccessorySpecification(
        string? searchTerm,
        string? accessoryType,
        string? compatibility,
        int? compatibleProductId,
        Product? targetProduct, // Optional: if checking compatibility logic in memory or via LINQ
        int? skip = null,
        int? take = null,
        bool includeCompatibilityLogic = false) 
        : base(a => 
            (string.IsNullOrEmpty(searchTerm) || 
             a.Name.Contains(searchTerm) || 
             a.Brand.Contains(searchTerm) || 
             a.Model.Contains(searchTerm) || 
             a.Description.Contains(searchTerm) || 
             a.AccessoryType.Contains(searchTerm)) &&
            (string.IsNullOrEmpty(accessoryType) || a.AccessoryType == accessoryType) &&
            (string.IsNullOrEmpty(compatibility) || (a.Compatibility != null && a.Compatibility.Contains(compatibility))) &&
            (!compatibleProductId.HasValue || targetProduct == null || 
             (a.Compatibility == null || 
              a.Compatibility.Contains(targetProduct.Brand) || 
              a.Compatibility.Contains(targetProduct.Model) || 
              a.Compatibility.Contains(targetProduct.GetType().Name))) &&
            a.IsActive
        )
    {
        AddInclude(a => a.Images);
        AddInclude(a => a.Inventory);
        AddInclude(a => a.Category);

        AddOrderBy(a => a.AccessoryType);
        AddOrderBy(a => a.Brand);
        
        if (includeCompatibilityLogic)
        {
             AddOrderBy(a => a.Name); // ThenBy
        }

        if (skip.HasValue && take.HasValue)
        {
             ApplyPaging(skip.Value, take.Value);
        }
    }
}

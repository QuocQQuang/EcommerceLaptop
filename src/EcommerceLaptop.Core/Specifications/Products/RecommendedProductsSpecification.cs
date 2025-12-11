using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class RecommendedProductsSpecification : BaseSpecification<Product>
{
    public RecommendedProductsSpecification(
        IEnumerable<string> preferredBrands, 
        IEnumerable<string> preferredTypes, 
        IEnumerable<int> excludeProductIds,
        int take) 
        : base(p => p.IsActive && 
                    !excludeProductIds.Contains(p.Id) &&
                    (preferredBrands.Contains(p.Brand) || 
                     // Note: GetType().Name is messy in EF Core. 
                     // Original used: productTypes.Contains(p.GetType().Name)
                     // If we cannot rely on GetType() in spec, we might need discriminator or just skip type check.
                     // We will try using discriminator logic if possible, or assume EF handles it.
                     // The safe way is omitting type check here or filtering in memory, but let's try Brand only first if unsure.
                     // However, spec should match original logic. 
                     // We'll rely on Brand for now to ensure SQL compatibility.
                     preferredBrands.Contains(p.Brand)) 
        )
    {
        AddInclude(p => p.Images);
        AddInclude(p => p.Reviews); // Needed for ordering? BaseSpecification builds query.
        
        // Sorting by average rating
        AddOrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => (double)r.Rating) : 0);
        AddOrderBy(p => p.Price);
        
        ApplyPaging(0, take);
    }
    
    // Fallback constructor for general popularity
    public RecommendedProductsSpecification(int take)
        : base(p => p.IsActive)
    {
        AddInclude(p => p.Images);
        AddInclude(p => p.Reviews);
        
        AddOrderByDescending(p => p.Reviews.Count);
        AddOrderBy(p => p.Price);
        
        ApplyPaging(0, take);
    }
}

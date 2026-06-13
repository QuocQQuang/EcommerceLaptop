using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class ProductFilterSpecification : BaseSpecification<Product>
{
    public ProductFilterSpecification(
        string? searchTerm,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice,
        string? category,
        bool? isActive,
        string? sortBy,
        int? skip = null,
        int? take = null,
        bool baseProductsOnly = false,
        bool variantsOnly = false,
        bool includeVariants = false)
        : this(
            searchTerm,
            SplitFilterValues(brand),
            minPrice,
            maxPrice,
            category,
            isActive,
            sortBy,
            skip,
            take,
            baseProductsOnly,
            variantsOnly,
            includeVariants)
    {
    }

    private ProductFilterSpecification(
        string? searchTerm,
        string[] brands,
        decimal? minPrice,
        decimal? maxPrice,
        string? category,
        bool? isActive,
        string? sortBy,
        int? skip = null,
        int? take = null,
        bool baseProductsOnly = false,
        bool variantsOnly = false,
        bool includeVariants = false)
        : base(p => 
            (string.IsNullOrEmpty(searchTerm) || p.Name.Contains(searchTerm) || p.Brand.Contains(searchTerm)) &&
            (brands.Length == 0 || brands.Contains(p.Brand.ToLower())) &&
            (!minPrice.HasValue || p.Price >= minPrice.Value) &&
            (!maxPrice.HasValue || p.Price <= maxPrice.Value) &&
            (string.IsNullOrEmpty(category) || (p.Category != null && p.Category.Name == category)) &&
            (!isActive.HasValue || p.IsActive == isActive.Value) &&
            (!baseProductsOnly || p.ParentProductId == null) &&
            (!variantsOnly || p.ParentProductId != null)
        )
    {
        AddInclude(p => p.Images);
        AddInclude(p => p.Inventory);
        AddInclude(p => p.ProductBrand);
        AddInclude(p => p.Category);

        if (includeVariants)
        {
             // Include variants and their details
             AddInclude("Variants");
             AddInclude("Variants.Images");
             AddInclude("Variants.Inventory");
        }

        switch (sortBy?.ToLower())
        {
            case "price_asc":
                AddOrderBy(p => p.Price);
                break;
            case "price_desc":
                AddOrderByDescending(p => p.Price);
                break;
            case "popularity":
                AddOrderByDescending(p => p.OrderItems.Count);
                break;
            case "newest":
                AddOrderByDescending(p => p.CreatedAt);
                break;
            case "name":
            case "name_asc":
                AddOrderBy(p => p.Name);
                break;
            case "name_desc":
                 AddOrderByDescending(p => p.Name);
                 break;
            default:
                AddOrderBy(p => p.Name);
                break;
        }

        if (skip.HasValue && take.HasValue)
        {
            ApplyPaging(skip.Value, take.Value);
        }
    }

    private static string[] SplitFilterValues(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => v.ToLowerInvariant())
                .ToArray();
    }
}
